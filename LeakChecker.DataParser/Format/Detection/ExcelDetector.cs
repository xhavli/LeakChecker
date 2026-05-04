using ExcelDataReader;
using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Content.Detection;
using LeakChecker.DataParser.Content.Parse;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Helpers.Settings;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Format.Detection;

public static class ExcelDetector
{
    public static async Task<Dictionary<int, Dictionary<int, ItemEnum>>> DetectFormat(
        string filePath, IParseLogger logger, ISettings settings)
    {
        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        logger.LogSchemaDetectionHeader();
        
        int detectSamples = settings.ExcelSamples;
        int thresholdPercent = settings. SchemaThreshold;
        Dictionary<int, Dictionary<int, ItemEnum>> schemas = new();
        

        int sheetNumber = 0;
        
        do
        {
            sheetNumber++;
            string sheetName = reader.Name;
    
            int row = 0;
            int samplesCount = 0;
            
            SchemaHeuristic analyzer = new();
            logger.Log($"Sampling sheet number [{sheetNumber}] with name [{sheetName}].");
    
            while (reader.Read() && samplesCount < detectSamples)
            {
                row++;
                
                if (ExcelParser.IsRowEmpty(reader))
                    continue;
                
                samplesCount++;
                List<SchemaHeuristicRecord> linePatterns = new();
                logger.LogSample(""); // Write blank line to make log more readable
                
                int columns = reader.FieldCount;
                for (int column = 0; column < columns; column++)
                {
                    string value = reader.GetValue(column)?.ToString() ?? string.Empty; // GetValue(i)? is necessary to have wit ? because can be null
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        logger.LogSample($"Excel sample {samplesCount}: row [{row}] column [{column}] value: [EMPTY]");
                        linePatterns.Add(new SchemaHeuristicRecord
                        {
                            Attribute = ItemEnum.Empty,
                            Position = column,
                            DelimitersInside = 0
                        });
                        continue;
                    }
                    
                    logger.LogSample($"Excel sample {samplesCount}: row [{row}] column [{column}] value: {value}");
                    ItemEnum item = await ContentDetector.DetectToken(value, logger);
                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = item,
                        Position = column,
                        DelimitersInside = 0
                    });
                }
                
                analyzer.AddLinePatterns(linePatterns, columns - 1);    // -1 because value is exact number of columns, not delimiters between
            }
    
            logger.LogHeuristicData(analyzer);
            logger.LogDominantSchema(analyzer, thresholdPercent);
        
            var original = analyzer.GetDominantSchema(thresholdPercent);
            var assigned = CredentialAssigner.Assign(original);
            
            logger.LogFinalSchema(assigned);
        
            schemas.Add(sheetNumber, assigned);
        }
        while (reader.NextResult());
        
        return schemas;
    }
}