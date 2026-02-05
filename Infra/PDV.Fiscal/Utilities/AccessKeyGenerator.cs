namespace PDV.Fiscal.Utilities;

/// <summary>
/// Generates NFC-e access keys (44 digits).
/// </summary>
public static class AccessKeyGenerator
{
    /// <summary>
    /// Generates an NFC-e access key.
    /// Format: cUF(2) + AAMM(4) + CNPJ(14) + mod(2) + serie(3) + nNF(9) + tpEmis(1) + cNF(8) + cDV(1)
    /// </summary>
    /// <param name="stateCode">IBGE state code (2 digits)</param>
    /// <param name="issueDate">Issue date (for AAMM)</param>
    /// <param name="cnpj">CNPJ (14 digits, no formatting)</param>
    /// <param name="model">Fiscal model (65 for NFC-e)</param>
    /// <param name="series">Invoice series (1-999)</param>
    /// <param name="number">Invoice number (1-999999999)</param>
    /// <param name="emissionType">Emission type (1=normal, 9=contingency)</param>
    /// <param name="numericCode">Random numeric code (8 digits)</param>
    /// <returns>44-digit access key</returns>
    public static string Generate(
        string stateCode,
        DateTime issueDate,
        string cnpj,
        int model,
        int series,
        int number,
        int emissionType,
        int numericCode)
    {
        // Build key without check digit
        var key = string.Concat(
            stateCode.PadLeft(2, '0'),
            issueDate.ToString("yyMM"),
            cnpj.PadLeft(14, '0'),
            model.ToString().PadLeft(2, '0'),
            series.ToString().PadLeft(3, '0'),
            number.ToString().PadLeft(9, '0'),
            emissionType.ToString(),
            numericCode.ToString().PadLeft(8, '0')
        );

        // Calculate check digit (module 11)
        var checkDigit = CalculateCheckDigit(key);

        return key + checkDigit;
    }

    /// <summary>
    /// Generates a random 8-digit numeric code for the access key.
    /// </summary>
    public static int GenerateNumericCode()
    {
        return Random.Shared.Next(10000000, 99999999);
    }

    /// <summary>
    /// Calculates the check digit (module 11) for an access key.
    /// </summary>
    private static int CalculateCheckDigit(string key)
    {
        var weights = new[] { 2, 3, 4, 5, 6, 7, 8, 9 };
        var sum = 0;
        var weightIndex = 0;

        // Process from right to left
        for (var i = key.Length - 1; i >= 0; i--)
        {
            sum += int.Parse(key[i].ToString()) * weights[weightIndex];
            weightIndex = (weightIndex + 1) % weights.Length;
        }

        var remainder = sum % 11;

        // If remainder is 0 or 1, check digit is 0
        // Otherwise, check digit is 11 - remainder
        return remainder < 2 ? 0 : 11 - remainder;
    }

    /// <summary>
    /// Validates an access key format and check digit.
    /// </summary>
    public static bool Validate(string accessKey)
    {
        if (string.IsNullOrWhiteSpace(accessKey))
            return false;

        accessKey = TextSanitizer.OnlyNumbers(accessKey);

        if (accessKey.Length != 44)
            return false;

        var keyWithoutDigit = accessKey[..43];
        var providedDigit = int.Parse(accessKey[43].ToString());
        var calculatedDigit = CalculateCheckDigit(keyWithoutDigit);

        return providedDigit == calculatedDigit;
    }

    /// <summary>
    /// Formats an access key with spaces for display.
    /// </summary>
    public static string Format(string accessKey)
    {
        accessKey = TextSanitizer.OnlyNumbers(accessKey);
        if (accessKey.Length != 44)
            return accessKey;

        return string.Join(" ",
            accessKey[..4],
            accessKey.Substring(4, 4),
            accessKey.Substring(8, 4),
            accessKey.Substring(12, 4),
            accessKey.Substring(16, 4),
            accessKey.Substring(20, 4),
            accessKey.Substring(24, 4),
            accessKey.Substring(28, 4),
            accessKey.Substring(32, 4),
            accessKey.Substring(36, 4),
            accessKey.Substring(40, 4));
    }

    /// <summary>
    /// Gets the IBGE state code from state abbreviation.
    /// </summary>
    public static string GetStateCode(string state)
    {
        return state.ToUpperInvariant() switch
        {
            "AC" => "12",
            "AL" => "27",
            "AP" => "16",
            "AM" => "13",
            "BA" => "29",
            "CE" => "23",
            "DF" => "53",
            "ES" => "32",
            "GO" => "52",
            "MA" => "21",
            "MT" => "51",
            "MS" => "50",
            "MG" => "31",
            "PA" => "15",
            "PB" => "25",
            "PR" => "41",
            "PE" => "26",
            "PI" => "22",
            "RJ" => "33",
            "RN" => "24",
            "RS" => "43",
            "RO" => "11",
            "RR" => "14",
            "SC" => "42",
            "SP" => "35",
            "SE" => "28",
            "TO" => "17",
            _ => throw new ArgumentException($"Invalid state: {state}")
        };
    }
}
