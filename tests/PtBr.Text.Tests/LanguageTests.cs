using PtBr.Text.Contacts;
using PtBr.Text.Inflection;
using PtBr.Text.Keywords;
using PtBr.Text.Narratives;
using PtBr.Text.Numbers;

namespace PtBr.Text.Tests;

public class InflectorTests
{
    [Theory]
    [InlineData("geladeira", "geladeiras")]
    [InlineData("fogão", "fogões")]
    [InlineData("televisão", "televisões")]
    [InlineData("mão", "mãos")]
    [InlineData("pão", "pães")]
    [InlineData("flor", "flores")]
    [InlineData("luz", "luzes")]
    [InlineData("jardim", "jardins")]
    [InlineData("animal", "animais")]
    [InlineData("papel", "papéis")]
    [InlineData("nível", "níveis")]
    [InlineData("lençol", "lençóis")]
    [InlineData("álcool", "álcoois")]
    [InlineData("azul", "azuis")]
    [InlineData("funil", "funis")]
    [InlineData("fóssil", "fósseis")]
    [InlineData("mês", "meses")]
    [InlineData("português", "portugueses")]
    [InlineData("gás", "gases")]
    [InlineData("país", "países")]
    [InlineData("tênis", "tênis")]
    [InlineData("lápis", "lápis")]
    [InlineData("ônibus", "ônibus")]
    [InlineData("tórax", "tórax")]
    [InlineData("fone de ouvido", "fones de ouvido")]
    [InlineData("Fogão", "Fogões")]
    [InlineData("NOTEBOOK", "NOTEBOOKS")]
    public void Pluralizes(string singular, string plural) => Assert.Equal(plural, Inflector.Pluralize(singular));

    [Theory]
    [InlineData("geladeiras", true)]
    [InlineData("fones de ouvido", true)]
    [InlineData("óculos", true)]
    [InlineData("geladeira", false)]
    [InlineData("tênis", false)]
    [InlineData("ônibus", false)]
    [InlineData("mês", false)]
    [InlineData("país", false)]
    public void Detects_plural(string term, bool plural) => Assert.Equal(plural, Inflector.IsPlural(term));

    [Theory]
    [InlineData("geladeira", Gender.Feminine)]
    [InlineData("fogão", Gender.Masculine)]
    [InlineData("televisão", Gender.Feminine)]
    [InlineData("panelas", Gender.Feminine)]
    [InlineData("problema", Gender.Masculine)]
    [InlineData("sofá", Gender.Masculine)]
    [InlineData("smart tv", Gender.Feminine)]
    [InlineData("air fryer", Gender.Feminine)]
    [InlineData("notebook", Gender.Masculine)]
    [InlineData("cadeira gamer", Gender.Feminine)]
    [InlineData("bicicleta elétrica", Gender.Feminine)]
    [InlineData("liquidificador", Gender.Masculine)]
    [InlineData("cor", Gender.Feminine)]
    public void Guesses_gender(string term, Gender gender) => Assert.Equal(gender, Inflector.GuessGender(term));
}

public class SeoTitlesTests
{
    [Theory]
    [InlineData("geladeira", false, "Qual a melhor geladeira?")]
    [InlineData("fogão", false, "Qual o melhor fogão?")]
    [InlineData("panelas", false, "Quais as melhores panelas?")]
    [InlineData("geladeira", true, "Quais as melhores geladeiras?")]
    [InlineData("fone de ouvido", true, "Quais os melhores fones de ouvido?")]
    [InlineData("tênis de corrida", false, "Qual o melhor tênis de corrida?")]
    public void Question(string term, bool plural, string expected) =>
        Assert.Equal(expected, SeoTitles.Question(term, plural: plural));

    [Fact]
    public void Worst_question() =>
        Assert.Equal("Qual a pior air fryer?", SeoTitles.Question("air fryer", Ranking.Worst));

    [Theory]
    [InlineData("geladeira", 10, 2026, "As 10 melhores geladeiras para comprar em 2026")]
    [InlineData("fogão", 5, null, "Os 5 melhores fogões para comprar")]
    [InlineData("smart tv", 1, null, "A melhor smart tv para comprar")]
    public void Headline(string term, int count, int? year, string expected) =>
        Assert.Equal(expected, SeoTitles.Headline(term, count, year: year));
}

public class ContactExtractorTests
{
    private const string Sac =
        "SAC: (11) 98765-4321 / 0800 123 4567. E-mail: Atendimento@Loja-Exemplo.com.br - site www.loja-exemplo.com.br. " +
        "Empresa Exemplo Ltda.ME, CNPJ 11.222.333/0001-81, CEP 01310-100.";

    [Fact]
    public void Extracts_phones_emails_and_urls()
    {
        var contacts = ContactExtractor.Extract(Sac);

        Assert.Equal(4, contacts.Count);
        Assert.Equal((ContactKind.Phone, "(11) 98765-4321", "tel:+5511987654321"), (contacts[0].Kind, contacts[0].Normalized, contacts[0].Href));
        Assert.Equal((ContactKind.Phone, "0800 123 4567", "tel:08001234567"), (contacts[1].Kind, contacts[1].Normalized, contacts[1].Href));
        Assert.Equal((ContactKind.Email, "atendimento@loja-exemplo.com.br"), (contacts[2].Kind, contacts[2].Normalized));
        Assert.Equal((ContactKind.Url, "https://www.loja-exemplo.com.br"), (contacts[3].Kind, contacts[3].Normalized));
    }

