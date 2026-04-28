using LeakChecker.UI.Models;
using LeakChecker.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LeakChecker.UI.Components.Pages;

public class ParseDetailBase : ComponentBase
{
    [Parameter] public required string MongoId { get; set; }

    [Inject] protected IDashboardService DashboardService { get; set; } = default!;
    [Inject] protected NavigationManager Nav              { get; set; } = default!;
    [Inject] protected IJSRuntime Js                      { get; set; } = default!;

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
            Detail = await DashboardService.GetParseByIdAsync(MongoId);
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
}