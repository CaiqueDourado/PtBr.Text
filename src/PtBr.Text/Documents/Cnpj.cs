using System.Text;

namespace PtBr.Text.Documents;

/// <summary>
/// CNPJ (Cadastro Nacional da Pessoa Jurídica), including the alphanumeric format introduced by the
/// Receita Federal (IN RFB 2.229/2024): the first 12 characters may be digits or upper-case letters,
/// the last two are always numeric check digits. Numeric CNPJs remain valid and are a special case.
/// </summary>
/// <remarks>
/// Each character is valued as its ASCII code minus 48 (<c>'0'</c>=0 … <c>'9'</c>=9, <c>'A'</c>=17 … <c>'Z'</c>=42)
/// and the check digits use the usual mod-11 weights. Example from the Receita: <c>12.ABC.345/01DE-35</c>.
/// </remarks>
public static class Cnpj
{
    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Removes punctuation (<c>. / -</c> and spaces) and upper-cases. Returns null when the result is not
    /// 12 alphanumeric characters followed by 2 digits.
    /// </summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var builder = new StringBuilder(14);
        foreach (var c in value)
        {
            if (c is '.' or '/' or '-' or ' ')
            {
                continue;
            }

            if (!char.IsAsciiLetterOrDigit(c))
            {
                return null;
            }

            builder.Append(char.ToUpperInvariant(c));
        }

        var clean = builder.ToString();
        return clean.Length == 14 && char.IsAsciiDigit(clean[12]) && char.IsAsciiDigit(clean[13]) ? clean : null;
    }

    /// <summary>True when the CNPJ (numeric or alphanumeric) has correct check digits and is not a run of one character.</summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static bool IsValid(string? value)
    {
        var clean = Clean(value);
        if (clean is null || DocumentText.AllSame(clean))
        {
            return false;
        }

        return CheckDigits(clean.AsSpan(0, 12)) == clean[12..];
    }

    /// <summary>True when the CNPJ uses letters (the new format).</summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static bool IsAlphanumeric(string? value) => Clean(value) is { } c && c.Any(char.IsAsciiLetter);

    /// <summary>Formats as <c>XX.XXX.XXX/XXXX-XX</c>. Returns null when the input has the wrong shape.</summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static string? Format(string? value)
    {
        var c = Clean(value);
        return c is null ? null : $"{c[..2]}.{c[2..5]}.{c[5..8]}/{c[8..12]}-{c[12..]}";
    }

    /// <summary>The 8-character root (identifies the company; branches share it).</summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static string? Root(string? value) => Clean(value)?[..8];

    /// <summary>True for the head office (order <c>0001</c>).</summary>
    /// <param name="value">CNPJ with or without punctuation.</param>
    public static bool IsHeadOffice(string? value) => Clean(value)?[8..12] == "0001";

    /// <summary>Computes the two check digits for the first 12 characters.</summary>
    /// <param name="twelveCharacters">Digits or upper-case letters.</param>
    /// <exception cref="ArgumentException">The input is not 12 alphanumeric characters.</exception>
    public static string CheckDigits(ReadOnlySpan<char> twelveCharacters)
    {
        if (twelveCharacters.Length != 12)
        {
            throw new ArgumentException("Expected exactly 12 characters.", nameof(twelveCharacters));
        }

        Span<int> values = stackalloc int[13];
        for (var i = 0; i < 12; i++)
        {
            var c = char.ToUpperInvariant(twelveCharacters[i]);
            if (!char.IsAsciiDigit(c) && !char.IsAsciiLetterUpper(c))
            {
                throw new ArgumentException("Only digits and letters A-Z are allowed.", nameof(twelveCharacters));
            }

            values[i] = c - '0';
        }

        values[12] = Mod11(values[..12], FirstWeights);
        var second = Mod11(values, SecondWeights);
        return $"{values[12]}{second}";
    }

    /// <summary>Generates a random valid CNPJ, for tests and sample data only.</summary>
    /// <param name="random">Random source (pass a seeded one for reproducible tests).</param>
    /// <param name="alphanumeric">Use letters in the root and order (new format).</param>
    /// <param name="formatted">Return with punctuation.</param>
    public static string Generate(Random? random = null, bool alphanumeric = false, bool formatted = false)
    {
        random ??= Random.Shared;
        const string digits = "0123456789";
        const string alnum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var pool = alphanumeric ? alnum : digits;

        var body = new char[12];
        for (var i = 0; i < 8; i++)
        {
            body[i] = pool[random.Next(pool.Length)];
        }

        "0001".CopyTo(0, body, 8, 4);
        var cnpj = new string(body) + CheckDigits(body);
        return formatted ? Format(cnpj)! : cnpj;
    }

    private static int Mod11(ReadOnlySpan<int> values, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i] * weights[i];
        }

        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }
}
