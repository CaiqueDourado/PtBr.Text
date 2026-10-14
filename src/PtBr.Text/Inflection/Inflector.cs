using PtBr.Text.Numbers;

namespace PtBr.Text.Inflection;

/// <summary>
/// Plural and gender of Portuguese nouns, by rule plus an exception list. Built for product and category
/// names ("geladeira" → "geladeiras", "fogão" → "fogões", "papel" → "papéis", "tênis" stays "tênis").
/// </summary>
/// <remarks>
/// Rules cover regular nouns; irregular ones are in an exception table you can extend with <see cref="AddException"/>.
/// For multi-word terms only the first word is inflected ("fone de ouvido" → "fones de ouvido"),
/// which is right for "noun + de + complement" and for most product names.
/// </remarks>
public static class Inflector
{
    private static readonly Dictionary<string, string> Exceptions = new(StringComparer.OrdinalIgnoreCase)
    {
        // -ão with -ãos / -ães
        ["mão"] = "mãos", ["irmão"] = "irmãos", ["cidadão"] = "cidadãos", ["cristão"] = "cristãos", ["órgão"] = "órgãos",
        ["sótão"] = "sótãos", ["bênção"] = "bênçãos", ["grão"] = "grãos", ["chão"] = "chãos", ["vão"] = "vãos",
        ["pão"] = "pães", ["cão"] = "cães", ["alemão"] = "alemães", ["capitão"] = "capitães", ["capelão"] = "capelães",
        ["charlatão"] = "charlatães", ["escrivão"] = "escrivães", ["tabelião"] = "tabeliães", ["sacristão"] = "sacristães",
        // others
        ["cônsul"] = "cônsules", ["mal"] = "males", ["deus"] = "deuses", ["adeus"] = "adeuses", ["qualquer"] = "quaisquer",
        ["caráter"] = "caracteres", ["júnior"] = "juniores", ["sênior"] = "seniores",
    };

    private static readonly HashSet<string> Invariable = new(StringComparer.OrdinalIgnoreCase)
    {
        "lápis", "tênis", "ônibus", "vírus", "pires", "atlas", "bônus", "ônus", "óculos", "cais", "tórax", "xerox", "fênix",
        "férias", "parabéns", "pêsames", "arredores", "núpcias", "oásis", "clímax", "ônix", "bíceps", "fórceps",
        "duplex", "triplex", "latex", "durex", "kit", "jeans",
    };

