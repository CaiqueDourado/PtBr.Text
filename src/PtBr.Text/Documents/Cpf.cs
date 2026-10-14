namespace PtBr.Text.Documents;

/// <summary>CPF (Cadastro de Pessoas Físicas): validation, formatting and generation for tests.</summary>
public static class Cpf
{
    /// <summary>Keeps only the digits. Returns null when the result does not have 11 digits.</summary>
    /// <param name="value">CPF with or without punctuation.</param>
    public static string? Clean(string? value)
    {
        var digits = DocumentText.Digits(value);
        return digits.Length == 11 ? digits : null;
    }

    /// <summary>
    /// True when the CPF has 11 digits, correct check digits and is not a run of the same digit
    /// (<c>111.111.111-11</c> passes the arithmetic but is not a real CPF).
    /// </summary>
    /// <param name="value">CPF with or without punctuation.</param>
    public static bool IsValid(string? value)
    {
        var digits = Clean(value);
        if (digits is null || DocumentText.AllSame(digits))
        {
            return false;
        }

        return CheckDigits(digits.AsSpan(0, 9)) == digits[9..];
    }

    /// <summary>Formats as <c>000.000.000-00</c>. Returns null when the input does not have 11 digits.</summary>
    /// <param name="value">CPF with or without punctuation.</param>
    public static string? Format(string? value)
    {
        var d = Clean(value);
        return d is null ? null : $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    }

    /// <summary>Computes the two check digits for the first nine digits.</summary>
    /// <param name="nineDigits">The first nine digits.</param>
    /// <exception cref="ArgumentException">The input does not have exactly nine digits.</exception>
    public static string CheckDigits(ReadOnlySpan<char> nineDigits)
    {
        if (nineDigits.Length != 9 || !DocumentText.AreDigits(nineDigits))
        {
            throw new ArgumentException("Expected exactly nine digits.", nameof(nineDigits));
        }

        Span<int> d = stackalloc int[10];
        for (var i = 0; i < 9; i++)
        {
            d[i] = nineDigits[i] - '0';
        }

        d[9] = Mod11(d[..9], 10);
        var second = Mod11(d, 11);
        return $"{d[9]}{second}";
    }

    /// <summary>Generates a random valid CPF, for tests and sample data only.</summary>
    /// <param name="random">Random source (pass a seeded one for reproducible tests).</param>
    /// <param name="formatted">Return with punctuation.</param>
    public static string Generate(Random? random = null, bool formatted = false)
    {
        random ??= Random.Shared;
        string body;
        do
        {
            body = string.Concat(Enumerable.Range(0, 9).Select(_ => (char)('0' + random.Next(10))));
        }
        while (DocumentText.AllSame(body));

        var cpf = body + CheckDigits(body);
        return formatted ? Format(cpf)! : cpf;
    }

    private static int Mod11(ReadOnlySpan<int> digits, int firstWeight)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += digits[i] * (firstWeight - i);
        }

        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}
