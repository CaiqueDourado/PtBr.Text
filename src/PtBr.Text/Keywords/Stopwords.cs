namespace PtBr.Text.Keywords;

/// <summary>Portuguese stopword lists, compared without accents and case.</summary>
public static class Stopwords
{
    /// <summary>
    /// General-purpose list: articles, prepositions and contractions, pronouns, conjunctions, common adverbs and
    /// the most frequent forms of ser, estar, ter, haver, ir and poder.
    /// </summary>
    public static IReadOnlySet<string> Portuguese { get; } = Build(
        // articles, prepositions, contractions
        "a", "o", "as", "os", "um", "uma", "uns", "umas", "ao", "aos", "à", "às", "de", "do", "da", "dos", "das", "dum", "duma",
        "em", "no", "na", "nos", "nas", "num", "numa", "por", "pelo", "pela", "pelos", "pelas", "para", "pra", "pro", "com",
        "sem", "sob", "sobre", "entre", "até", "após", "ante", "contra", "desde", "perante", "trás", "dele", "dela", "deles",
        "delas", "nele", "nela", "neles", "nelas", "deste", "desta", "destes", "destas", "desse", "dessa", "desses", "dessas",
        "daquele", "daquela", "daqueles", "daquelas", "neste", "nesta", "nesse", "nessa", "naquele", "naquela", "disso", "disto",
        "nisso", "nisto", "daquilo", "naquilo",
        // pronouns and determiners
        "eu", "tu", "ele", "ela", "nós", "vós", "eles", "elas", "você", "vocês", "me", "te", "se", "lhe", "lhes", "nos", "vos",
        "mim", "ti", "si", "comigo", "contigo", "conosco", "meu", "minha", "meus", "minhas", "teu", "tua", "teus", "tuas",
        "seu", "sua", "seus", "suas", "nosso", "nossa", "nossos", "nossas", "este", "esta", "estes", "estas", "esse", "essa",
        "esses", "essas", "aquele", "aquela", "aqueles", "aquelas", "isto", "isso", "aquilo", "que", "quem", "qual", "quais",
        "cujo", "cuja", "onde", "quando", "como", "quanto", "quanta", "quantos", "quantas", "outro", "outra", "outros", "outras",
        "todo", "toda", "todos", "todas", "algum", "alguma", "alguns", "algumas", "nenhum", "nenhuma", "cada", "tal", "tais",
        "mesmo", "mesma", "mesmos", "mesmas", "próprio", "própria",
        // conjunctions
        "e", "ou", "mas", "porém", "contudo", "todavia", "entretanto", "porque", "pois", "logo", "portanto", "se", "embora",
        "caso", "conforme", "nem", "enquanto", "senão",
        // adverbs
        "não", "sim", "já", "ainda", "também", "só", "apenas", "muito", "muita", "muitos", "muitas", "pouco", "pouca", "mais",
        "menos", "bem", "mal", "tão", "tanto", "tanta", "aqui", "aí", "ali", "lá", "cá", "agora", "sempre", "nunca", "depois",
        "antes", "então", "assim", "quase", "bastante", "demais", "talvez", "inclusive",
        // ser / estar / ter / haver / ir / poder
        "ser", "é", "são", "era", "eram", "foi", "foram", "fosse", "sejam", "seja", "sou", "somos", "será", "serão", "sido",
        "estar", "está", "estão", "estava", "estavam", "esteve", "estou", "estamos", "estado",
        "ter", "tem", "têm", "tinha", "tinham", "teve", "tenho", "temos", "tido", "terá",
        "haver", "há", "havia", "houve", "ir", "vai", "vão", "vou", "pode", "podem", "poder", "podia", "deve", "devem");

    /// <summary>
    /// Only conjunctions and a few connectors. Used as RAKE delimiters this keeps phrases long, which works well for
    /// short texts such as reviews ("tela grande e bateria que dura o dia todo").
    /// </summary>
    public static IReadOnlySet<string> Conjunctions { get; } = Build(
        "e", "ou", "mas", "porém", "contudo", "todavia", "entretanto", "no entanto", "porque", "pois", "portanto", "embora",
        "também", "além disso", "então");

    /// <summary>True when <paramref name="word"/> is in <paramref name="list"/> (accent- and case-insensitive).</summary>
    /// <param name="word">The word.</param>
    /// <param name="list">The list; null = <see cref="Portuguese"/>.</param>
    public static bool Contains(string word, IReadOnlySet<string>? list = null) =>
        (list ?? Portuguese).Contains(Key(word));

    internal static string Key(string word) => TextNormalizer.RemoveAccents(word).ToLowerInvariant();

    private static HashSet<string> Build(params string[] words) =>
        new(words.Select(Key), StringComparer.Ordinal);
}
