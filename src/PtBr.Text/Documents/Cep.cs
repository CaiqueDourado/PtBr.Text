namespace PtBr.Text.Documents;

/// <summary>CEP (Código de Endereçamento Postal): cleaning, formatting and the state (UF) by numeric range.</summary>
public static class Cep
{
    // Correios ranges by state (first five digits).
    private static readonly (int From, int To, string Uf)[] Ranges =
    [
        (1000, 19999, "SP"), (20000, 28999, "RJ"), (29000, 29999, "ES"), (30000, 39999, "MG"),
        (40000, 48999, "BA"), (49000, 49999, "SE"), (50000, 56999, "PE"), (57000, 57999, "AL"),
        (58000, 58999, "PB"), (59000, 59999, "RN"), (60000, 63999, "CE"), (64000, 64999, "PI"),
        (65000, 65999, "MA"), (66000, 68899, "PA"), (68900, 68999, "AP"), (69000, 69299, "AM"),
        (69300, 69399, "RR"), (69400, 69899, "AM"), (69900, 69999, "AC"), (70000, 72799, "DF"),
        (72800, 72999, "GO"), (73000, 73699, "DF"), (73700, 76799, "GO"), (76800, 76999, "RO"),
        (77000, 77999, "TO"), (78000, 78899, "MT"), (79000, 79999, "MS"), (80000, 87999, "PR"),
        (88000, 89999, "SC"), (90000, 99999, "RS"),
    ];

    /// <summary>Keeps only the digits. Returns null when the result does not have 8 digits.</summary>
    /// <param name="value">CEP with or without the dash.</param>
    public static string? Clean(string? value)
    {
        var digits = DocumentText.Digits(value);
        return digits.Length == 8 ? digits : null;
    }

    /// <summary>True when the CEP has 8 digits and falls inside a known state range.</summary>
    /// <param name="value">CEP with or without the dash.</param>
    public static bool IsValid(string? value) => GetState(value) is not null;

    /// <summary>Formats as <c>00000-000</c>. Returns null when the input does not have 8 digits.</summary>
    /// <param name="value">CEP with or without the dash.</param>
    public static string? Format(string? value)
    {
        var d = Clean(value);
        return d is null ? null : $"{d[..5]}-{d[5..]}";
    }

    /// <summary>The state (UF) whose range contains the CEP, e.g. <c>SP</c>, or null.</summary>
    /// <param name="value">CEP with or without the dash.</param>
    public static string? GetState(string? value)
    {
        var d = Clean(value);
        if (d is null)
        {
            return null;
        }

        var prefix = int.Parse(d.AsSpan(0, 5), System.Globalization.CultureInfo.InvariantCulture);
        foreach (var (from, to, uf) in Ranges)
        {
            if (prefix >= from && prefix <= to)
            {
                return uf;
            }
        }

        return null;
    }
}
