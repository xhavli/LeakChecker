namespace LeakChecker.UI.Models;

public class ParseListModel
{
    public string   MongoId     { get; init; } = string.Empty;
    public Guid     ParseId     { get; init; }
    public string   SourcePath  { get; init; } = string.Empty;
    public string   FileName    => Path.GetFileName(SourcePath);
    public long     RecordsRead { get; init; }
    public long     BytesRead   { get; init; }
    public double   Accuracy    { get; init; }
    public TimeSpan Duration    { get; init; }
    public DateTime ParseEnd    { get; init; }
}