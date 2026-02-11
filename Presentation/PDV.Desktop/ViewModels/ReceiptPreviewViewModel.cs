using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Interfaces.Services;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class ReceiptPreviewViewModel : ViewModelBase
{
    private readonly IReceiptPrinterService _printerService;

    [ObservableProperty]
    private string _receiptContent = string.Empty;

    [ObservableProperty]
    private string _title = Res.Receipt_Lbl_Title;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isPrinting;

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private int _saleNumber;

    [ObservableProperty]
    private string _fiscalNumber = string.Empty;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private bool _isFiscalAuthorized;

    [ObservableProperty]
    private bool _isContingency;

    public string SaleNumberDisplay => string.Format(Res.Receipt_Lbl_SaleNumber, SaleNumber);
    public string TotalDisplay => string.Format(Res.Receipt_Lbl_TotalFormat, Total);

    partial void OnSaleNumberChanged(int value) => OnPropertyChanged(nameof(SaleNumberDisplay));
    partial void OnTotalChanged(decimal value) => OnPropertyChanged(nameof(TotalDisplay));

    public ObservableCollection<string> AvailablePrinters { get; } = new();

    public event EventHandler? CloseRequested;

    public ReceiptPreviewViewModel(IReceiptPrinterService printerService)
    {
        _printerService = printerService;
        NotifyOnCultureChanged(nameof(SaleNumberDisplay), nameof(TotalDisplay));
    }

    [RelayCommand]
    private async Task LoadPrintersAsync()
    {
        try
        {
            var printers = await _printerService.GetAvailablePrintersAsync();

            AvailablePrinters.Clear();
            foreach (var printer in printers)
            {
                AvailablePrinters.Add(printer);
            }

            // Select default printer if available
            if (!string.IsNullOrEmpty(_printerService.DefaultPrinterName) &&
                AvailablePrinters.Contains(_printerService.DefaultPrinterName))
            {
                SelectedPrinter = _printerService.DefaultPrinterName;
            }
            else if (AvailablePrinters.Count > 0)
            {
                SelectedPrinter = AvailablePrinters[0];
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_LoadPrintersError, ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (string.IsNullOrEmpty(ReceiptContent))
        {
            SetStatus(Res.Receipt_Msg_NoContent, true);
            return;
        }

        try
        {
            IsPrinting = true;
            SetStatus(Res.Global_Msg_Printing, false);

            bool success;
            if (!string.IsNullOrEmpty(SelectedPrinter))
            {
                success = await _printerService.PrintAsync(ReceiptContent, SelectedPrinter);
            }
            else
            {
                success = await _printerService.PrintWithDialogAsync(ReceiptContent);
            }

            if (success)
            {
                SetStatus(Res.Global_Msg_PrintSent, false);
            }
            else
            {
                SetStatus(Res.Global_Msg_PrintFailed, true);
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_PrintError, ex.Message), true);
        }
        finally
        {
            IsPrinting = false;
        }
    }

    [RelayCommand]
    private async Task PrintAndCloseAsync()
    {
        await PrintAsync();

        if (!IsError)
        {
            await Task.Delay(500); // Brief delay to show success message
            Close();
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task SaveToFileAsync()
    {
        try
        {
            var fileName = $"NFC-e_{FiscalNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, "PDV", "Recibos", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await File.WriteAllTextAsync(filePath, ReceiptContent);

            SetStatus(string.Format(Res.Global_Msg_SavedAt, filePath), false);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_SaveError, ex.Message), true);
        }
    }

    public void SetReceiptData(
        string content,
        int saleNumber,
        string fiscalNumber,
        decimal total,
        bool isAuthorized,
        bool isContingency)
    {
        ReceiptContent = content;
        SaleNumber = saleNumber;
        FiscalNumber = fiscalNumber;
        Total = total;
        IsFiscalAuthorized = isAuthorized;
        IsContingency = isContingency;

        Title = isContingency
            ? string.Format(Res.Receipt_Msg_NfceContingency, fiscalNumber)
            : string.Format(Res.Receipt_Msg_NfceNormal, fiscalNumber);
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
