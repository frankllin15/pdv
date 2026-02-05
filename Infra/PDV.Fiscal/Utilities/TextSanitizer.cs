using System.Globalization;
using System.Text;

namespace PDV.Fiscal.Utilities;

/// <summary>
/// Utility class for sanitizing text for XML/NFC-e compatibility.
/// Removes accents, special characters, and normalizes text.
/// </summary>
public static class TextSanitizer
{
    /// <summary>
    /// Removes accents and diacritical marks from text.
    /// </summary>
    public static string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Sanitizes text for NFC-e XML content.
    /// Removes accents, special characters, and limits length.
    /// </summary>
    public static string SanitizeForXml(string text, int maxLength = 0)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove accents
        var result = RemoveAccents(text);

        // Replace special characters
        result = result
            .Replace("&", "E")
            .Replace("<", " ")
            .Replace(">", " ")
            .Replace("\"", " ")
            .Replace("'", " ")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        // Remove multiple spaces
        while (result.Contains("  "))
        {
            result = result.Replace("  ", " ");
        }

        result = result.Trim();

        // Limit length if specified
        if (maxLength > 0 && result.Length > maxLength)
        {
            result = result[..maxLength];
        }

        return result;
    }

    /// <summary>
    /// Sanitizes product description for NFC-e (max 120 chars).
    /// </summary>
    public static string SanitizeProductDescription(string description)
    {
        return SanitizeForXml(description, 120);
    }

    /// <summary>
    /// Sanitizes company name for NFC-e (max 60 chars).
    /// </summary>
    public static string SanitizeCompanyName(string name)
    {
        return SanitizeForXml(name, 60);
    }

    /// <summary>
    /// Sanitizes address for NFC-e (max 60 chars).
    /// </summary>
    public static string SanitizeAddress(string address)
    {
        return SanitizeForXml(address, 60);
    }

    /// <summary>
    /// Formats a number with specified decimal places for XML.
    /// </summary>
    public static string FormatDecimal(decimal value, int decimalPlaces = 2)
    {
        return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a quantity value (up to 4 decimal places).
    /// </summary>
    public static string FormatQuantity(decimal quantity)
    {
        return FormatDecimal(quantity, 4);
    }

    /// <summary>
    /// Formats a monetary value (2 decimal places).
    /// </summary>
    public static string FormatMoney(decimal value)
    {
        return FormatDecimal(value, 2);
    }

    /// <summary>
    /// Removes all non-numeric characters from a string.
    /// </summary>
    public static string OnlyNumbers(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return new string(text.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    /// Pads a number with leading zeros.
    /// </summary>
    public static string PadNumber(int number, int totalWidth)
    {
        return number.ToString().PadLeft(totalWidth, '0');
    }
}
