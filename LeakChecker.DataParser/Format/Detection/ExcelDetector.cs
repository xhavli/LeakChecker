using ExcelDataReader;
using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Content.Detection;
using LeakChecker.DataParser.Content.Parsing;
using LeakChecker.DataParser.Format.Schema;
using LeakChecker.DataParser.Logging.Parse;

namespace LeakChecker.DataParser.Format.Detection;

public static class ExcelDetector
{
    public static async Task<Dictionary<int, Dictionary<int, ItemEnum>>> DetectFormat(
        long startSheet, string filePath, IParseLogger logger, int detectSamples, int threshold)
    {
        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        
        await logger.LogSchemaDetectionHeader();
        Dictionary<int, Dictionary<int, ItemEnum>> schema = new();
        
        int sheetNumber = 0;
        do
        {
            sheetNumber++;
            string sheetName = reader.Name; // Context
    
            int row = 0;
            int samplesCount = 0;
            
            SchemaHeuristic analyzer = new();
            await logger.Log($"Sampling sheet number [{sheetNumber}] with name [{sheetName}]");
    
            while (reader.Read() && samplesCount < detectSamples)
            {
                row++;
                if (ExcelParser.IsRowEmpty(reader)) { continue; }
                samplesCount++;
                
                await logger.LogSample(""); // Write blank line to make log more readable
                
                List<SchemaHeuristicRecord> linePatterns = new();
                
                int columns = reader.FieldCount;
                for (int column = 0; column < columns; column++)
                {
                    string value = reader.GetValue(column)?.ToString() ?? string.Empty; // GetValue(i)? is necessary to have wit ? because can be null
                    if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    {
                        await logger.LogSample($"Excel sample {samplesCount}: row [{row}] column [{column}] value: [NULL]");
                        linePatterns.Add(new SchemaHeuristicRecord
                        {
                            Attribute = ItemEnum.Empty,
                            Position = column,
                            DelimitersInside = 0
                        });
                        continue;
                    }
                    
                    await logger.LogSample($"Excel sample {samplesCount}: row [{row}] column [{column}] value: {value}");
                    ItemEnum item = await ContentDetector.DetectToken(value, logger);
                    linePatterns.Add(new SchemaHeuristicRecord
                    {
                        Attribute = item,
                        Position = column,
                        DelimitersInside = 0
                    });
                }
                
                analyzer.AddLinePatterns(linePatterns, columns - 1);    // -1 because value is exact number of columns, not delimiters
            }
    
            await logger.LogHeuristicData(analyzer);
            await logger.LogDominantSchema(analyzer, threshold);
        
            var sheetSchema = analyzer.GetDominantSchema(threshold);
            var assigned = CredentialAssigner.Assign(sheetSchema);
            await logger.LogFinalSchema(assigned);
        
            schema.Add(sheetNumber, assigned);
        }
        while (reader.NextResult());
        
        return schema;
    }
}