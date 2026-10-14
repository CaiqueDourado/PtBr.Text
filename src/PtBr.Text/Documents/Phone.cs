namespace PtBr.Text.Documents;

/// <summary>Kind of Brazilian phone number.</summary>
public enum PhoneKind
{
    /// <summary>Mobile: DDD + 9 digits starting with 9.</summary>
    Mobile,

    /// <summary>Landline: DDD + 8 digits starting with 2 to 5.</summary>
    Landline,

    /// <summary>Toll-free <c>0800</c>.</summary>
    TollFree,

    /// <summary>Non-geographic shared-cost or premium (<c>0300</c>, <c>0500</c>, <c>0900</c>).</summary>
    NonGeographic,

    /// <summary>National single numbers without DDD (<c>3003-xxxx</c>, <c>4003-xxxx</c>, <c>4004-xxxx</c>...).</summary>
    SingleNumber,
}

/// <summary>A parsed Brazilian phone number.</summary>
/// <param name="Kind">Kind of number.</param>
/// <param name="AreaCode">DDD (two digits) for mobile and landline; null otherwise.</param>
/// <param name="Number">Subscriber number without DDD, digits only.</param>
public sealed record BrazilianPhone(PhoneKind Kind, string? AreaCode, string Number)
{
    /// <summary>Human format: <c>(11) 98765-4321</c>, <c>(11) 3456-7890</c>, <c>0800 123 4567</c>, <c>4004-0001</c>.</summary>
    public string Formatted => Kind switch
    {
        PhoneKind.Mobile => $"({AreaCode}) {Number[..5]}-{Number[5..]}",
        PhoneKind.Landline => $"({AreaCode}) {Number[..4]}-{Number[4..]}",
        PhoneKind.TollFree or PhoneKind.NonGeographic => $"{Number[..4]} {Number[4..7]} {Number[7..]}",
        _ => $"{Number[..4]}-{Number[4..]}",
    };

    /// <summary>E.164 format (<c>+5511987654321</c>) for mobile and landline; null for numbers without DDD.</summary>
    public string? E164 => AreaCode is null ? null : $"+55{AreaCode}{Number}";

    /// <summary>Value for an HTML <c>tel:</c> link.</summary>
    public string TelUri => "tel:" + (E164 ?? Number);

    /// <inheritdoc />
    public override string ToString() => Formatted;
}

/// <summary>
/// Parses Brazilian phone numbers as people type them: with or without <c>+55</c>, with a leading zero
/// (<c>011</c>), with a carrier code (<c>0 21 11 ...</c>), with any punctuation.
/// </summary>
public static class Phone
{
    private static readonly HashSet<string> ValidAreaCodes =
    [
        "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "21", "22", "24", "27", "28",
        "31", "32", "33", "34", "35", "37", "38",
        "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "51", "53", "54", "55",
        "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "71", "73", "74", "75", "77", "79",
        "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "91", "92", "93", "94", "95", "96", "97", "98", "99",
    ];

    /// <summary>All 67 valid DDDs.</summary>
    public static IReadOnlyCollection<string> AreaCodes => ValidAreaCodes;

    /// <summary>Parses <paramref name="value"/>; returns null when it is not a valid Brazilian number.</summary>
    /// <param name="value">E.g. <c>(11) 98765-4321</c>, <c>+55 11 3456 7890</c>, <c>011 3456-7890</c>, <c>0800 123 4567</c>, <c>4004-0001</c>.</param>
    public static BrazilianPhone? Parse(string? value)
    {
        var d = DocumentText.Digits(value);
        if (d.Length == 0)
        {
            return null;
        }

        // 0800 / 0300 / 0500 / 0900 + 7 digits
        if (d.Length == 11 && d[0] == '0' && d[2..4] == "00" && d[1] is '8' or '3' or '5' or '9')
        {
            return new BrazilianPhone(d[1] == '8' ? PhoneKind.TollFree : PhoneKind.NonGeographic, null, d);
        }

        // 3003-xxxx, 4003-xxxx, 4004-xxxx, 4020-xxxx...
        if (d.Length == 8 && (d.StartsWith("300", StringComparison.Ordinal) || d.StartsWith("400", StringComparison.Ordinal) || d.StartsWith("4020", StringComparison.Ordinal)))
        {
            return new BrazilianPhone(PhoneKind.SingleNumber, null, d);
        }

        // Country code
        if ((d.Length == 12 || d.Length == 13) && d.StartsWith("55", StringComparison.Ordinal))
        {
            d = d[2..];
        }

        // Trunk prefix "0" and optional carrier code: 0 + DDD + number, or 0 + carrier + DDD + number
        if (d.Length > 0 && d[0] == '0')
        {
            d = d[1..];
            if (d.Length is 12 or 13)
            {
                d = d[2..];
            }
        }

        if (d.Length is not (10 or 11))
        {
            return null;
        }

        var ddd = d[..2];
        var number = d[2..];
        if (!ValidAreaCodes.Contains(ddd))
        {
            return null;
        }

        if (number.Length == 9 && number[0] == '9')
        {
            return new BrazilianPhone(PhoneKind.Mobile, ddd, number);
        }

        if (number.Length == 8 && number[0] is >= '2' and <= '5')
        {
            return new BrazilianPhone(PhoneKind.Landline, ddd, number);
        }

        return null;
    }

    /// <summary>True when <paramref name="value"/> parses as a valid Brazilian number.</summary>
    /// <param name="value">The phone number.</param>
    public static bool IsValid(string? value) => Parse(value) is not null;

    /// <summary>Formats a valid number (<c>(11) 98765-4321</c>); returns null when it is not valid.</summary>
    /// <param name="value">The phone number.</param>
    public static string? Format(string? value) => Parse(value)?.Formatted;
}
