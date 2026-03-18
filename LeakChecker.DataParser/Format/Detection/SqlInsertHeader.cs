namespace LeakChecker.DataParser.Format.Detection;

public class SqlInsertHeader
{
    public string Subject = string.Empty;
    public List<string> Headers = new();
    public string FullHeader = string.Empty;
    public string ValuesTail = string.Empty;
}