namespace LeakChecker.UI.Models;

public class DashboardStats
{
    public long   TotalParses   { get; init; }
    public long   TotalBytes    { get; init; }
    public long   TotalRecords  { get; init; }
    public long   TotalMalformed { get; init; }
    public double IngestionAcceptance => TotalRecords > 0
        ? Math.Max(0, (double)(TotalRecords - TotalMalformed) / TotalRecords * 100)
        : 0;
}