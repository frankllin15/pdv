namespace PDV.Shared.DTOs;

public record FiscalConfigurationDto
{
    public Guid Id { get; init; }
    public string TaxId { get; init; } = string.Empty;              // CNPJ
    public string LegalName { get; init; } = string.Empty;          // Razão Social
    public string TradeName { get; init; } = string.Empty;          // Nome Fantasia
    public string StateRegistration { get; init; } = string.Empty;  // Inscrição Estadual
    public string State { get; init; } = string.Empty;              // UF
    public string CityCode { get; init; } = string.Empty;           // Código IBGE
    public string Address { get; init; } = string.Empty;
    public string AddressNumber { get; init; } = string.Empty;
    public string? Neighborhood { get; init; }
    public string ZipCode { get; init; } = string.Empty;            // CEP
    public int TaxRegime { get; init; }                              // 1=Simples, 2=Simples Excesso, 3=Normal
    public int Series { get; init; }                                 // Série NFC-e
    public int NextNumber { get; init; }                             // Próximo número
    public string? CscToken { get; init; }                           // CSC Token
    public string? CscId { get; init; }                              // CSC Id
    public bool IsProduction { get; init; }                          // Ambiente
    public bool IsActive { get; init; }
}

public record CreateFiscalConfigurationRequest
{
    public string TaxId { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string StateRegistration { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string CityCode { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string AddressNumber { get; init; } = string.Empty;
    public string? Neighborhood { get; init; }
    public string ZipCode { get; init; } = string.Empty;
    public int TaxRegime { get; init; } = 1;
    public int Series { get; init; } = 1;
    public int NextNumber { get; init; } = 1;
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
    public string? CscToken { get; init; }
    public string? CscId { get; init; }
    public bool IsProduction { get; init; }
}

public record UpdateFiscalConfigurationRequest
{
    public string? TradeName { get; init; }
    public string? Address { get; init; }
    public string? AddressNumber { get; init; }
    public string? Neighborhood { get; init; }
    public string? ZipCode { get; init; }
    public int? Series { get; init; }
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
    public string? CscToken { get; init; }
    public string? CscId { get; init; }
    public bool? IsProduction { get; init; }
    public bool? IsActive { get; init; }
}
