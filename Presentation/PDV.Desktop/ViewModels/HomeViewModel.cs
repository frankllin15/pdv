using CommunityToolkit.Mvvm.ComponentModel;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Services;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Desktop.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly ISaleQuery _saleQuery;
    private readonly IOperatorSessionService _operatorSession;

    [ObservableProperty]
    private string _operatorName = string.Empty;

    [ObservableProperty]
    private int _pendingSalesCount;

    [ObservableProperty]
    private int _todaySalesCount;

    [ObservableProperty]
    private decimal _todayTotal;

    [ObservableProperty]
    private bool _isLoading;

    public HomeViewModel(ISaleQuery saleQuery, IOperatorSessionService operatorSession)
    {
        _saleQuery = saleQuery;
        _operatorSession = operatorSession;
        OperatorName = _operatorSession.CurrentOperator?.Name ?? "Operator";
    }

    public async Task LoadDashboardDataAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            var today = DateTime.Today;

            // Get today's sales summary
            var summary = await _saleQuery.GetSummaryAsync(today, today);
            TodaySalesCount = summary.CompletedSales;
            TodayTotal = summary.TotalRevenue;

            // Get pending sales count for current operator
            if (_operatorSession.CurrentOperator != null)
            {
                var pendingSales = await _saleQuery.GetPendingSalesByOperatorAsync(
                    _operatorSession.CurrentOperator.Id);
                PendingSalesCount = pendingSales.Count();
            }
        }
        catch
        {
            // Silently fail - dashboard is informational
        }
        finally
        {
            IsLoading = false;
        }
    }
}
