namespace PDV.Fiscal.Exceptions;

/// <summary>
/// Exception thrown when a fiscal operation fails.
/// </summary>
public class FiscalException : Exception
{
    /// <summary>
    /// SEFAZ status code (cStat).
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Whether the error is recoverable (can retry).
    /// </summary>
    public bool IsRecoverable { get; }

    public FiscalException(string message)
        : base(message)
    {
        IsRecoverable = false;
    }

    public FiscalException(string message, int statusCode, bool isRecoverable = false)
        : base(message)
    {
        StatusCode = statusCode;
        IsRecoverable = isRecoverable;
    }

    public FiscalException(string message, Exception innerException)
        : base(message, innerException)
    {
        IsRecoverable = false;
    }

    public FiscalException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        IsRecoverable = false;
    }

    /// <summary>
    /// Creates an exception for configuration errors.
    /// </summary>
    public static FiscalException ConfigurationError(string detail)
    {
        return new FiscalException($"Fiscal configuration error: {detail}");
    }

    /// <summary>
    /// Creates an exception for certificate errors.
    /// </summary>
    public static FiscalException CertificateError(string detail)
    {
        return new FiscalException($"Certificate error: {detail}");
    }

    /// <summary>
    /// Creates an exception for validation errors.
    /// </summary>
    public static FiscalException ValidationError(string detail)
    {
        return new FiscalException($"Validation error: {detail}");
    }

    /// <summary>
    /// Creates an exception for SEFAZ rejection.
    /// </summary>
    public static FiscalException Rejected(int statusCode, string message)
    {
        return new FiscalException($"SEFAZ rejection [{statusCode}]: {message}", statusCode, false);
    }

    /// <summary>
    /// Creates an exception for service unavailability (recoverable).
    /// </summary>
    public static FiscalException ServiceUnavailable(string detail)
    {
        return new FiscalException($"Service unavailable: {detail}", 0, true);
    }
}
