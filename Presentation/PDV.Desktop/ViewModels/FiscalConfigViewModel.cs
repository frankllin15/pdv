using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Repositories;
using PDV.Core.Interfaces.Services;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class FiscalConfigViewModel : ViewModelBase
{
    private readonly IFiscalConfigurationRepository _configRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalManager _fiscalManager;

    private FiscalConfiguration? _currentConfig;

    [ObservableProperty]
    private string _statusMessage = Res.Fiscal_Msg_Loading;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private bool _isLoaded;

    // Company Data
    [ObservableProperty]
    private string _taxId = string.Empty;

    [ObservableProperty]
    private string _legalName = string.Empty;

    [ObservableProperty]
    private string _tradeName = string.Empty;

    [ObservableProperty]
    private string _stateRegistration = string.Empty;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private string _cityCode = string.Empty;

    // Address
    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _addressNumber = string.Empty;

    [ObservableProperty]
    private string _neighborhood = string.Empty;

    [ObservableProperty]
    private string _zipCode = string.Empty;

    // NFC-e Parameters
    [ObservableProperty]
    private int _taxRegime = 1;

    [ObservableProperty]
    private int _series = 1;

    [ObservableProperty]
    private int _nextNumber = 1;

    [ObservableProperty]
    private string _cscToken = string.Empty;

    [ObservableProperty]
    private string _cscId = string.Empty;

    [ObservableProperty]
    private bool _isProduction;

    [ObservableProperty]
    private string _certificatePath = string.Empty;

    [ObservableProperty]
    private string _certificatePassword = string.Empty;

    // Tax Regime options for combo box
    public List<TaxRegimeOption> TaxRegimeOptions { get; } = new()
    {
        new TaxRegimeOption(1, Res.Fiscal_TaxRegime_SimplesNacional),
        new TaxRegimeOption(2, Res.Fiscal_TaxRegime_SimplesExcesso),
        new TaxRegimeOption(3, Res.Fiscal_TaxRegime_Normal)
    };

    // Brazilian states for combo box
    public List<string> StateOptions { get; } = new()
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO",
        "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI",
        "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public FiscalConfigViewModel(
        IFiscalConfigurationRepository configRepository,
        IUnitOfWork unitOfWork,
        IFiscalManager fiscalManager)
    {
        _configRepository = configRepository;
        _unitOfWork = unitOfWork;
        _fiscalManager = fiscalManager;
    }

    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        if (IsLoaded) return;

        try
        {
            SetStatus(Res.Fiscal_Msg_Loading, false);

            _currentConfig = await _configRepository.GetActiveAsync();

            if (_currentConfig != null)
            {
                MapFromEntity(_currentConfig);
                SetStatus(Res.Fiscal_Msg_Loaded, false);
            }
            else
            {
                SetStatus(Res.Fiscal_Msg_NoConfig, false);
            }

            IsLoaded = true;
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_LoadError, ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        if (!ValidateFields()) return;

        try
        {
            IsSaving = true;
            SetStatus(Res.Fiscal_Msg_Saving, false);

            if (_currentConfig == null)
            {
                // Create new
                _currentConfig = new FiscalConfiguration(
                    TaxId,
                    LegalName,
                    TradeName,
                    StateRegistration,
                    State,
                    CityCode,
                    Address,
                    AddressNumber,
                    ZipCode,
                    TaxRegime,
                    Series,
                    NextNumber);

                _currentConfig.SetCsc(CscToken, CscId);
                _currentConfig.SetEnvironment(IsProduction);

                if (!string.IsNullOrEmpty(CertificatePath) && !string.IsNullOrEmpty(CertificatePassword))
                {
                    _currentConfig.SetCertificate(CertificatePath, CertificatePassword);
                }

                await _configRepository.AddAsync(_currentConfig);
            }
            else
            {
                // Update existing
                _currentConfig.SetTaxId(TaxId);
                _currentConfig.SetTradeName(TradeName);
                _currentConfig.SetAddress(Address, AddressNumber, Neighborhood, ZipCode);
                _currentConfig.SetSeries(Series);
                _currentConfig.SetCsc(CscToken, CscId);
                _currentConfig.SetEnvironment(IsProduction);

                if (!string.IsNullOrEmpty(CertificatePath) && !string.IsNullOrEmpty(CertificatePassword))
                {
                    _currentConfig.SetCertificate(CertificatePath, CertificatePassword);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            SetStatus(Res.Fiscal_Msg_Saved, false);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Global_Msg_SaveError, ex.Message), true);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTesting = true;
            SetStatus(Res.Fiscal_Msg_TestingConnection, false);

            var isAvailable = await _fiscalManager.IsSefazAvailableAsync();

            if (isAvailable)
            {
                SetStatus(Res.Fiscal_Msg_ConnectionOk, false);
            }
            else
            {
                SetStatus(Res.Fiscal_Msg_SefazUnavailable, true);
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Fiscal_Msg_TestError, ex.Message), true);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool ValidateFields()
    {
        if (string.IsNullOrWhiteSpace(TaxId))
        {
            SetStatus(Res.Fiscal_Msg_TaxIdRequired, true);
            return false;
        }

        if (TaxId.Replace(".", "").Replace("/", "").Replace("-", "").Length != 14)
        {
            SetStatus(Res.Fiscal_Msg_TaxIdLength, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(LegalName))
        {
            SetStatus(Res.Fiscal_Msg_LegalNameRequired, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(TradeName))
        {
            SetStatus(Res.Fiscal_Msg_TradeNameRequired, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(State) || State.Length != 2)
        {
            SetStatus(Res.Fiscal_Msg_StateRequired, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(CityCode) || CityCode.Length != 7)
        {
            SetStatus(Res.Fiscal_Msg_CityCodeLength, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            SetStatus(Res.Fiscal_Msg_AddressRequired, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(AddressNumber))
        {
            SetStatus(Res.Fiscal_Msg_NumberRequired, true);
            return false;
        }

        if (string.IsNullOrWhiteSpace(ZipCode))
        {
            SetStatus(Res.Fiscal_Msg_ZipCodeRequired, true);
            return false;
        }

        if (Series < 1)
        {
            SetStatus(Res.Fiscal_Msg_SeriesRequired, true);
            return false;
        }

        return true;
    }

    private void MapFromEntity(FiscalConfiguration config)
    {
        TaxId = config.TaxId;
        LegalName = config.LegalName;
        TradeName = config.TradeName;
        StateRegistration = config.StateRegistration;
        State = config.State;
        CityCode = config.CityCode;
        Address = config.Address;
        AddressNumber = config.AddressNumber;
        Neighborhood = config.Neighborhood ?? string.Empty;
        ZipCode = config.ZipCode;
        TaxRegime = config.TaxRegime;
        Series = config.Series;
        NextNumber = config.NextNumber;
        CscToken = config.CscToken ?? string.Empty;
        CscId = config.CscId ?? string.Empty;
        IsProduction = config.IsProduction;
        CertificatePath = config.CertificatePath ?? string.Empty;
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}

public record TaxRegimeOption(int Value, string Description);
