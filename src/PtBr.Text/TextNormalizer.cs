using System.Text;
using System.Text.RegularExpressions;

namespace PtBr.Text;

/// <summary>Options for <see cref="TextNormalizer.Slugify(string?, SlugOptions?)"/>.</summary>
public sealed class SlugOptions
{
    /// <summary>Separator between words. Default: <c>-</c>.</summary>
    public char Separator { get; set; } = '-';

    /// <summary>Maximum length; the slug is cut at a word boundary. Default: no limit.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Replace <c>&amp;</c> with <c>e</c> (Portuguese "and"). Default: true.</summary>
    public bool AmpersandAsE { get; set; } = true;

    /// <summary>Lower-case the result. Default: true.</summary>
    public bool LowerCase { get; set; } = true;
}

/// <summary>
/// Accent removal, slugs, whitespace and capitalisation for Portuguese text.
/// Does not depend on ICU, so results are identical with <c>InvariantGlobalization</c> enabled
/// (where <c>string.Normalize(FormD)</c> does not decompose accented letters).
/// </summary>
public static class TextNormalizer
{
    private const string From = "ÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝàáâãäåçèéêëìíîïðñòóôõöøùúûüýÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĴĵĶķĸĹĺĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽž";
    private const string To = "AAAAAACEEEEIIIIDNOOOOOOUUUUYaaaaaaceeeeiiiidnoooooouuuuyyAaAaAaCcCcCcCcDdDdEeEeEeEeEeGgGgGgGgHhHhIiIiIiIiIiJjKkkLlLlLlLlLlNnNnNnnNnOoOoOoRrRrRrSsSsSsSsTtTtTtUuUuUuUuUuUuWwYyYZzZzZz";

    private static readonly Regex Spaces = new("\\s+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> LowerParticles = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "à", "ao", "aos", "as", "às", "com", "da", "das", "de", "do", "dos", "e", "em", "na", "nas", "no", "nos", "o", "os", "ou", "para", "pela", "pelas", "pelo", "pelos", "por", "sem", "sob", "sobre",
    };

    /// <summary>Removes accents and other diacritics from Latin letters (á→a, ç→c, ñ→n, æ→ae, ß→ss).</summary>
    /// <param name="text">Input text. Null returns an empty string.</param>
    public static string RemoveAccents(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder? builder = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c < 0xC0)
            {
                builder?.Append(c);
                continue;
            }

            var replacement = Map(c);
            if (replacement is null)
            {
                builder?.Append(c);
                continue;
            }

            builder ??= new StringBuilder(text.Length + 8).Append(text, 0, i);
            builder.Append(replacement);
        }

        return builder?.ToString() ?? text;
    }

    /// <summary>Removes the accent of a single character; returns it unchanged when it has none.</summary>
    /// <param name="c">The character.</param>
    public static char RemoveAccent(char c)
    {
        if (c < 0xC0)
        {
            return c;
        }

        var index = From.IndexOf(c, StringComparison.Ordinal);
        return index >= 0 ? To[index] : c;
    }

    /// <summary>Creates a URL slug: <c>"Fogão 4 Bocas &amp; Forno"</c> becomes <c>"fogao-4-bocas-e-forno"</c>.</summary>
    /// <param name="text">Input text.</param>
    /// <param name="options">Options; null = defaults.</param>
    public static string Slugify(string? text, SlugOptions? options = null)
    {
        options ??= new SlugOptions();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var source = RemoveAccents(text.Replace('º', 'o').Replace('ª', 'a'));
        var builder = new StringBuilder(source.Length);
        var pendingSeparator = false;

        foreach (var raw in source)
        {
            if (raw == '&' && options.AmpersandAsE)
            {
                AppendWord(builder, "e", options.Separator, ref pendingSeparator);
                continue;
            }

            if (char.IsAsciiLetterOrDigit(raw))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append(options.Separator);
                }

                pendingSeparator = false;
                builder.Append(options.LowerCase ? char.ToLowerInvariant(raw) : raw);
            }
            else if (raw is '\'' or '’')
            {
                // "d'água" -> "dagua"
            }
            else
            {
                pendingSeparator = true;
            }
        }

        var slug = builder.ToString();
        if (options.MaxLength is { } max && slug.Length > max)
        {
            var cut = slug.LastIndexOf(options.Separator, Math.Min(max, slug.Length - 1));
            slug = cut > 0 ? slug[..cut] : slug[..max];
        }

        return slug;
    }

    /// <summary>Trims and collapses any run of whitespace (including line breaks and tabs) into one space.</summary>
    /// <param name="text">Input text.</param>
    public static string CollapseWhitespace(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : Spaces.Replace(text, " ").Trim();

    /// <summary>
    /// Capitalises each word, keeping Portuguese particles in lower case and words that are already
    /// mixed case, acronyms or contain digits as they are: <c>"GELADEIRA FROST FREE DE 375 LITROS"</c> becomes
    /// <c>"Geladeira Frost Free de 375 Litros"</c>.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="keepUpperCaseUpTo">All-caps words up to this length are kept (e.g. <c>TV</c>, <c>LED</c>) when the text is not entirely upper case. Default: 3.</param>
    public static string ToTitleCase(string? text, int keepUpperCaseUpTo = 3)
    {
        var clean = CollapseWhitespace(text);
        if (clean.Length == 0)
        {
            return clean;
        }

        var allUpper = clean.Any(char.IsLetter) && !clean.Any(char.IsLower);
        var words = clean.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var isAcronym = !allUpper && word.Length <= keepUpperCaseUpTo && word.Any(char.IsLetter) && word.All(c => !char.IsLower(c));
            var isMixed = !allUpper && word.Any(char.IsUpper) && word.Skip(1).Any(char.IsUpper) && word.Any(char.IsLower);
            var hasDigit = word.Any(char.IsAsciiDigit); // 375L, 128GB, 4K, i7: keep as written
            if (isAcronym || isMixed || hasDigit)
            {
                continue;
            }

            var lower = word.ToLowerInvariant();
            words[i] = i > 0 && LowerParticles.Contains(lower)
                ? lower
                : lower.Length == 0 ? lower : char.ToUpperInvariant(lower[0]) + lower[1..];
        }

        return string.Join(' ', words);
    }

    private static void AppendWord(StringBuilder builder, string word, char separator, ref bool pendingSeparator)
    {
        if (builder.Length > 0)
        {
            builder.Append(separator);
        }

        builder.Append(word);
        pendingSeparator = true;
    }

    private static string? Map(char c) => c switch
    {
        'Æ' => "AE",
        'æ' => "ae",
        'Œ' => "OE",
        'œ' => "oe",
        'ß' => "ss",
        'Þ' => "Th",
        'þ' => "th",
        _ => From.IndexOf(c, StringComparison.Ordinal) is var i and >= 0 ? To[i].ToString() : null,
    };
}
