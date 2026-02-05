namespace PDV.Fiscal.Utilities;

/// <summary>
/// Utility class for validating Brazilian documents (CPF, CNPJ).
/// </summary>
public static class DocumentValidator
{
    /// <summary>
    /// Validates a CPF (Individual Taxpayer Identification Number).
    /// </summary>
    public static bool IsValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Remove non-numeric characters
        cpf = TextSanitizer.OnlyNumbers(cpf);

        if (cpf.Length != 11)
            return false;

        // Check for known invalid patterns (all same digits)
        if (cpf.Distinct().Count() == 1)
            return false;

        // Calculate first check digit
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += int.Parse(cpf[i].ToString()) * (10 - i);
        }
        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cpf[9].ToString()) != digit1)
            return false;

        // Calculate second check digit
        sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += int.Parse(cpf[i].ToString()) * (11 - i);
        }
        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cpf[10].ToString()) == digit2;
    }

    /// <summary>
    /// Validates a CNPJ (Corporate Taxpayer Identification Number).
    /// </summary>
    public static bool IsValidCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        // Remove non-numeric characters
        cnpj = TextSanitizer.OnlyNumbers(cnpj);

        if (cnpj.Length != 14)
            return false;

        // Check for known invalid patterns (all same digits)
        if (cnpj.Distinct().Count() == 1)
            return false;

        // Calculate first check digit
        int[] multiplier1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += int.Parse(cnpj[i].ToString()) * multiplier1[i];
        }
        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cnpj[12].ToString()) != digit1)
            return false;

        // Calculate second check digit
        int[] multiplier2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        sum = 0;
        for (var i = 0; i < 13; i++)
        {
            sum += int.Parse(cnpj[i].ToString()) * multiplier2[i];
        }
        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cnpj[13].ToString()) == digit2;
    }

    /// <summary>
    /// Formats a CPF with mask (XXX.XXX.XXX-XX).
    /// </summary>
    public static string FormatCpf(string cpf)
    {
        cpf = TextSanitizer.OnlyNumbers(cpf);
        if (cpf.Length != 11)
            return cpf;

        return $"{cpf[..3]}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
    }

    /// <summary>
    /// Formats a CNPJ with mask (XX.XXX.XXX/XXXX-XX).
    /// </summary>
    public static string FormatCnpj(string cnpj)
    {
        cnpj = TextSanitizer.OnlyNumbers(cnpj);
        if (cnpj.Length != 14)
            return cnpj;

        return $"{cnpj[..2]}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    /// <summary>
    /// Validates a state registration number (basic validation).
    /// </summary>
    public static bool IsValidStateRegistration(string ie, string state)
    {
        if (string.IsNullOrWhiteSpace(ie))
            return false;

        // Remove non-numeric characters
        ie = TextSanitizer.OnlyNumbers(ie);

        // Basic length validation by state
        return state.ToUpperInvariant() switch
        {
            "SP" => ie.Length == 12,
            "RJ" => ie.Length == 8,
            "MG" => ie.Length == 13,
            "RS" => ie.Length == 10,
            "PR" => ie.Length == 10,
            "SC" => ie.Length == 9,
            "BA" => ie.Length == 8 || ie.Length == 9,
            "GO" => ie.Length == 9,
            "PE" => ie.Length == 9,
            "CE" => ie.Length == 9,
            _ => ie.Length >= 8 && ie.Length <= 14 // Generic validation
        };
    }
}
