namespace LeakChecker.Content.Parsing;

public class ParsingState
{
    public long MalformedRecordsRead = 0;
    public long RecordsRead = 0;
    public long LinesRead = 0;
    public long BytesRead = 0;
}