using System.Diagnostics;
using System.Runtime.InteropServices;
using PDV.Core.Interfaces.Services;

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
/// </summary>
public class ThermalPrinterService : ReceiptPrinterService
{
    // ESC/POS Commands
    private static readonly byte[] Initialize = { 0x1B, 0x40 }; // ESC @
    private static readonly byte[] CutPaper = { 0x1D, 0x56, 0x00 }; // GS V 0
    private static readonly byte[] LineFeed = { 0x0A }; // LF
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 }; // ESC E 1
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 }; // ESC E 0
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 }; // ESC a 1
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 }; // ESC a 0
    private static readonly byte[] DoubleHeight = { 0x1B, 0x21, 0x10 }; // ESC ! 16
    private static readonly byte[] NormalSize = { 0x1B, 0x21, 0x00 }; // ESC ! 0

    /// <summary>
    /// Prints raw ESC/POS commands to a thermal printer via USB/COM port.
    /// </summary>
    /// <param name="content">Text content</param>
    /// <param name="portName">COM port or printer share name</param>
    public async Task<bool> PrintRawAsync(string content, string portName)
    {
        try
        {
            var bytes = BuildEscPosDocument(content);

            if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                // Serial port printing
                return await PrintToSerialPortAsync(bytes, portName);
            }
            else
            {
                // Windows printer share
                return await PrintToWindowsShareAsync(bytes, portName);
            }
        }
        catch
        {
            return false;
        }
    }

    private byte[] BuildEscPosDocument(string content)
    {
        var result = new List<byte>();

        // Initialize printer
        result.AddRange(Initialize);

        // Convert text to bytes (CP850 for Portuguese characters)
        var encoding = System.Text.Encoding.GetEncoding(850);
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            // Check for formatting hints in the text
            if (line.Contains("===") || line.Contains("---"))
            {
                result.AddRange(encoding.GetBytes(line.Replace("===", "").Replace("---", "")));
            }
            else
            {
                result.AddRange(encoding.GetBytes(line));
            }
            result.AddRange(LineFeed);
        }

        // Add some line feeds and cut paper
        result.AddRange(LineFeed);
        result.AddRange(LineFeed);
        result.AddRange(LineFeed);
        result.AddRange(CutPaper);

        return result.ToArray();
    }

    private async Task<bool> PrintToSerialPortAsync(byte[] data, string portName)
    {
        // Note: For production, use System.IO.Ports.SerialPort
        // This is a simplified version
        await Task.CompletedTask;
        return false; // Not implemented - requires System.IO.Ports package
    }

    private async Task<bool> PrintToWindowsShareAsync(byte[] data, string printerName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            // Write to temp file and use copy command to send to printer
            var tempFile = Path.Combine(Path.GetTempPath(), "receipt_raw.bin");
            await File.WriteAllBytesAsync(tempFile, data);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c copy /b \"{tempFile}\" \"{printerName}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000);

            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