    [Fact]
    public void Does_not_take_cnpj_cep_or_company_suffix_as_contacts()
    {
        var texts = ContactExtractor.Extract(Sac).Select(c => c.Text).ToList();
        Assert.DoesNotContain(texts, t => t.Contains("Ltda", StringComparison.Ordinal) || t.Contains("01310", StringComparison.Ordinal) || t.Contains("0001", StringComparison.Ordinal));
    }

    [Fact]
    public void Bare_domains_can_be_disabled()
    {
        var contacts = ContactExtractor.Extract("Visite loja-exemplo.com.br", new ContactOptions { BareDomains = false });
        Assert.Empty(contacts);
        Assert.Single(ContactExtractor.Extract("Visite loja-exemplo.com.br"));
    }

    [Fact]
    public void Default_area_code_for_local_numbers()
    {
        var contact = Assert.Single(ContactExtractor.Extract("Ligue 3456-7890", new ContactOptions { DefaultAreaCode = "71" }));
        Assert.Equal("(71) 3456-7890", contact.Normalized);
    }

    [Fact]
    public void Linkify_encodes_and_links_every_occurrence()
    {
        var html = ContactExtractor.Linkify("Ligue (11) 98765-4321 <agora> ou (11) 98765-4321");

        Assert.Equal(
            "Ligue <a href=\"tel:+5511987654321\">(11) 98765-4321</a> &lt;agora&gt; ou <a href=\"tel:+5511987654321\">(11) 98765-4321</a>",
            html);
    }

    [Fact]
    public void Linkify_urls_get_rel()
    {
        var html = ContactExtractor.Linkify("Acesse https://loja-exemplo.com.br/promo.");
        Assert.Equal("Acesse <a href=\"https://loja-exemplo.com.br/promo\" rel=\"nofollow noopener\">https://loja-exemplo.com.br/promo</a>.", html);
    }
}

public class RakeTests
{
    [Fact]
    public void Extracts_phrases_between_stopwords_keeping_accents()
    {
        var phrases = RakeExtractor.Extract("A bateria dura muito e a câmera noturna surpreende. A tela é ótima.")
            .Select(p => p.Phrase)
            .ToList();

        Assert.Equal("câmera noturna surpreende", phrases[0]);
        Assert.Contains("bateria dura", phrases);
        Assert.Contains("tela", phrases);
        Assert.Contains("ótima", phrases);
    }

    [Fact]
    public void Conjunction_list_keeps_long_phrases()
    {
        var options = new RakeOptions { Stopwords = Stopwords.Conjunctions, MaxWords = 8 };
        var phrases = RakeExtractor.Extract("Entrega rápida e produto muito bem embalado, no entanto veio sem manual.", options: options)
            .Select(p => p.Phrase)
            .ToList();

        Assert.Contains("entrega rápida", phrases);
        Assert.Contains("produto muito bem embalado", phrases);
        Assert.Contains("veio sem manual", phrases);
    }

    [Fact]
    public void Counts_repeated_phrases_and_limits_results()
    {
        var phrases = RakeExtractor.Extract("Ótimo custo-benefício. Recomendo, ótimo custo-benefício!", top: 1);

        var phrase = Assert.Single(phrases);
        Assert.Equal("ótimo custo-benefício", phrase.Phrase);
        Assert.Equal(2, phrase.Occurrences);
    }

    [Fact]
    public void Stopwords_ignore_accents_and_case()
    {
        Assert.True(Stopwords.Contains("NÃO"));
        Assert.True(Stopwords.Contains("nao"));
        Assert.False(Stopwords.Contains("geladeira"));
    }
}

public class PriceHistoryNarratorTests
{
    private static readonly DateTime Today = new(2026, 10, 14);

    [Fact]
    public void Describes_history_across_stores()
    {
        var history = new[]
        {
            new PriceObservation(new DateTime(2026, 6, 10), "Loja A", 3999.90m),
            new PriceObservation(new DateTime(2026, 8, 1), "Loja B", 3499.00m),
            new PriceObservation(new DateTime(2026, 10, 13), "Loja A", 3799.90m),
            new PriceObservation(new DateTime(2026, 10, 13), "Loja B", 3599.00m),
        };

        var narrative = PriceHistoryNarrator.Describe("Geladeira Exemplo 375L", history, Today);

        Assert.Equal(new[]
        {
            "Monitoramos o preço de Geladeira Exemplo 375L há 4 meses.",
            "O preço médio é R$ 3.724,45.",
            "O preço varia de R$ 3.499,00 a R$ 3.999,90, dependendo da loja.",
            "O menor preço registrado foi R$ 3.499,00, em 01/08/2026, na loja Loja B.",
            "O maior preço registrado foi R$ 3.999,90, em 10/06/2026, na loja Loja A.",
            "A oferta mais barata no momento é R$ 3.599,00, na loja Loja B.",
        }, narrative.Sentences);

        Assert.Equal(new[]
        {
            "Qual o preço de Geladeira Exemplo 375L?",
            "Qual o menor preço de Geladeira Exemplo 375L?",
            "Onde comprar Geladeira Exemplo 375L mais barato?",
        }, narrative.Faq.Select(f => f.Question));
    }

    [Fact]
    public void Single_store_short_history()
    {
        var history = new[]
        {
            new PriceObservation(Today.AddDays(-10), "Loja A", 100m),
            new PriceObservation(Today.AddDays(-1), "Loja A", 100m),
        };

        var narrative = PriceHistoryNarrator.Describe("Produto", history, Today);

        Assert.Equal(new[] { "Monitoramos o preço de Produto há 10 dias.", "O preço médio é R$ 100,00." }, narrative.Sentences);
        Assert.Single(narrative.Faq);
    }

    [Fact]
    public void Empty_history() => Assert.Empty(PriceHistoryNarrator.Describe("Produto", [], Today).Sentences);
}
