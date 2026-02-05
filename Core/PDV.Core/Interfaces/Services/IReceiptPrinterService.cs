namespace PDV.Core.Interfaces.Services;

/// <summary>
/// Service for printing receipts (DANFE NFC-e).
/// </summary>
public interface IReceiptPrinterService
{
    /// <summary>
    /// Gets the list of available printers on the system.
    /// </summary>
    Task<IEnumerable<string>> GetAvailablePrintersAsync();

    /// <summary>
    /// Gets or sets the default printer name for receipts.
    /// </summary>
    string? DefaultPrinterName { get; set; }

    /// <summary>
    /// Prints text content to the specified printer.
    /// </summary>
    /// <param name="content">Text content to print</param>
    /// <param name="printerName">Printer name (uses default if null)</param>
    /// <returns>True if printing was successful</returns>
    Task<bool> PrintAsync(string content, string? printerName = null);

    /// <summary>
    /// Prints text content to the default printer.
    /// </summary>
    /// <param name="content">Text content to print</param>
    /// <returns>True if printing was successful</returns>
    Task<bool> PrintToDefaultAsync(string content);

    /// <summary>
    /// Opens the system print dialog for the given content.
    /// </summary>
    /// <param name="content">Text content to print</param>
    /// <returns>True if user confirmed printing</returns>
    Task<bool> PrintWithDialogAsync(string content);

    /// <summary>
    /// Checks if a printer is available and ready.
    /// </summary>
    /// <param name="printerName">Printer name to check</param>
    /// <returns>True if printer is available</returns>
    Task<bool> IsPrinterAvailableAsync(string printerName);

    /// <summary>
    /// Prints a PDF document.
    /// </summary>
    /// <param name="pdfContent">PDF content as byte array</param>
    /// <param name="printerName">Printer name (uses default if null)</param>
    /// <returns>True if printing was successful</returns>
    Task<bool> PrintPdfAsync(byte[] pdfContent, string? printerName = null);

    /// <summary>
    /// Opens a PDF for viewing/printing in the default PDF viewer.
    /// </summary>
    /// <param name="pdfContent">PDF content as byte array</param>
    /// <param name="fileName">Suggested file name for the PDF</param>
    /// <returns>The path where the PDF was saved</returns>
    Task<string> OpenPdfAsync(byte[] pdfContent, string? fileName = null);

    /// <summary>
    /// Saves a PDF to the specified path.
    /// </summary>
    /// <param name="pdfContent">PDF content as byte array</param>
    /// <param name="filePath">Full path to save the PDF</param>
    Task SavePdfAsync(byte[] pdfContent, string filePath);
}
