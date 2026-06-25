using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyPo.Blazor.Portfolio.App.Shared;

namespace MyPo.Blazor.Portfolio.App.Pages.Widgets;

public partial class WidgetChart : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public string ChartId { get; set; } = $"chart-{Guid.NewGuid():N}";

    /// <summary>Chart.js configuration object (anonymous/POCO) serialized and passed to Chart.js.</summary>
    [Parameter]
    public object? Config { get; set; }

    [Parameter]
    public string CssClass { get; set; } = string.Empty;

    [Parameter]
    public string Style { get; set; } = "position: relative; height: 320px;";

    private IJSObjectReference? _module;
    private string? _lastConfigJson;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (Config is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(Config);
        if (string.Equals(json, _lastConfigJson, StringComparison.Ordinal))
        {
            // configuration unchanged; nothing to re-render
            return;
        }

        _module ??= await PortfolioUtils.LoadJSCharts(JS);
        await _module.InvokeVoidAsync("render", ChartId, Config);
        _lastConfigJson = json;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync("destroy", ChartId);
            }
        }
        catch
        {
            // ignore JS disconnect/disposal errors
        }
        GC.SuppressFinalize(this);
    }
}
