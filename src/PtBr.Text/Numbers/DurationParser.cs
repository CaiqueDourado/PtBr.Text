using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PtBr.Text.Numbers;

/// <summary>
/// Parses durations written in Portuguese ("1 hora e 30 minutos", "1h30", "meia hora", "uma hora e meia",
/// "90 min", "2 dias") and writes them as ISO 8601 (<c>PT1H30M</c>), as used by schema.org
/// (<c>Recipe.totalTime</c>, <c>cookTime</c>, video <c>duration</c>).
/// </summary>
public static class DurationParser
{
    private static readonly RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    // "1h30", "1h 30", "1h30min" -> hours and minutes without a minute unit word.
    private static readonly Regex HourMinute = new("(\\d+)\\s*h\\s*(\\d{1,2})(?!\\s*(?:h|d))", Options, Timeout);

    // "1:30" (h:mm)
    private static readonly Regex Clock = new("^\\s*(\\d{1,3}):(\\d{2})\\s*$", Options, Timeout);

    private static readonly Regex Amount = new(
        "(?<![a-z0-9])(\\d+(?:[.,]\\d+)?|um|uma|dois|duas|tres|quatro|cinco|seis|sete|oito|nove|dez|quinze|vinte|trinta|quarenta|cinquenta|sessenta|meia|meio)\\s*" +
        "(dias?|d|horas?|hrs?|h|minutos?|mins?|m|segundos?|segs?|s)\\b",
        Options, Timeout);

    private static readonly Regex AndAHalf = new("(horas?|hrs?|h)\\s+e\\s+meia", Options, Timeout);

    private static readonly Dictionary<string, double> NumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["um"] = 1, ["uma"] = 1, ["dois"] = 2, ["duas"] = 2, ["tres"] = 3, ["quatro"] = 4, ["cinco"] = 5, ["seis"] = 6,
        ["sete"] = 7, ["oito"] = 8, ["nove"] = 9, ["dez"] = 10, ["quinze"] = 15, ["vinte"] = 20, ["trinta"] = 30,
        ["quarenta"] = 40, ["cinquenta"] = 50, ["sessenta"] = 60, ["meia"] = 0.5, ["meio"] = 0.5,
    };

    /// <summary>Parses <paramref name="text"/>; returns null when no duration is found.</summary>
    /// <param name="text">Duration in Portuguese.</param>
    public static TimeSpan? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = TextNormalizer.RemoveAccents(text).ToLowerInvariant();

        var clock = Clock.Match(normalized);
        if (clock.Success)
        {
            return TimeSpan.FromHours(int.Parse(clock.Groups[1].Value, CultureInfo.InvariantCulture))
                + TimeSpan.FromMinutes(int.Parse(clock.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        var total = TimeSpan.Zero;
        var found = false;

        normalized = HourMinute.Replace(normalized, m =>
        {
            total += TimeSpan.FromHours(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
                + TimeSpan.FromMinutes(int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
            found = true;
            return " ";
        });

        if (AndAHalf.IsMatch(normalized))
        {
            total += TimeSpan.FromMinutes(30);
            found = true;
            normalized = AndAHalf.Replace(normalized, "$1");
        }

        foreach (Match match in Amount.Matches(normalized))
        {
            var number = NumberWords.TryGetValue(match.Groups[1].Value, out var word)
                ? word
                : double.Parse(match.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);

            var unit = match.Groups[2].Value;
            total += unit[0] switch
            {
                'd' => TimeSpan.FromDays(number),
                'h' => TimeSpan.FromHours(number),
                'm' => TimeSpan.FromMinutes(number),
                _ => TimeSpan.FromSeconds(number),
            };
            found = true;
        }

        return found ? total : null;
    }

    /// <summary>Writes a duration in ISO 8601: <c>PT1H30M</c>, <c>P1DT2H</c>, <c>PT45S</c>, <c>PT0S</c>.</summary>
    /// <param name="duration">The duration (negative values are not supported).</param>
    /// <exception cref="ArgumentOutOfRangeException">The duration is negative.</exception>
    public static string ToIso8601(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Negative durations are not supported.");
        }

        var builder = new StringBuilder("P");
        if (duration.Days > 0)
        {
            builder.Append(duration.Days).Append('D');
        }

        if (duration.Hours > 0 || duration.Minutes > 0 || duration.Seconds > 0 || duration.Days == 0)
        {
            builder.Append('T');
            if (duration.Hours > 0)
            {
                builder.Append(duration.Hours).Append('H');
            }

            if (duration.Minutes > 0)
            {
                builder.Append(duration.Minutes).Append('M');
            }

            if (duration.Seconds > 0 || (duration.Hours == 0 && duration.Minutes == 0))
            {
                builder.Append(duration.Seconds).Append('S');
            }
        }

        return builder.ToString();
    }

    /// <summary>Parses Portuguese text straight to ISO 8601; returns null when no duration is found.</summary>
    /// <param name="text">Duration in Portuguese.</param>
    public static string? ToIso8601(string? text) => Parse(text) is { } span ? ToIso8601(span) : null;
}
