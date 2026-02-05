namespace PDV.Shared.Enums;

public enum FiscalStatus
{
    None = 0,           // Sale without fiscal document (e.g., quote)
    Pending = 1,        // Awaiting issuance
    Authorized = 2,     // Authorized by SEFAZ
    Contingency = 3,    // Issued in offline contingency mode
    Cancelled = 4,      // Cancelled
    Rejected = 5        // Rejected by SEFAZ
}
