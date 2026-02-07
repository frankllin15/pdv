using System.Text;

namespace PDV.Desktop.Services;

/// <summary>
/// Builder for ESC/POS commands for thermal printers.
/// Compatible with Epson, Elgin, Bematech, Daruma, and similar printers.
/// </summary>
public class EscPosBuilder
{
    private readonly List<byte> _buffer = new();
    private readonly Encoding _encoding;

    // Character encoding for Portuguese
    private const int CodePagePC850 = 850;  // Latin 1 (Western European)
    private const int CodePagePC860 = 860;  // Portuguese

    public EscPosBuilder(int codePage = CodePagePC850)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = Encoding.GetEncoding(codePage);
    }

    #region Basic Commands

    /// <summary>
    /// Initialize printer (ESC @)
    /// </summary>
    public EscPosBuilder Initialize()
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x40 });
        return this;
    }

    /// <summary>
    /// Line feed (LF)
    /// </summary>
    public EscPosBuilder LineFeed(int lines = 1)
    {
        for (int i = 0; i < lines; i++)
        {
            _buffer.Add(0x0A);
        }
        return this;
    }

    /// <summary>
    /// Carriage return (CR)
    /// </summary>
    public EscPosBuilder CarriageReturn()
    {
        _buffer.Add(0x0D);
        return this;
    }

    /// <summary>
    /// Cut paper (GS V)
    /// </summary>
    /// <param name="fullCut">True for full cut, false for partial cut</param>
    public EscPosBuilder Cut(bool fullCut = false)
    {
        _buffer.AddRange(new byte[] { 0x1D, 0x56, (byte)(fullCut ? 0x00 : 0x01) });
        return this;
    }

    /// <summary>
    /// Feed and cut paper (GS V m n)
    /// </summary>
    /// <param name="feedLines">Lines to feed before cut (0-255)</param>
    public EscPosBuilder FeedAndCut(int feedLines = 3)
    {
        _buffer.AddRange(new byte[] { 0x1D, 0x56, 0x42, (byte)Math.Min(255, Math.Max(0, feedLines)) });
        return this;
    }

    /// <summary>
    /// Open cash drawer (ESC p m t1 t2)
    /// </summary>
    public EscPosBuilder OpenCashDrawer()
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
        return this;
    }

    /// <summary>
    /// Beep (ESC B n t) - Not supported by all printers
    /// </summary>
    public EscPosBuilder Beep(int times = 1, int duration = 2)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x42, (byte)times, (byte)duration });
        return this;
    }

    #endregion

    #region Text Formatting

    /// <summary>
    /// Set text alignment (ESC a n)
    /// </summary>
    public EscPosBuilder Align(TextAlignment alignment)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x61, (byte)alignment });
        return this;
    }

    /// <summary>
    /// Set bold mode (ESC E n)
    /// </summary>
    public EscPosBuilder Bold(bool on = true)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x45, (byte)(on ? 0x01 : 0x00) });
        return this;
    }

    /// <summary>
    /// Set underline mode (ESC - n)
    /// </summary>
    public EscPosBuilder Underline(UnderlineMode mode = UnderlineMode.Single)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x2D, (byte)mode });
        return this;
    }

    /// <summary>
    /// Set double strike mode (ESC G n)
    /// </summary>
    public EscPosBuilder DoubleStrike(bool on = true)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x47, (byte)(on ? 0x01 : 0x00) });
        return this;
    }

    /// <summary>
    /// Set font size (GS ! n)
    /// </summary>
    public EscPosBuilder FontSize(int widthMultiplier = 1, int heightMultiplier = 1)
    {
        // Width: bits 0-3, Height: bits 4-7
        int w = Math.Min(8, Math.Max(1, widthMultiplier)) - 1;
        int h = Math.Min(8, Math.Max(1, heightMultiplier)) - 1;
        byte n = (byte)((h << 4) | w);
        _buffer.AddRange(new byte[] { 0x1D, 0x21, n });
        return this;
    }

    /// <summary>
    /// Set font (ESC M n)
    /// </summary>
    public EscPosBuilder Font(PrinterFont font)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x4D, (byte)font });
        return this;
    }

    /// <summary>
    /// Set inverted (white on black) mode (GS B n)
    /// </summary>
    public EscPosBuilder Inverted(bool on = true)
    {
        _buffer.AddRange(new byte[] { 0x1D, 0x42, (byte)(on ? 0x01 : 0x00) });
        return this;
    }

    /// <summary>
    /// Set character code table (ESC t n)
    /// </summary>
    public EscPosBuilder CodeTable(int table)
    {
        _buffer.AddRange(new byte[] { 0x1B, 0x74, (byte)table });
        return this;
    }

    /// <summary>
    /// Reset to default formatting
    /// </summary>
    public EscPosBuilder ResetFormatting()
    {
        return Bold(false)
            .Underline(UnderlineMode.Off)
            .DoubleStrike(false)
            .FontSize(1, 1)
            .Align(TextAlignment.Left)
            .Inverted(false);
    }

    #endregion

    #region Text Output

    /// <summary>
    /// Write text
    /// </summary>
    public EscPosBuilder Text(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            // Normalize and sanitize text
            var sanitized = SanitizeText(text);
            _buffer.AddRange(_encoding.GetBytes(sanitized));
        }
        return this;
    }

    /// <summary>
    /// Write text with line feed
    /// </summary>
    public EscPosBuilder TextLine(string text)
    {
        return Text(text).LineFeed();
    }

    /// <summary>
    /// Write centered text with line feed
    /// </summary>
    public EscPosBuilder CenteredLine(string text)
    {
        return Align(TextAlignment.Center).TextLine(text).Align(TextAlignment.Left);
    }

    /// <summary>
    /// Write bold centered text with line feed
    /// </summary>
    public EscPosBuilder TitleLine(string text)
    {
        return Align(TextAlignment.Center).Bold().TextLine(text).Bold(false).Align(TextAlignment.Left);
    }

    /// <summary>
    /// Write a separator line
    /// </summary>
    public EscPosBuilder Separator(char character = '-', int width = 48)
    {
        return TextLine(new string(character, width));
    }

    /// <summary>
    /// Write two columns (left and right aligned)
    /// </summary>
    public EscPosBuilder TwoColumns(string left, string right, int totalWidth = 48)
    {
        var leftText = left ?? string.Empty;
        var rightText = right ?? string.Empty;

        int spaces = totalWidth - leftText.Length - rightText.Length;
        if (spaces < 1) spaces = 1;

        return TextLine(leftText + new string(' ', spaces) + rightText);
    }

    #endregion

    #region QR Code (Native ESC/POS)

    /// <summary>
    /// Print QR Code using native ESC/POS commands.
    /// Compatible with most modern thermal printers (Epson, Elgin, Bematech, etc.)
    /// </summary>
    /// <param name="data">Data to encode in QR Code (URL or text)</param>
    /// <param name="moduleSize">Module size (1-16, recommended: 4-8)</param>
    /// <param name="errorCorrection">Error correction level</param>
    /// <param name="model">QR Code model (1 or 2)</param>
    public EscPosBuilder QrCode(string data, int moduleSize = 6, QrErrorCorrection errorCorrection = QrErrorCorrection.M, QrModel model = QrModel.Model2)
    {
        if (string.IsNullOrEmpty(data))
            return this;

        var dataBytes = Encoding.UTF8.GetBytes(data);
        int dataLength = dataBytes.Length;

        // Center the QR Code
        Align(TextAlignment.Center);

        // Function 165: Select QR Code model
        // GS ( k pL pH cn fn n1 n2
        // cn = 49 (QR Code), fn = 65 (select model)
        _buffer.AddRange(new byte[] {
            0x1D, 0x28, 0x6B,  // GS ( k
            0x04, 0x00,        // pL pH (4 bytes follow)
            0x31,              // cn = 49 (QR Code)
            0x41,              // fn = 65 (select model)
            (byte)model,       // n1 = model (1 or 2)
            0x00               // n2 = 0
        });

        // Function 167: Set module size
        // GS ( k pL pH cn fn n
        _buffer.AddRange(new byte[] {
            0x1D, 0x28, 0x6B,  // GS ( k
            0x03, 0x00,        // pL pH (3 bytes follow)
            0x31,              // cn = 49 (QR Code)
            0x43,              // fn = 67 (set size)
            (byte)Math.Min(16, Math.Max(1, moduleSize))  // n = module size
        });

        // Function 169: Set error correction level
        // GS ( k pL pH cn fn n
        _buffer.AddRange(new byte[] {
            0x1D, 0x28, 0x6B,  // GS ( k
            0x03, 0x00,        // pL pH (3 bytes follow)
            0x31,              // cn = 49 (QR Code)
            0x45,              // fn = 69 (set error correction)
            (byte)(48 + (int)errorCorrection)  // n = '0' + level (48=L, 49=M, 50=Q, 51=H)
        });

        // Function 180: Store QR Code data
        // GS ( k pL pH cn fn m d1...dk
        int storeLength = dataLength + 3;
        byte pL = (byte)(storeLength % 256);
        byte pH = (byte)(storeLength / 256);

        _buffer.AddRange(new byte[] {
            0x1D, 0x28, 0x6B,  // GS ( k
            pL, pH,            // pL pH
            0x31,              // cn = 49 (QR Code)
            0x50,              // fn = 80 (store data)
            0x30               // m = 48 ('0')
        });
        _buffer.AddRange(dataBytes);

        // Function 181: Print stored QR Code
        // GS ( k pL pH cn fn m
        _buffer.AddRange(new byte[] {
            0x1D, 0x28, 0x6B,  // GS ( k
            0x03, 0x00,        // pL pH (3 bytes follow)
            0x31,              // cn = 49 (QR Code)
            0x51,              // fn = 81 (print)
            0x30               // m = 48 ('0')
        });

        // Return to left alignment
        Align(TextAlignment.Left);

        return this;
    }

    /// <summary>
    /// Print QR Code using alternative method (for older printers).
    /// Uses GS k command.
    /// </summary>
    public EscPosBuilder QrCodeAlternative(string data, int moduleSize = 6)
    {
        if (string.IsNullOrEmpty(data))
            return this;

        var dataBytes = Encoding.UTF8.GetBytes(data);

        Align(TextAlignment.Center);

        // Some older printers use this simpler command
        // GS k m d1...dk NUL
        _buffer.AddRange(new byte[] { 0x1D, 0x6B, 0x10 }); // GS k 16 (QR Code type)
        _buffer.AddRange(dataBytes);
        _buffer.Add(0x00); // NUL terminator

        Align(TextAlignment.Left);

        return this;
    }

    #endregion

    #region Barcode

    /// <summary>
    /// Print barcode (for Code128, EAN13, etc.)
    /// </summary>
    public EscPosBuilder Barcode(string data, BarcodeType type, int height = 50, int width = 2, bool showText = true)
    {
        if (string.IsNullOrEmpty(data))
            return this;

        Align(TextAlignment.Center);

        // Set barcode height (GS h n)
        _buffer.AddRange(new byte[] { 0x1D, 0x68, (byte)Math.Min(255, Math.Max(1, height)) });

        // Set barcode width (GS w n)
        _buffer.AddRange(new byte[] { 0x1D, 0x77, (byte)Math.Min(6, Math.Max(1, width)) });

        // Set HRI position (GS H n)
        _buffer.AddRange(new byte[] { 0x1D, 0x48, (byte)(showText ? 0x02 : 0x00) }); // 2 = below

        // Print barcode (GS k m n d1...dn)
        var dataBytes = Encoding.ASCII.GetBytes(data);
        _buffer.AddRange(new byte[] { 0x1D, 0x6B, (byte)type, (byte)dataBytes.Length });
        _buffer.AddRange(dataBytes);

        Align(TextAlignment.Left);

        return this;
    }

    #endregion

    #region Image Printing

    /// <summary>
    /// Print raster image (for QR Code as bitmap if native QR is not supported).
    /// </summary>
    /// <param name="imageData">Bitmap data (1 bit per pixel, MSB first)</param>
    /// <param name="widthBytes">Width in bytes (8 pixels per byte)</param>
    /// <param name="height">Height in pixels</param>
    public EscPosBuilder RasterImage(byte[] imageData, int widthBytes, int height)
    {
        if (imageData == null || imageData.Length == 0)
            return this;

        Align(TextAlignment.Center);

        // GS v 0 m xL xH yL yH d1...dk
        int xL = widthBytes % 256;
        int xH = widthBytes / 256;
        int yL = height % 256;
        int yH = height / 256;

        _buffer.AddRange(new byte[] {
            0x1D, 0x76, 0x30, 0x00,  // GS v 0 m (m=0: normal)
            (byte)xL, (byte)xH,      // width in bytes
            (byte)yL, (byte)yH       // height in dots
        });
        _buffer.AddRange(imageData);

        Align(TextAlignment.Left);

        return this;
    }

    #endregion

    #region Build

    /// <summary>
    /// Get the built command bytes
    /// </summary>
    public byte[] Build()
    {
        return _buffer.ToArray();
    }

    /// <summary>
    /// Get the current buffer length
    /// </summary>
    public int Length => _buffer.Count;

    /// <summary>
    /// Clear the buffer
    /// </summary>
    public EscPosBuilder Clear()
    {
        _buffer.Clear();
        return this;
    }

    #endregion

    #region Helper Methods

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Replace common problematic characters
        return text
            .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
            .Replace("ú", "u").Replace("ü", "u")
            .Replace("ç", "c")
            .Replace("Á", "A").Replace("À", "A").Replace("Ã", "A").Replace("Â", "A")
            .Replace("É", "E").Replace("Ê", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O").Replace("Ô", "O").Replace("Õ", "O")
            .Replace("Ú", "U").Replace("Ü", "U")
            .Replace("Ç", "C");
    }

    #endregion
}

#region Enums

public enum TextAlignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2
}

public enum UnderlineMode : byte
{
    Off = 0,
    Single = 1,
    Double = 2
}

public enum PrinterFont : byte
{
    FontA = 0,  // 12x24
    FontB = 1,  // 9x17
    FontC = 2   // Special (if supported)
}

public enum QrErrorCorrection
{
    L = 0,  // 7% recovery
    M = 1,  // 15% recovery
    Q = 2,  // 25% recovery
    H = 3   // 30% recovery
}

public enum QrModel : byte
{
    Model1 = 49,  // Original
    Model2 = 50   // Enhanced (recommended)
}

public enum BarcodeType : byte
{
    UPC_A = 65,
    UPC_E = 66,
    EAN13 = 67,
    EAN8 = 68,
    CODE39 = 69,
    ITF = 70,
    CODABAR = 71,
    CODE93 = 72,
    CODE128 = 73
}

#endregion
