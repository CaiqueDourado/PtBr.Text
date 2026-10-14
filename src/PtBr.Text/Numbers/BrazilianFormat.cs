using System.Globalization;

namespace PtBr.Text.Numbers;

/// <summary>
/// Brazilian number, money and date formatting that does not depend on the machine culture
/// (works the same with <c>InvariantGlobalization</c> and on servers configured in English).
/// </summary>
public static class BrazilianFormat
{
    /// <summary>Formats with thousands separator "." and decimal ",": <c>1234.5</c> → <c>1.234,50</c>.</summary>
    /// <param name="value">The number.</param>
    /// <param name="decimals">Decimal places.</param>
    public static string Number(decimal value, int decimals = 2)
    {
        var invariant = Math.Round(value, decimals, MidpointRounding.AwayFromZero)
            .ToString(decimals > 0 ? "#,0." + new string('0', decimals) : "#,0", CultureInfo.InvariantCulture);
        return invariant.Replace(",", "\u0001", StringComparison.Ordinal)
            .Replace('.', ',')
            .Replace('\u0001', '.');
    }

    /// <summary>Formats as Brazilian reais: <c>1299.9</c> → <c>R$ 1.299,90</c>.</summary>
    /// <param name="value">The amount.</param>
    public static string Money(decimal value) =>
        value < 0 ? "-R$ " + Number(-value) : "R$ " + Number(value);

    /// <summary>Formats a date as <c>dd/MM/yyyy</c>.</summary>
    /// <param name="date">The date.</param>
    public static string Date(DateTime date) => date.ToString("dd'/'MM'/'yyyy", CultureInfo.InvariantCulture);
}
