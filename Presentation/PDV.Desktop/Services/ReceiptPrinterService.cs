using System.Diagnostics;
using System.Runtime.InteropServices;
using PDV.Core.Interfaces.Services;
using PDV.Desktop.Utilities;

namespace PDV.Desktop.Services;

/// <summary>
/// Receipt printer service implementation for desktop.
/// Uses raw text printing for thermal printers.
/// </summary>
public class ReceiptPrinterService : IReceiptPrinterService
{
    private const string ReceiptFileName = "receipt.txt";

    public string? DefaultPrinterName { get; set; }

    public Task<IEnumerable<string>> GetAvailablePrintersAsync()
    {
        var printers = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // Use PowerShell to get printer list on Windows
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Get-Printer | Select-Object -ExpandProperty Name\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    printers.AddRange(output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p)));
                }
            }
            catch
            {
                // Fallback: return empty list
            }
        }

        return Task.FromResult<IEnumerable<string>>(printers);
    }

    public async Task<bool> PrintAsync(string content, string? printerName = null)
    {
        var printer = printerName ?? DefaultPrinterName;

        if (string.IsNullOrEmpty(printer))
        {
            return await PrintWithDialogAsync(content);
        }

        return await PrintToSpecificPrinterAsync(content, printer);
    }

    public async Task<bool> PrintToDefaultAsync(string content)
    {
        if (!string.IsNullOrEmpty(DefaultPrinterName))
        {
            return await PrintToSpecificPrinterAsync(content, DefaultPrinterName);
        }

        return await PrintWithDialogAsync(content);
    }

    public Task<bool> PrintWithDialogAsync(string content)
    {
        try
        {
            // Save to temp file and open with default application
            var tempPath = Path.Combine(Path.GetTempPath(), ReceiptFileName);
            File.WriteAllText(tempPath, content);

            var startInfo = new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true,
                Verb = "print"
            };

            Process.Start(startInfo);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<bool> IsPrinterAvailableAsync(string printerName)
    {
        var printers = await GetAvailablePrintersAsync();
        return printers.Contains(printerName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> PrintPdfAsync(byte[] pdfContent, string? printerName = null)
    {
        try
        {
            // Save PDF to temp file and print
            var tempPath = await SavePdfToTempAsync(pdfContent, "danfe_print.pdf");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use SumatraPDF or Adobe Reader command line to print silently
                // Fallback to opening with default PDF viewer's print dialog
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                    Verb = "print"
                };

                Process.Start(startInfo);
                return true;
            }

            // For other platforms, open with default viewer
            await OpenPdfAsync(pdfContent, "danfe_print.pdf");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> OpenPdfAsync(byte[] pdfContent, string? fileName = null)
    {
        var tempPath = await SavePdfToTempAsync(pdfContent, fileName ?? "danfe.pdf");

        var startInfo = new ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
        return tempPath;
    }

    public async Task SavePdfAsync(byte[] pdfContent, string filePath)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(filePath, pdfContent);
    }

    private static async Task<string> SavePdfToTempAsync(byte[] pdfContent, string fileName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PDV");
        Directory.CreateDirectory(tempDir);

        var tempPath = Path.Combine(tempDir, fileName);
        await File.WriteAllBytesAsync(tempPath, pdfContent);

        return tempPath;
    }

    private Task<bool> PrintToSpecificPrinterAsync(string content, string printerName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return PrintWindowsAsync(content, printerName);
        }

        // Fallback for other platforms: save to file
        return PrintWithDialogAsync(content);
    }

    private Task<bool> PrintWindowsAsync(string content, string printerName)
    {
        try
        {
            // Save content to temp file
            var tempPath = Path.Combine(Path.GetTempPath(), ReceiptFileName);
            File.WriteAllText(tempPath, content);

            // Use notepad to print (works with most thermal printers)
            var startInfo = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"/p \"{tempPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000); // Wait max 5 seconds

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

/// <summary>
/// Extended receipt printer service with ESC/POS support for thermal printers.
/// Supports native QR Code printing on compatible printers.
/// </summary>
public class ThermalPrinterService : ReceiptPrinterService, IThermalPrinterService
{
    /// <summary>
    /// Prints raw ESC/POS commands to a thermal printer.
    /// </summary>
    /// <param name="content">Text content</param>
    /// <param name="printerName">Printer name or COM port</param>
    public async Task<bool> PrintRawAsync(string content, string printerName)
    {
        try
        {
            var builder = new EscPosBuilder()
                .Initialize();

            // Parse and build ESC/POS document from text
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("==="))
                    builder.Separator('=');
                else if (line.Contains("---"))
                    builder.Separator('-');
                else
                    builder.TextLine(line.TrimEnd('\r'));
            }

            builder.LineFeed(3).FeedAndCut();

            return await PrintRawBytesAsync(builder.Build(), printerName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Prints a complete DANFE NFC-e with QR Code using ESC/POS commands.
    /// </summary>
    /// <param name="danfeText">DANFE text content (with [QRCODE] marker)</param>
    /// <param name="qrCodeUrl">URL for QR Code (will be printed natively)</param>
    /// <param name="printerName">Printer name or COM port</param>
    /// <param name="qrCodeSize">QR Code module size (1-16, default: 6)</param>
    public async Task<bool> PrintDanfeWithQrCodeAsync(
        string danfeText,
        string? qrCodeUrl,
        string printerName,
        int qrCodeSize = 6)
    {
        try
        {
            var builder = new EscPosBuilder()
                .Initialize();

            var lines = danfeText.Split('\n');
            var qrCodePrinted = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r');

                // Check for QR Code marker
                if (trimmedLine.Contains("[QRCODE]") && !string.IsNullOrEmpty(qrCodeUrl) && !qrCodePrinted)
                {
                    builder.LineFeed();
                    builder.QrCode(qrCodeUrl, qrCodeSize, QrErrorCorrection.M, QrModel.Model2);
                    builder.LineFeed();
                    qrCodePrinted = true;
                    continue;
                }

                // Handle formatting markers
                if (trimmedLine.Contains("================================"))
                {
                    builder.Separator('=');
                }
                else if (trimmedLine.Contains("------------------------------------------------") || trimmedLine.Contains("---"))
                {
                    builder.Separator('-');
                }
                else if (trimmedLine.Contains("** REIMPRESSAO"))
                {
                    builder.Bold().CenteredLine(trimmedLine).Bold(false);
                }
                else if (trimmedLine.Contains("DANFE NFC-e") || trimmedLine.Contains("VALOR TOTAL"))
                {
                    builder.Bold().TextLine(trimmedLine).Bold(false);
                }
                else if (trimmedLine.StartsWith("http"))
                {
                    // Skip URL lines (QR Code already printed)
                    continue;
                }
                else
                {
                    builder.TextLine(trimmedLine);
                }
            }

            builder.LineFeed(4).FeedAndCut();

            return await PrintRawBytesAsync(builder.Build(), printerName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Prints raw bytes to the printer using Windows RAW printing.
    /// </summary>
    public async Task<bool> PrintRawBytesAsync(byte[] data, string printerName)
    {
        if (printerName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        {
            return await PrintToSerialPortAsync(data, printerName);
        }
        
        await File.WriteAllBytesAsync("C:\\temp\\debug_nfe.bin", data);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await PrintToWindowsPrinterAsync(data, printerName);
        }

        return await PrintToUnixPrinterAsync(data, printerName);
    }

    /// <summary>
    /// Tests if the printer supports native QR Code by sending a test pattern.
    /// </summary>
    public async Task<bool> TestQrCodeSupportAsync(string printerName)
    {
        try
        {
            var builder = new EscPosBuilder()
                .Initialize()
                .CenteredLine("TESTE QR CODE")
                .LineFeed()
                .QrCode("https://www.nfe.fazenda.gov.br", 4)
                .LineFeed(2)
                .CenteredLine("Se o QR Code acima esta visivel,")
                .CenteredLine("sua impressora suporta QR Code nativo.")
                .LineFeed(3)
                .FeedAndCut();

            return await PrintRawBytesAsync(builder.Build(), printerName);
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> PrintToSerialPortAsync(byte[] data, string portName)
    {
        // Note: Requires System.IO.Ports package for full implementation
        // This is a basic implementation using file write to COM port
        try
        {
            await File.WriteAllBytesAsync($@"\\.\{portName}", data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> PrintToWindowsPrinterAsync(byte[] data, string printerName)
    {
        // try
        // {
        //     // Method 1: Using Windows spooler via temp file and copy command
        //     var tempFile = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}.bin");
        //     await File.WriteAllBytesAsync(tempFile, data);
        //
        //     try
        //     {
        //         // Try direct RAW printing using Windows print command
        //         var startInfo = new ProcessStartInfo
        //         {
        //             FileName = "cmd.exe",
        //             Arguments = $"/c copy /b \"{tempFile}\" \"\\\\.\\{printerName}\"",
        //             UseShellExecute = false,
        //             CreateNoWindow = true,
        //             RedirectStandardOutput = true,
        //             RedirectStandardError = true
        //         };
        //
        //         using var process = Process.Start(startInfo);
        //         if (process != null)
        //         {
        //             await process.WaitForExitAsync();
        //             if (process.ExitCode == 0)
        //                 return true;
        //         }
        //
        //         // Fallback: Try using printer share path
        //         startInfo.Arguments = $"/c copy /b \"{tempFile}\" \"{printerName}\"";
        //         using var process2 = Process.Start(startInfo);
        //         if (process2 != null)
        //         {
        //             await process2.WaitForExitAsync();
        //             return process2.ExitCode == 0;
        //         }
        //
        //         return false;
        //     }
        //     finally
        //     {
        //         // Clean up temp file
        //         try { File.Delete(tempFile); } catch { }
        //     }
        // }
        // catch
        // {
        //     return false;
        // }
        
        return await Task.Run(() => RawPrinterHelper.SendBytesToPrinter(printerName, data, "Impressao NFE"));
    }

    private async Task<bool> PrintToUnixPrinterAsync(byte[] data, string printerName)
    {
        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}.bin");
            await File.WriteAllBytesAsync(tempFile, data);

            try
            {
                // Use lp command on Unix/Linux/macOS
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lp",
                    Arguments = $"-d \"{printerName}\" -o raw \"{tempFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }

                return false;
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Interface for thermal printer with ESC/POS support.
/// </summary>
public interface IThermalPrinterService : IReceiptPrinterService
{
    /// <summary>
    /// Prints DANFE with native QR Code using ESC/POS.
    /// </summary>
    Task<bool> PrintDanfeWithQrCodeAsync(string danfeText, string? qrCodeUrl, string printerName, int qrCodeSize = 6);

    /// <summary>
    /// Prints raw ESC/POS bytes.
    /// </summary>
    Task<bool> PrintRawBytesAsync(byte[] data, string printerName);

    /// <summary>
    /// Tests if printer supports native QR Code.
    /// </summary>
    Task<bool> TestQrCodeSupportAsync(string printerName);
}