    private static readonly Dictionary<string, Gender> GenderExceptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dia"] = Gender.Masculine, ["mapa"] = Gender.Masculine, ["sofá"] = Gender.Masculine, ["problema"] = Gender.Masculine,
        ["sistema"] = Gender.Masculine, ["tema"] = Gender.Masculine, ["programa"] = Gender.Masculine, ["clima"] = Gender.Masculine,
        ["idioma"] = Gender.Masculine, ["planeta"] = Gender.Masculine, ["cometa"] = Gender.Masculine, ["pijama"] = Gender.Masculine,
        ["telefonema"] = Gender.Masculine, ["poema"] = Gender.Masculine, ["drama"] = Gender.Masculine, ["esquema"] = Gender.Masculine,
        ["guarda-chuva"] = Gender.Masculine, ["guarda-roupa"] = Gender.Masculine, ["porta-retrato"] = Gender.Masculine,
        ["mão"] = Gender.Feminine, ["pele"] = Gender.Feminine, ["árvore"] = Gender.Feminine, ["chave"] = Gender.Feminine,
        ["lente"] = Gender.Feminine, ["rede"] = Gender.Feminine, ["ponte"] = Gender.Feminine, ["parede"] = Gender.Feminine,
        ["fonte"] = Gender.Feminine, ["noite"] = Gender.Feminine, ["tarde"] = Gender.Feminine, ["sorte"] = Gender.Feminine,
        ["morte"] = Gender.Feminine, ["gente"] = Gender.Feminine, ["mente"] = Gender.Feminine, ["neve"] = Gender.Feminine,
        ["base"] = Gender.Feminine, ["frase"] = Gender.Feminine, ["classe"] = Gender.Feminine, ["fase"] = Gender.Feminine,
        ["cor"] = Gender.Feminine, ["dor"] = Gender.Feminine, ["flor"] = Gender.Feminine, ["mulher"] = Gender.Feminine,
        ["colher"] = Gender.Feminine, ["luz"] = Gender.Feminine, ["voz"] = Gender.Feminine, ["paz"] = Gender.Feminine,
        ["vez"] = Gender.Feminine, ["tv"] = Gender.Feminine, ["smart tv"] = Gender.Feminine, ["foto"] = Gender.Feminine,
        ["moto"] = Gender.Feminine, ["bike"] = Gender.Feminine, ["airfryer"] = Gender.Feminine, ["air fryer"] = Gender.Feminine,
        ["tribo"] = Gender.Feminine, ["libido"] = Gender.Feminine,
    };

    /// <summary>Adds or replaces an irregular plural.</summary>
    /// <param name="singular">Singular form.</param>
    /// <param name="plural">Plural form.</param>
    public static void AddException(string singular, string plural) => Exceptions[singular] = plural;

    /// <summary>Plural of a word or term. Only the first word of a multi-word term is inflected.</summary>
    /// <param name="term">Singular word or term, e.g. <c>fogão</c>, <c>fone de ouvido</c>.</param>
    public static string Pluralize(string term)
    {
        var trimmed = TextNormalizer.CollapseWhitespace(term);
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
        var head = space < 0 ? trimmed : trimmed[..space];
        var tail = space < 0 ? string.Empty : trimmed[space..];
        return MatchCase(head, PluralizeWord(head.ToLowerInvariant())) + tail;
    }

    /// <summary>
    /// Heuristic: true when the first word looks plural (ends in <c>s</c> and is not an invariable word such as
    /// "tênis" or "lápis"). "Óculos" counts as plural.
    /// </summary>
    /// <param name="term">Word or term.</param>
    public static bool IsPlural(string term)
    {
        var head = FirstWord(term);
        if (head.Length < 3)
        {
            return false;
        }

        if (head.Equals("óculos", StringComparison.OrdinalIgnoreCase) || Exceptions.ContainsValue(head))
        {
            return true;
        }

        if (Invariable.Contains(head) || !head.EndsWith('s'))
        {
            return false;
        }

        // Stressed final syllable: mês, gás, país, português are singular.
        if ("êáíóé".Contains(head[^2], StringComparison.Ordinal))
        {
            return false;
        }

        // Paroxytones ending in -is/-us/-ax with an accent before them are invariable singulars (tênis, ônibus, vírus).
        var beforeLastSyllable = head[..^2];
        return !(HasAccent(beforeLastSyllable) && (head.EndsWith("is", StringComparison.OrdinalIgnoreCase) || head.EndsWith("us", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Guesses the grammatical gender of the first word: exceptions first, then endings
    /// (<c>-a</c>, <c>-ção</c>, <c>-são</c>, <c>-dade</c>, <c>-gem</c>, <c>-ice</c>, <c>-ez</c> → feminine; <c>-ma</c> of Greek origin and the rest → masculine).
    /// </summary>
    /// <param name="term">Word or term, singular or plural.</param>
    public static Gender GuessGender(string term)
    {
        var clean = TextNormalizer.CollapseWhitespace(term).ToLowerInvariant();
        if (GenderExceptions.TryGetValue(clean, out var whole))
        {
            return whole;
        }

        var head = FirstWord(clean);
        if (GenderExceptions.TryGetValue(head, out var exception))
        {
            return exception;
        }

        var singular = head.EndsWith("as", StringComparison.Ordinal) ? head[..^1] : head;
        string[] feminineEndings = ["ção", "são", "dade", "tude", "gem", "ice", "ez", "ise"];
        if (feminineEndings.Any(e => singular.EndsWith(e, StringComparison.Ordinal)))
        {
            return Gender.Feminine;
        }

        if (singular.EndsWith("ões", StringComparison.Ordinal) || singular.EndsWith('ã'))
        {
            return singular.EndsWith('ã') ? Gender.Feminine : Gender.Masculine;
        }

        return singular.EndsWith('a') ? Gender.Feminine : Gender.Masculine;
    }

    private static string PluralizeWord(string word)
    {
        if (Exceptions.TryGetValue(word, out var exception))
        {
            return exception;
        }

        if (Invariable.Contains(word) || word.EndsWith('x') || !word.Any(char.IsLetter))
        {
            return word;
        }

        var last = word[^1];
        var accented = HasAccent(word[..^1]);

        if (word.EndsWith("ão", StringComparison.Ordinal))
        {
            return word[..^2] + "ões";
        }

        if ("aeiouáéíóúâêôã".Contains(last, StringComparison.Ordinal))
        {
            return word + "s";
        }

        switch (last)
        {
            case 'r':
            case 'z':
                return word + "es";
            case 'm':
                return word[..^1] + "ns";
            case 'n':
                return word + "s";
            case 's':
                if (word.EndsWith("ês", StringComparison.Ordinal))
                {
                    return word[..^2] + "eses"; // mês → meses, português → portugueses
                }

                if (word.EndsWith("ás", StringComparison.Ordinal))
                {
                    return word[..^2] + "ases"; // gás → gases
                }

                if (word.EndsWith("ís", StringComparison.Ordinal))
                {
                    return word + "es"; // país → países
                }

                // Paroxytones (lápis, ônibus, vírus) do not change; monosyllables take -es (gás handled above).
                return word.Length <= 3 ? word + "es" : word;
            case 'l':
                return PluralOfL(word, accented);
        }

        return word + "s";
    }

    private static string PluralOfL(string word, bool accented)
    {
        var stem = word[..^2];
        return word[^2] switch
        {
            'a' => stem + "ais",                                    // animal → animais
            'e' => stem + (accented ? "eis" : "éis"),               // nível → níveis, papel → papéis
            'o' => stem + (accented ? "ois" : "óis"),               // álcool → álcoois, lençol → lençóis
            'u' => stem + "uis",                                    // azul → azuis
            'i' => accented ? word[..^2] + "eis" : word[..^1] + "s", // fóssil → fósseis, funil → funis
            _ => word + "s",
        };
    }

    private static bool HasAccent(string text) => text.Any(c => "áéíóúâêôãõà".Contains(c, StringComparison.Ordinal));

    private static string FirstWord(string term)
    {
        var clean = TextNormalizer.CollapseWhitespace(term).ToLowerInvariant();
        var space = clean.IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? clean : clean[..space];
    }

    private static string MatchCase(string original, string inflected)
    {
        if (original.Length == 0 || inflected.Length == 0)
        {
            return inflected;
        }

        if (original.All(c => !char.IsLetter(c) || char.IsUpper(c)) && original.Any(char.IsLetter))
        {
            return inflected.ToUpperInvariant();
        }

        return char.IsUpper(original[0]) ? char.ToUpperInvariant(inflected[0]) + inflected[1..] : inflected;
    }
}
