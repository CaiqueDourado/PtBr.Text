using PtBr.Text.Numbers;

namespace PtBr.Text.Inflection;

/// <summary>Whether a ranking is about the best or the worst items.</summary>
public enum Ranking
{
    /// <summary>"melhor/melhores".</summary>
    Best,

    /// <summary>"pior/piores".</summary>
    Worst,
}

/// <summary>
/// Ranking titles in correct Portuguese, with article, number and gender agreement:
/// "Qual a melhor geladeira?", "Quais os melhores fones de ouvido?", "As 10 melhores panelas para comprar em 2026".
/// </summary>
public static class SeoTitles
{
    /// <summary>
    /// The question form. Keeps the term as given: a singular term gives "Qual a/o", a plural term gives
    /// "Quais as/os". Use <paramref name="plural"/> to force the plural ("geladeira" → "Quais as melhores geladeiras?").
    /// </summary>
    /// <param name="term">Product or category name.</param>
    /// <param name="ranking">Best or worst.</param>
    /// <param name="plural">Pluralise a singular term.</param>
    public static string Question(string term, Ranking ranking = Ranking.Best, bool plural = false)
    {
        var (text, isPlural, gender) = Prepare(term, plural);
        var adjective = Adjective(ranking, isPlural);
        return isPlural
            ? $"Quais {Article(gender, true)} {adjective} {text}?"
            : $"Qual {Article(gender, false)} {adjective} {text}?";
    }

    /// <summary>
    /// The headline form: "As 10 melhores geladeiras para comprar em 2026". With <paramref name="count"/> of 1 or
    /// less, gives "A melhor geladeira para comprar".
    /// </summary>
    /// <param name="term">Product or category name (singular or plural).</param>
    /// <param name="count">Number of items in the ranking.</param>
    /// <param name="ranking">Best or worst.</param>
    /// <param name="year">Optional year appended as "em {year}".</param>
    /// <param name="suffix">Text after the term. Default: "para comprar".</param>
    public static string Headline(string term, int count, Ranking ranking = Ranking.Best, int? year = null, string suffix = "para comprar")
    {
        var plural = count > 1;
        var (text, _, gender) = Prepare(term, plural);
        var article = Capitalize(Article(gender, plural));
        var head = plural
            ? $"{article} {count} {Adjective(ranking, true)} {text}"
            : $"{article} {Adjective(ranking, false)} {text}";

        var tail = string.IsNullOrWhiteSpace(suffix) ? string.Empty : " " + suffix.Trim();
        return year is null ? head + tail : $"{head}{tail} em {year}";
    }

    private static (string Text, bool Plural, Gender Gender) Prepare(string term, bool forcePlural)
    {
        var clean = TextNormalizer.CollapseWhitespace(term);
        var gender = Inflector.GuessGender(clean);
        var isPlural = Inflector.IsPlural(clean);
        if (forcePlural && !isPlural)
        {
            clean = Inflector.Pluralize(clean);
            isPlural = true;
        }

        return (clean, isPlural, gender);
    }

    private static string Adjective(Ranking ranking, bool plural) => (ranking, plural) switch
    {
        (Ranking.Best, false) => "melhor",
        (Ranking.Best, true) => "melhores",
        (Ranking.Worst, false) => "pior",
        _ => "piores",
    };

    private static string Article(Gender gender, bool plural) => (gender, plural) switch
    {
        (Gender.Feminine, false) => "a",
        (Gender.Feminine, true) => "as",
        (Gender.Masculine, false) => "o",
        _ => "os",
    };

    private static string Capitalize(string text) => char.ToUpperInvariant(text[0]) + text[1..];
}
