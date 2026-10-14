using PtBr.Text.Numbers;

namespace PtBr.Text.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("Ação, coração e pão", "Acao, coracao e pao")]
    [InlineData("ÁÉÍÓÚ âêô ãõ à ç ü ñ", "AEIOU aeo ao a c u n")]
    [InlineData("Æsir straße Œuvre", "AEsir strasse OEuvre")]
    [InlineData("sem acento", "sem acento")]
    [InlineData("", "")]
    public void Removes_accents(string input, string expected) => Assert.Equal(expected, TextNormalizer.RemoveAccents(input));

    [Theory]
    [InlineData("Fogão 4 Bocas & Forno", "fogao-4-bocas-e-forno")]
    [InlineData("  Geladeira   Frost-Free 375L!! ", "geladeira-frost-free-375l")]
    [InlineData("Caixa d'água 1000 litros", "caixa-dagua-1000-litros")]
    [InlineData("Nº 1 em vendas – 2ª edição", "no-1-em-vendas-2a-edicao")]
    [InlineData("---", "")]
    public void Slugifies(string input, string expected) => Assert.Equal(expected, TextNormalizer.Slugify(input));

    [Fact]
    public void Slug_is_cut_at_word_boundary()
    {
        var slug = TextNormalizer.Slugify("Geladeira Frost Free Duplex Inox 375 Litros", new SlugOptions { MaxLength = 20 });
        Assert.Equal("geladeira-frost-free", slug);
    }

    [Fact]
    public void Slug_with_custom_separator() =>
        Assert.Equal("fogao_4_bocas", TextNormalizer.Slugify("Fogão 4 bocas", new SlugOptions { Separator = '_' }));

    [Theory]
    [InlineData("GELADEIRA FROST FREE DE 375 LITROS", "Geladeira Frost Free de 375 Litros")]
    [InlineData("smart tv LED 50 polegadas", "Smart Tv LED 50 Polegadas")]
    [InlineData("tênis da Nike para corrida", "Tênis da Nike para Corrida")]
    [InlineData("iPhone 15 de 128GB", "iPhone 15 de 128GB")]
    [InlineData("GELADEIRA 375L INOX", "Geladeira 375L Inox")]
    public void Title_case(string input, string expected) => Assert.Equal(expected, TextNormalizer.ToTitleCase(input));

    [Fact]
    public void Collapses_whitespace() => Assert.Equal("a b c", TextNormalizer.CollapseWhitespace("  a \t b\r\n c "));
}

public class NumberToWordsTests
{
    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "um")]
    [InlineData(10, "dez")]
    [InlineData(14, "quatorze")]
    [InlineData(21, "vinte e um")]
    [InlineData(100, "cem")]
    [InlineData(101, "cento e um")]
    [InlineData(150, "cento e cinquenta")]
    [InlineData(600, "seiscentos")]
    [InlineData(999, "novecentos e noventa e nove")]
    [InlineData(1000, "mil")]
    [InlineData(1001, "mil e um")]
    [InlineData(1100, "mil e cem")]
    [InlineData(1250, "mil duzentos e cinquenta")]
    [InlineData(2005, "dois mil e cinco")]
    [InlineData(21000, "vinte e um mil")]
    [InlineData(100000, "cem mil")]
    [InlineData(1000000, "um milhão")]
    [InlineData(1000001, "um milhão e um")]
    [InlineData(1200000, "um milhão e duzentos mil")]
    [InlineData(2500000, "dois milhões e quinhentos mil")]
    [InlineData(1234567, "um milhão duzentos e trinta e quatro mil quinhentos e sessenta e sete")]
    [InlineData(1000000000, "um bilhão")]
    [InlineData(-15, "menos quinze")]
    public void Cardinals(int value, string expected) => Assert.Equal(expected, NumberToWords.ToWords(value));

    [Theory]
    [InlineData(1, "uma")]
    [InlineData(2, "duas")]
    [InlineData(22, "vinte e duas")]
    [InlineData(200, "duzentas")]
    [InlineData(2200, "duas mil e duzentas")]
    [InlineData(2000000, "dois milhões")] // milhão is masculine even for feminine nouns
    public void Feminine(int value, string expected) => Assert.Equal(expected, NumberToWords.ToWords(value, Gender.Feminine));

    [Theory]
    [InlineData("0", "zero reais")]
    [InlineData("0.01", "um centavo")]
    [InlineData("0.5", "cinquenta centavos")]
    [InlineData("1", "um real")]
    [InlineData("1.5", "um real e cinquenta centavos")]
    [InlineData("2.01", "dois reais e um centavo")]
    [InlineData("1299.90", "mil duzentos e noventa e nove reais e noventa centavos")]
    [InlineData("1000000", "um milhão de reais")]
    [InlineData("2000000", "dois milhões de reais")]
    [InlineData("1500000", "um milhão e quinhentos mil reais")]
    [InlineData("-10", "menos dez reais")]
    public void Reais(string amount, string expected) =>
        Assert.Equal(expected, NumberToWords.ToReais(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Out_of_range_throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NumberToWords.ToWords(NumberToWords.MaxValue + 1));
}

public class DurationParserTests
{
    [Theory]
    [InlineData("1 hora e 30 minutos", "PT1H30M")]
    [InlineData("1h30", "PT1H30M")]
    [InlineData("1h 30min", "PT1H30M")]
    [InlineData("1:30", "PT1H30M")]
    [InlineData("90 minutos", "PT1H30M")]
    [InlineData("meia hora", "PT30M")]
    [InlineData("uma hora e meia", "PT1H30M")]
    [InlineData("duas horas", "PT2H")]
    [InlineData("45 min", "PT45M")]
    [InlineData("2 dias", "P2D")]
    [InlineData("1 dia e 2 horas", "P1DT2H")]
    [InlineData("30 segundos", "PT30S")]
    [InlineData("Tempo de preparo: 40 minutos", "PT40M")]
    [InlineData("1,5 hora", "PT1H30M")]
    public void Parses(string text, string iso) => Assert.Equal(iso, DurationParser.ToIso8601(text));

    [Theory]
    [InlineData("rápido")]
    [InlineData("nenhum minuto")]
    [InlineData("")]
    public void Returns_null_without_duration(string text) => Assert.Null(DurationParser.ToIso8601(text));

    [Fact]
    public void Zero_duration_is_pt0s() => Assert.Equal("PT0S", DurationParser.ToIso8601(TimeSpan.Zero));
}

public class BrazilianFormatTests
{
    [Theory]
    [InlineData("1299.9", "R$ 1.299,90")]
    [InlineData("0.5", "R$ 0,50")]
    [InlineData("1234567.891", "R$ 1.234.567,89")]
    [InlineData("-10", "-R$ 10,00")]
    public void Money(string value, string expected) =>
        Assert.Equal(expected, BrazilianFormat.Money(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Number_without_decimals() => Assert.Equal("1.234", BrazilianFormat.Number(1234.4m, 0));

    [Fact]
    public void Date() => Assert.Equal("05/03/2026", BrazilianFormat.Date(new DateTime(2026, 3, 5)));
}
