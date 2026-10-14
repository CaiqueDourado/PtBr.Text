using System.Text;

namespace PtBr.Text.Keywords;

/// <summary>A key phrase and its score.</summary>
/// <param name="Phrase">The phrase, lower case, as it appears (accents kept).</param>
/// <param name="Score">RAKE score: sum of word degree/frequency.</param>
/// <param name="Occurrences">How many times the phrase appears.</param>
public sealed record KeyPhrase(string Phrase, double Score, int Occurrences);

/// <summary>Options for <see cref="RakeExtractor"/>.</summary>
public sealed class RakeOptions
{
    /// <summary>Words that split phrases. Default: <see cref="Stopwords.Portuguese"/>.</summary>
    public IReadOnlySet<string> Stopwords { get; set; } = Keywords.Stopwords.Portuguese;

    /// <summary>Discard phrases with more words than this. Default: 4.</summary>
    public int MaxWords { get; set; } = 4;

    /// <summary>Discard words shorter than this (characters). Default: 2.</summary>
    public int MinWordLength { get; set; } = 2;

    /// <summary>Discard phrases made only of numbers. Default: true.</summary>
    public bool IgnoreNumbers { get; set; } = true;
}

/// <summary>
/// RAKE (Rapid Automatic Keyword Extraction; Rose, Engel, Cramer and Cowley, 2010) for Portuguese:
/// candidate phrases are the runs of words between stopwords and punctuation; each word scores degree/frequency;
/// a phrase scores the sum of its words. Unlike most ports, accented letters are kept as part of words.
/// </summary>
/// <example>
/// <code>
/// var phrases = RakeExtractor.Extract("A bateria dura o dia todo e a câmera noturna surpreende.");
/// // "câmera noturna surpreende", "bateria dura", "dia todo" ...
/// </code>
/// </example>
public static class RakeExtractor
{
    /// <summary>Extracts key phrases from <paramref name="text"/>, best first.</summary>
    /// <param name="text">Input text.</param>
    /// <param name="top">Maximum number of phrases (null = all).</param>
    /// <param name="options">Options; null = defaults.</param>
    public static IReadOnlyList<KeyPhrase> Extract(string? text, int? top = null, RakeOptions? options = null)
    {
        options ??= new RakeOptions();
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var candidates = Candidates(text, options);
        if (candidates.Count == 0)
        {
            return [];
        }

        var frequency = new Dictionary<string, int>(StringComparer.Ordinal);
        var degree = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var phrase in candidates)
        {
            foreach (var word in phrase)
            {
                frequency[word] = frequency.GetValueOrDefault(word) + 1;
                degree[word] = degree.GetValueOrDefault(word) + phrase.Count; // co-occurrences incl. itself
            }
        }

        var result = candidates
            .GroupBy(p => string.Join(' ', p), StringComparer.Ordinal)
            .Select(g => new KeyPhrase(
                g.Key,
                g.First().Sum(w => (double)degree[w] / frequency[w]),
                g.Count()))
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.Occurrences)
            .ThenBy(p => p.Phrase, StringComparer.Ordinal);

        return (top is null ? result : result.Take(top.Value)).ToList();
    }

    private static List<List<string>> Candidates(string text, RakeOptions options)
    {
        var phrases = new List<List<string>>();
        var current = new List<string>();

        void Close()
        {
            if (current.Count > 0 && current.Count <= options.MaxWords
                && !(options.IgnoreNumbers && current.All(w => w.All(char.IsDigit))))
            {
                phrases.Add(current);
            }

            current = [];
        }

        // Multi-word stopwords ("no entanto", "além disso") are joined first so they act as one token.
        var multiWord = options.Stopwords.Where(s => s.Contains(' ', StringComparison.Ordinal)).ToList();

        foreach (var token in Tokenize(text))
        {
            if (token.IsBoundary)
            {
                Close();
                continue;
            }

            var key = Stopwords.Key(token.Text);
            if (options.Stopwords.Contains(key) || token.Text.Length < options.MinWordLength)
            {
                Close();
                continue;
            }

            if (multiWord.Count > 0 && current.Count > 0
                && multiWord.Contains(Stopwords.Key(current[^1] + " " + token.Text)))
            {
                current.RemoveAt(current.Count - 1);
                Close();
                continue;
            }

            current.Add(token.Text.ToLowerInvariant());
        }

        Close();
        return phrases;
    }

    private readonly record struct Token(string Text, bool IsBoundary);

    private static IEnumerable<Token> Tokenize(string text)
    {
        var word = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var inWord = char.IsLetterOrDigit(c)
                || (c is '-' or '\'' && word.Length > 0 && i + 1 < text.Length && char.IsLetter(text[i + 1]));

            if (inWord)
            {
                word.Append(c);
                continue;
            }

            if (word.Length > 0)
            {
                yield return new Token(word.ToString(), false);
                word.Clear();
            }

            if (!char.IsWhiteSpace(c))
            {
                yield return new Token(c.ToString(), true); // punctuation splits phrases
            }
        }

        if (word.Length > 0)
        {
            yield return new Token(word.ToString(), false);
        }
    }
}
