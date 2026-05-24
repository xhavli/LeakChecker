using LeakChecker.UI.Models;
using LeakChecker.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LeakChecker.UI.Components;

public class ParseDetailBase : ComponentBase
{
    [Parameter] public required string ParseId { get; set; }

    [Inject] protected IJSRuntime Js { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IDashboardService DashboardService { get; set; } = null!;

    protected ParseDetailModel? Detail { get; private set; }
    protected bool   Loaded { get; private set; }
    protected string? Error { get; private set; }

    protected override async Task OnParametersSetAsync() => await Load();

    private async Task Load()
    {
        Loaded = false;
        Error  = null;
        try
        {
            Detail = await DashboardService.GetParseByIdAsync(ParseId);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Loaded = true;
        }
    }
    
    protected async Task OnSchemasToggle(EventArgs e)
    {
        var isOpen = await Js.InvokeAsync<bool>("eval", "document.getElementById('schemas-root').open");
        if (isOpen)
            await Js.InvokeVoidAsync("setSubSchemas", true);
    }
    
    protected static IEnumerable<(T Item, int Count)> RunLengthEncode<T>(IEnumerable<T> source) where T : notnull
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext()) yield break;

        var current = e.Current;
        int count = 1;

        while (e.MoveNext())
        {
            if (EqualityComparer<T>.Default.Equals(e.Current, current))
            {
                count++;
            }
            else
            {
                yield return (current, count);
                current = e.Current;
                count = 1;
            }
        }
        yield return (current, count);
    }
}