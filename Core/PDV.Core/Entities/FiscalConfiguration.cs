using PDV.Core.Entities.Base;

namespace PDV.Core.Entities;

public class FiscalConfiguration : Entity
{
    public string TaxId { get; private set; } = string.Empty;              // CNPJ
    public string LegalName { get; private set; } = string.Empty;          // Razão Social
    public string TradeName { get; private set; } = string.Empty;          // Nome Fantasia
    public string StateRegistration { get; private set; } = string.Empty;  // Inscrição Estadual
    public string State { get; private set; } = string.Empty;              // UF (2 chars)
    public string CityCode { get; private set; } = string.Empty;           // Código IBGE (7 digits)
    public string Address { get; private set; } = string.Empty;
    public string AddressNumber { get; private set; } = string.Empty;
    public string? Neighborhood { get; private set; }
    public string ZipCode { get; private set; } = string.Empty;            // CEP
    public int TaxRegime { get; private set; }                              // 1=Simples, 2=Simples Excesso, 3=Normal
    public int Series { get; private set; }                                 // Série NFC-e
    public int NextNumber { get; private set; }                             // Próximo número
    public string? CertificatePath { get; private set; }
    public string? CertificatePassword { get; private set; }                // Should be encrypted
    public string? CscToken { get; private set; }                           // Código de Segurança do Contribuinte
    public string? CscId { get; private set; }                              // CSC Id
    public bool IsProduction { get; private set; }                          // true = Production, false = Homologation
    public bool IsActive { get; private set; }

    private FiscalConfiguration() { }

    public FiscalConfiguration(
        string taxId,
        string legalName,
        string tradeName,
        string stateRegistration,
        string state,
        string cityCode,
        string address,
        string addressNumber,
        string zipCode,
        int taxRegime,
        int series = 1,
        int nextNumber = 1)
    {
        SetTaxId(taxId);
        LegalName = legalName;
        TradeName = tradeName;
        StateRegistration = stateRegistration;
        State = state.ToUpperInvariant();
        CityCode = cityCode;
        Address = address;
        AddressNumber = addressNumber;
        ZipCode = zipCode;
        TaxRegime = taxRegime;
        Series = series;
        NextNumber = nextNumber;
        IsActive = true;
    }

    public void SetTaxId(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            throw new ArgumentException("TaxId (CNPJ) cannot be empty", nameof(taxId));

        TaxId = taxId.Replace(".", "").Replace("/", "").Replace("-", "");
        SetUpdatedAt();
    }

    public void SetCertificate(string path, string password)
    {
        CertificatePath = path;
        CertificatePassword = password;
        SetUpdatedAt();
    }

    public void SetCsc(string token, string id)
    {
        CscToken = token;
        CscId = id;
        SetUpdatedAt();
    }

    public void SetEnvironment(bool isProduction)
    {
        IsProduction = isProduction;
        SetUpdatedAt();
    }

    public void SetAddress(string address, string number, string? neighborhood, string zipCode)
    {
        Address = address;
        AddressNumber = number;
        Neighborhood = neighborhood;
        ZipCode = zipCode.Replace("-", "");
        SetUpdatedAt();
    }

    public void SetTradeName(string tradeName)
    {
        TradeName = tradeName;
        SetUpdatedAt();
    }

    public void SetSeries(int series)
    {
        if (series < 1)
            throw new ArgumentException("Series must be at least 1", nameof(series));

        Series = series;
        SetUpdatedAt();
    }

    public int GetNextNumberAndIncrement()
    {
        var current = NextNumber;
        NextNumber++;
        SetUpdatedAt();
        return current;
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
