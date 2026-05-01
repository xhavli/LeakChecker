using LeakChecker.UI.Helpers;
using LeakChecker.UI.Models;
using LeakChecker.UI.Services;
using Microsoft.AspNetCore.Components;

namespace LeakChecker.UI.Components;

public class HomeBase : ComponentBase
{
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] private IDashboardService DashboardService { get; set; } = null!;
    
    protected record ParseColumn(string Header, Func<ParseListModel, string> Value);

    protected List<ParseColumn> Columns => [
        new("File Name",  p => StringFormatter.Truncate(p.FileName, 28)),
        new("Records",    p => StringFormatter.Number(p.RecordsRead)),
        new("Accuracy",   p => p.Accuracy.ToString("F2") + "%"),
        new("File Size",  p => StringFormatter.Bytes(p.BytesRead)),
        new("Duration",   p => StringFormatter.Duration(p.Duration)),
        new("Finished",   p => p.ParseEnd.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
    ];

    protected DashboardStats    Stats        { get; private set; } = new();
    protected List<ParseListModel> RecentParses { get; set; } = [];
    protected bool              Loaded       { get; private set; }
    protected string?           Error        { get; private set; }

    protected override async Task OnInitializedAsync() => await Load();

    public async Task Load()
    {
        Loaded = false;
        Error  = null;
        try
        {
            var statsTask  = DashboardService.GetStatsAsync();
            var parsesTask = DashboardService.GetRecentParsesAsync();
            await Task.WhenAll(statsTask, parsesTask);

            Stats        = statsTask.Result;
            RecentParses = parsesTask.Result;
            Loaded       = true;
        }
        catch (Exception ex)
        {
            Error  = ex.Message;
            Loaded = true;
        }
    }
    
    protected static string AccuracyClass(double a) =>
        a >= 99.8 ? "status-ok" : a >= 95 ? "status-warn" : "status-err";
}