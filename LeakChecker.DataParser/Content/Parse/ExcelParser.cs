using ExcelDataReader;
using LeakChecker.DataParser.Format;
using LeakChecker.DataParser.Format.Detection;
using LeakChecker.DataParser.Logging;
using LeakChecker.DataParser.Logging.Parse;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Content.Parse;

public static class ExcelParser
{
    private const int SamplesLimit = 23;
    private const long ParseLimit = long.MaxValue;

    public static async Task ParseFile(string filePath, IParseLogger logger, IParseStats stats, int thresholdPercent)
    {
        Dictionary<int, Dictionary<int, ItemEnum>> schemas = 
            await ExcelDetector.DetectFormat(filePath, logger, SamplesLimit, thresholdPercent);

        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
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
                
                for (int column = 0; column < fieldsCount; column++)
                {
                    // GetValue(i)? is necessary to have wit ? because can be null
                    string value = reader.GetValue(column)?.ToString() ?? string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    ItemEnum type = schema[column];

                    if (type == ItemEnum.Empty)
                        type = ItemEnum.Other;

                    // Console.WriteLine($"Row [{row}] Column [{column}], {type}: {value}");
                }
                
                // Console.WriteLine();
                if (rowsRead == ParseLimit)
                    break;
            }
            
            rowsRead += row;
        }
        while (reader.NextResult());
        
        stats.MalformedRecordsRead = 0; // Cant properly detect
        stats.RecordsRead = recordsRead;
        stats.LinesRead = rowsRead;
        stats.BytesRead = new FileInfo(filePath).Length;
    }
    
    public static bool IsRowEmpty(IExcelDataReader reader)
    {
        for (int column = 0; column < reader.FieldCount; column++)
        {
            // GetValue(i)? is necessary to have wit ? because can be null
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