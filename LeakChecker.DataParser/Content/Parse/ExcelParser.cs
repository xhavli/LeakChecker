using ExcelDataReader;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Content.Parse;

public class ExcelParser(string filePath, IParseLogger logger, IParseStats stats, ISettings settings)
{
    private const long ParseLimit = long.MaxValue;
    private readonly Guid _parseId = stats.ParseId;
    private readonly List<Dictionary<ItemEnum, List<string>>> _cachedRecords = new();
    private readonly IDatabase _database = settings.Database;

    public async Task ParseAsync()
    {
        Dictionary<int, Dictionary<int, ItemEnum>> schemas = await ExcelDetector.DetectFormat(filePath, logger, settings);

        await using var stream = File.OpenRead(filePath);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        long rowsRead = 0;
        long recordsRead = 0;
        int sheetNumber = 0;
        do
        {
            string sheetName = reader.Name;
            stats.Context.Add(GetContext(sheetName, filePath));
            stats.Formats.Add(FormatEnum.Excel);
            
            sheetNumber++;
            Dictionary<int, ItemEnum> schema = schemas[sheetNumber];
            int expectedFields = schema.Count == 0 ? 0 : schema.Keys.Max() + 1;
    
            int row = 0;
            while (reader.Read())
            {
                row++;
                
                if (IsRowEmpty(reader))
                    continue;
                
                recordsRead++;
                
                int fieldsCount = reader.FieldCount;
                if (fieldsCount != expectedFields)
                {
                    await logger.Log($"Bad row length at at sheet [{sheetName}] row [{row}]: expected {expectedFields}, " +
                                     $"got {fieldsCount}.", LogLevel.Warning);
                } 
                
                await ParseRow(reader, schema, sheetName, row);
                if (_cachedRecords.Count > 2000)
                {
                    await _database.SaveUserMany(_cachedRecords, _parseId);
                    _cachedRecords.Clear();
                }
                
                if (rowsRead == ParseLimit)
                    break;
            }
            
            rowsRead += row;
        }
        while (reader.NextResult());

        await _database.SaveUserMany(_cachedRecords, _parseId);
        
        stats.MalformedRead = 0; // Cant properly detect
        stats.RecordsRead = recordsRead;
        stats.LinesRead = rowsRead;
        stats.BytesRead = new FileInfo(filePath).Length;
    }

    private async Task ParseRow(IExcelDataReader reader, Dictionary<int, ItemEnum> schema, string sheetName, int row)
    {
        Dictionary<ItemEnum, List<string>> record = new();

        for (int column = 0; column < reader.FieldCount; column++)
        {
            // GetValue(i)? can be null
            string value = reader.GetValue(column)?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!schema.TryGetValue(column, out ItemEnum type))
            {
                await logger.Log($"Unmapped Excel column[{column}] at sheet [{sheetName}] row [{row}] = {value}",
                                 LogLevel.Warning, LogContext.Parsing);
                continue;
            }

            if (type == ItemEnum.Empty)
                type = ItemEnum.Other;

            if (!record.TryGetValue(type, out List<string>? values))
                record[type] = values = new List<string>();

            values.Add(value);
        }

        // Forward to content storage
        _cachedRecords.Add(record);
    }
    
    public static bool IsRowEmpty(IExcelDataReader reader)
    {
        for (int column = 0; column < reader.FieldCount; column++)
        {
            // GetValue(i)? can be null
            string value = reader.GetValue(column)?.ToString() ?? string.Empty;
            
            if (!string.IsNullOrWhiteSpace(value))
                return false;
        }

        return true;
    }

    private static string GetContext(string sheetName, string path)
    {
        if (sheetName.StartsWith("Sheet"))
        { 
            string suffix = sheetName.Substring(5); // get part after "Sheet"
            if (!int.TryParse(suffix, out _))
                return sheetName;
        }
        
        return Path.GetFileNameWithoutExtension(path);
    }
}