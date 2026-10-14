using System.Text;

namespace PtBr.Text.Numbers;

/// <summary>Grammatical gender used for numbers that agree with a noun ("duas casas", "duzentas pessoas").</summary>
public enum Gender
{
    /// <summary>Masculine: um, dois, duzentos.</summary>
    Masculine,

    /// <summary>Feminine: uma, duas, duzentas.</summary>
    Feminine,
}

/// <summary>
/// Writes numbers in Brazilian Portuguese words ("por extenso"), including amounts in reais,
/// up to 999 trilhões.
/// </summary>
/// <example>
/// <code>
/// NumberToWords.ToWords(1250);              // "mil duzentos e cinquenta"
/// NumberToWords.ToWords(2, Gender.Feminine); // "duas"
/// NumberToWords.ToReais(1_000_000m);         // "um milhão de reais"
/// NumberToWords.ToReais(1.5m);               // "um real e cinquenta centavos"
/// </code>
/// </example>
public static class NumberToWords
{
    private static readonly string[] Units =
        ["zero", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove",
         "dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"];

    private static readonly string[] Tens =
        ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];

    private static readonly string[] Hundreds =
        ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

    private static readonly (string Singular, string Plural)[] Scales =
        [("", ""), ("mil", "mil"), ("milhão", "milhões"), ("bilhão", "bilhões"), ("trilhão", "trilhões")];

    /// <summary>The largest supported absolute value.</summary>
    public const long MaxValue = 999_999_999_999_999;

    /// <summary>Writes an integer in words.</summary>
    /// <param name="value">Between -<see cref="MaxValue"/> and <see cref="MaxValue"/>.</param>
    /// <param name="gender">Gender of the noun being counted.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public static string ToWords(long value, Gender gender = Gender.Masculine)
    {
        if (value is > MaxValue or < -MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Supported range is ±{MaxValue}.");
        }

        if (value == 0)
        {
            return "zero";
        }

        var words = Integer(Math.Abs(value), gender);
        return value < 0 ? "menos " + words : words;
    }

    /// <summary>Writes an amount of money in reais and centavos.</summary>
    /// <param name="amount">The amount; rounded to two decimals.</param>
    /// <exception cref="ArgumentOutOfRangeException">The integer part is outside the supported range.</exception>
    public static string ToReais(decimal amount) =>
        ToCurrency(amount, "real", "reais", "centavo", "centavos");

    /// <summary>Writes an amount in any currency with masculine units.</summary>
    /// <param name="amount">The amount; rounded to two decimals.</param>
    /// <param name="singular">Unit, singular (e.g. <c>dólar</c>).</param>
    /// <param name="plural">Unit, plural (e.g. <c>dólares</c>).</param>
    /// <param name="centSingular">Subunit, singular.</param>
    /// <param name="centPlural">Subunit, plural.</param>
    /// <exception cref="ArgumentOutOfRangeException">The integer part is outside the supported range.</exception>
    public static string ToCurrency(decimal amount, string singular, string plural, string centSingular, string centPlural)
    {
        var negative = amount < 0;
        amount = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        if (amount > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"Supported range is ±{MaxValue}.");
        }

        var integer = (long)Math.Truncate(amount);
        var cents = (int)((amount - integer) * 100);

        var parts = new List<string>(2);
        if (integer > 0)
        {
            // "um milhão de reais", but "um milhão e duzentos mil reais"
            var de = integer >= 1_000_000 && integer % 1_000_000 == 0 ? "de " : string.Empty;
            parts.Add($"{Integer(integer, Gender.Masculine)} {de}{(integer == 1 ? singular : plural)}");
        }

        if (cents > 0)
        {
            parts.Add($"{Integer(cents, Gender.Masculine)} {(cents == 1 ? centSingular : centPlural)}");
        }

        var text = parts.Count == 0 ? $"zero {plural}" : string.Join(" e ", parts);
        return negative ? "menos " + text : text;
    }

    private static string Integer(long value, Gender gender)
    {
        // Split into groups of three digits, lowest first.
        var groups = new List<int>();
        while (value > 0)
        {
            groups.Add((int)(value % 1000));
            value /= 1000;
        }

        var lowestNonZero = groups.FindIndex(g => g > 0);
        var builder = new StringBuilder();

        for (var scale = groups.Count - 1; scale >= 0; scale--)
        {
            var group = groups[scale];
            if (group == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                // "e" joins the last group when it is below 100 or a round hundred: "mil e cem", "dois mil e cinco",
                // "um milhão e duzentos mil"; otherwise words just follow: "mil duzentos e cinquenta".
                builder.Append(scale == lowestNonZero && (group < 100 || group % 100 == 0) ? " e " : " ");
            }

            // Millions and above are masculine nouns ("duas mil pessoas", but "dois milhões de pessoas").
            var groupGender = scale >= 2 ? Gender.Masculine : gender;

            if (scale == 1 && group == 1)
            {
                builder.Append("mil"); // "mil", never "um mil"
                continue;
            }

            builder.Append(Group(group, groupGender));
            if (scale > 0)
            {
                builder.Append(' ').Append(group == 1 ? Scales[scale].Singular : Scales[scale].Plural);
            }
        }

        return builder.ToString();
    }

    private static string Group(int value, Gender gender)
    {
        if (value == 100)
        {
            return "cem";
        }

        var parts = new List<string>(3);
        var hundreds = value / 100;
        var rest = value % 100;

        if (hundreds > 0)
        {
            parts.Add(gender == Gender.Feminine && hundreds > 1 ? Hundreds[hundreds][..^2] + "as" : Hundreds[hundreds]);
        }

        if (rest > 0)
        {
            if (rest < 20)
            {
                parts.Add(Unit(rest, gender));
            }
            else
            {
                parts.Add(rest % 10 == 0 ? Tens[rest / 10] : $"{Tens[rest / 10]} e {Unit(rest % 10, gender)}");
            }
        }

        return string.Join(" e ", parts);
    }

    private static string Unit(int value, Gender gender) => (value, gender) switch
    {
        (1, Gender.Feminine) => "uma",
        (2, Gender.Feminine) => "duas",
        _ => Units[value],
    };
}
