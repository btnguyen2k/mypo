using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MyPo.Portfolio.Shared.Api;
using MyPo.Portfolio.Shared.Models;

namespace MyPo.Blazor.Portfolio.App.Pages.PortfolioDetails;

public partial class CPortfolioPreferences : CBase
{
    private const int ALERT_AUTO_CLOSE_MS = 15000;

    [Parameter]
    public PortfolioResp? Portfolio { get; set; }

    [Parameter]
    public IEnumerable<MarketDefResp>? Markets { get; set; }

    private static readonly DayOfWeek[] WeekDays =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
    };

    private static readonly IReadOnlyList<KeyValuePair<int, string>> FiscalMonths =
        [.. Enumerable.Range(1, 12).Select(m => new KeyValuePair<int, string>(m, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)))];

    private bool Saving { get; set; } = false;

    private bool IsContainer { get; set; } = false;
    private DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Monday;
    private int FiscalYearStartMonth { get; set; } = 1;

    private string? _loadedPortfolioId;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Portfolio is not null && !string.Equals(Portfolio.Id, _loadedPortfolioId, StringComparison.Ordinal))
        {
            _loadedPortfolioId = Portfolio.Id;
            LoadFromPortfolio();
        }
    }

    private void LoadFromPortfolio()
    {
        var metadata = Portfolio?.Metadata;
        IsContainer = metadata?.IsContainer ?? false;
        FirstDayOfWeek = metadata?.FirstDayOfWeek ?? DayOfWeek.Monday;
        FiscalYearStartMonth = metadata?.FiscalYearStartMonth ?? 1;
        CloseAlert();
    }

    private async Task BtnClickSave()
    {
        if (Portfolio is null)
        {
            return;
        }

        Saving = true;
        ShowAlert("info", "Saving portfolio preferences...");

        // Preserve all existing metadata fields (totals, report markers, etc.) and only update preferences.
        var metadata = Portfolio.Metadata ?? new PortfolioMetadata();
        var oldPref = metadata.IsContainer;
        metadata.IsContainer = IsContainer;
        metadata.FirstDayOfWeek = FirstDayOfWeek;
        metadata.FiscalYearStartMonth = FiscalYearStartMonth;
        Portfolio.Metadata = metadata;

        var req = CreateOrUpdatePortfolioReq.NewRequest(Portfolio);
        var apiClient = ServiceProvider.GetRequiredService<IPortfolioApiClient>();
        var resp = await apiClient.UpdateMyPortfolioAsync(Portfolio.Id, req, await GetAuthTokenAsync(), ApiBaseUrl);
        if (!resp.IsSuccess)
        {
            ShowAlert("error", $"Failed to save portfolio preferences: {resp.Message}");
        }
        else
        {
            if (resp.Data?.Metadata is not null)
            {
                Portfolio.Metadata = resp.Data.Metadata;
            }
            ShowAlert("success", "Portfolio preferences saved successfully!");
            if (oldPref != IsContainer)
            {
                // If the container preference changed, reload the portfolio to reflect the change in the UI.
                NavigationManager.NavigateTo(NavigationManager.Uri, forceLoad: true);
            }
        }

        Saving = false;
    }
}
