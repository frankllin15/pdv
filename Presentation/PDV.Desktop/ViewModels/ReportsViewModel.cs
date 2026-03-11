using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Queries;
using PDV.Desktop.Services;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public enum ReportType
{
    SalesByPeriod,
    RevenueByPaymentMethod,
    StockPosition
}

public record ReportTypeOption(ReportType Value, string Label);

public partial class ReportsViewModel : ViewModelBase
{
    private readonly ISalesReportQuery _salesReportQuery;
    private readonly IStockReportQuery _stockReportQuery;
    private readonly ICsvExportService _csvExportService;

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private DateTimeOffset? _endDate = DateTimeOffset.Now.Date;

    [ObservableProperty]
    private ReportType _selectedReportType;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _showDateRange = true;

    public List<ReportTypeOption> ReportTypes { get; }

    public ReportsViewModel(
        ISalesReportQuery salesReportQuery,
        IStockReportQuery stockReportQuery,
        ICsvExportService csvExportService)
    {
        _salesReportQuery = salesReportQuery;
        _stockReportQuery = stockReportQuery;
        _csvExportService = csvExportService;

        ReportTypes =
        [
            new(ReportType.SalesByPeriod, Res.Reports_Type_SalesByPeriod),
            new(ReportType.RevenueByPaymentMethod, Res.Reports_Type_RevenueByPayment),
            new(ReportType.StockPosition, Res.Reports_Type_StockPosition)
        ];
    }

    partial void OnSelectedReportTypeChanged(ReportType value)
    {
        ShowDateRange = value != ReportType.StockPosition;
    }

    [RelayCommand]
    private void SetToday()
    {
        StartDate = DateTimeOffset.Now.Date;
        EndDate = DateTimeOffset.Now.Date;
    }

    [RelayCommand]
    private void SetThisWeek()
    {
        var today = DateTimeOffset.Now.Date;
        var dayOfWeek = (int)today.DayOfWeek;
        var monday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        StartDate = today.AddDays(-monday);
        EndDate = today;
    }

    [RelayCommand]
    private void SetThisMonth()
    {
        var now = DateTimeOffset.Now;
        StartDate = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        EndDate = now.Date;
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        IsLoading = true;
        IsError = false;
        StatusMessage = Res.Reports_Msg_Generating;

        try
        {
            string? savedFileName = SelectedReportType switch
            {
                ReportType.SalesByPeriod => await GenerateSalesByPeriodAsync(),
                ReportType.RevenueByPaymentMethod => await GenerateRevenueByPaymentMethodAsync(),
                ReportType.StockPosition => await GenerateStockPositionAsync(),
                _ => null
            };

            if (savedFileName != null)
            {
                SetStatus(string.Format(Res.Reports_Msg_Success, savedFileName), isError: false);
            }
            else
            {
                SetStatus(string.Empty, isError: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Reports_Msg_Error, ex.Message), isError: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<string?> GenerateSalesByPeriodAsync()
    {
        var (start, end) = GetDateRange();
        var data = await _salesReportQuery.GetSalesByPeriodAsync(start, end);
        var rows = data.ToList();

        if (rows.Count == 0)
        {
            SetStatus(Res.Reports_Msg_NoData, isError: false);
            return null;
        }

        var columns = new CsvColumnDefinition<PDV.Shared.DTOs.SalesByPeriodRow>[]
        {
            new("Data", r => r.Date.ToString("dd/MM/yyyy")),
            new("Total Vendas", r => r.TotalSales.ToString()),
            new("Concluidas", r => r.CompletedSales.ToString()),
            new("Canceladas", r => r.CancelledSales.ToString()),
            new("Faturamento Bruto", r => r.GrossRevenue.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Descontos", r => r.Discounts.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Faturamento Liquido", r => r.NetRevenue.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Ticket Medio", r => r.AverageTicket.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")))
        };

        var fileName = $"vendas_periodo_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
        return await _csvExportService.ExportToCsvAsync(rows, columns, fileName);
    }

    private async Task<string?> GenerateRevenueByPaymentMethodAsync()
    {
        var (start, end) = GetDateRange();
        var data = await _salesReportQuery.GetRevenueByPaymentMethodAsync(start, end);
        var rows = data.ToList();

        if (rows.Count == 0)
        {
            SetStatus(Res.Reports_Msg_NoData, isError: false);
            return null;
        }

        var columns = new CsvColumnDefinition<PDV.Shared.DTOs.RevenueByPaymentMethodRow>[]
        {
            new("Forma de Pagamento", r => r.PaymentMethodName),
            new("Qtd Transacoes", r => r.TransactionCount.ToString()),
            new("Valor Total", r => r.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Percentual (%)", r => r.Percentage.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")))
        };

        var fileName = $"receita_pagamento_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
        return await _csvExportService.ExportToCsvAsync(rows, columns, fileName);
    }

    private async Task<string?> GenerateStockPositionAsync()
    {
        var data = await _stockReportQuery.GetCurrentStockPositionAsync();
        var rows = data.ToList();

        if (rows.Count == 0)
        {
            SetStatus(Res.Reports_Msg_NoData, isError: false);
            return null;
        }

        var columns = new CsvColumnDefinition<PDV.Shared.DTOs.StockPositionRow>[]
        {
            new("Codigo de Barras", r => r.Barcode),
            new("Descricao", r => r.Description),
            new("Unidade", r => r.UnitOfMeasure),
            new("Preco Unitario", r => r.UnitPrice.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Qtd Estoque", r => r.StockQuantity.ToString("N3", CultureInfo.GetCultureInfo("pt-BR"))),
            new("Valor Estoque", r => r.StockValue.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")))
        };

        var fileName = $"posicao_estoque_{DateTime.Now:yyyyMMdd}.csv";
        return await _csvExportService.ExportToCsvAsync(rows, columns, fileName);
    }

    private (DateTime start, DateTime end) GetDateRange()
    {
        var start = StartDate?.Date ?? DateTime.Today;
        var end = (EndDate?.Date ?? DateTime.Today).AddDays(1);
        return (start, end);
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
