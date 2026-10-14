using PtBr.Text.Numbers;

namespace PtBr.Text.Narratives;

/// <summary>A price seen in a store on a date.</summary>
/// <param name="Date">When the price was observed.</param>
/// <param name="Store">Store name.</param>
/// <param name="Price">Price in reais.</param>
public sealed record PriceObservation(DateTime Date, string Store, decimal Price);

/// <summary>A question and its answer, ready for a FAQ section or a schema.org <c>FAQPage</c>.</summary>
/// <param name="Question">The question.</param>
/// <param name="Answer">The answer, plain text.</param>
public sealed record FaqEntry(string Question, string Answer);

/// <summary>The text produced from a price history.</summary>
/// <param name="Sentences">Sentences in reading order.</param>
/// <param name="Faq">Questions and answers derived from the same facts.</param>
public sealed record PriceNarrative(IReadOnlyList<string> Sentences, IReadOnlyList<FaqEntry> Faq)
{
    /// <summary>All sentences as one paragraph.</summary>
    public string Text => string.Join(" ", Sentences);
}

/// <summary>
/// Turns a product's price history into plain Portuguese sentences and FAQ entries: how long it has been
/// monitored, average price, price range across stores, lowest and highest price ever, and today's best offer.
/// Facts only — no invented opinions.
/// </summary>
/// <example>
/// <code>
/// var narrative = PriceHistoryNarrator.Describe("Geladeira Frost Free 375L", history, today: DateTime.Today);
/// Console.WriteLine(narrative.Text);
/// // "Monitoramos o preço de Geladeira Frost Free 375L há 4 meses. O preço médio é R$ 3.512,40. ..."
/// </code>
/// </example>
public static class PriceHistoryNarrator
{
    /// <summary>Describes the price history of <paramref name="product"/>.</summary>
    /// <param name="product">Product name as it should appear in the text.</param>
    /// <param name="history">Observations (any order). Non-positive prices are ignored.</param>
    /// <param name="today">Reference date; null = <see cref="DateTime.Today"/>.</param>
    public static PriceNarrative Describe(string product, IEnumerable<PriceObservation> history, DateTime? today = null)
    {
        var now = (today ?? DateTime.Today).Date;
        var points = history.Where(h => h.Price > 0).OrderBy(h => h.Date).ToList();
        if (points.Count == 0)
        {
            return new PriceNarrative([], []);
        }

        var sentences = new List<string>();
        var faq = new List<FaqEntry>();

        var days = (now - points[0].Date.Date).Days;
        sentences.Add(days switch
        {
            <= 1 => $"Começamos a monitorar o preço de {product} recentemente.",
            <= 60 => $"Monitoramos o preço de {product} há {days} dias.",
            _ => $"Monitoramos o preço de {product} há {(int)Math.Round(days / 30.0)} meses.",
        });

        var average = Math.Round(points.Average(p => p.Price), 2);
        var min = points.MinBy(p => p.Price)!;
        var max = points.MaxBy(p => p.Price)!;
        var stores = points.Select(p => p.Store).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        var priceAnswer = new List<string> { $"O preço médio é {BrazilianFormat.Money(average)}." };
        if (min.Price != max.Price)
        {
            priceAnswer.Add(stores > 1
                ? $"O preço varia de {BrazilianFormat.Money(min.Price)} a {BrazilianFormat.Money(max.Price)}, dependendo da loja."
                : $"O preço variou de {BrazilianFormat.Money(min.Price)} a {BrazilianFormat.Money(max.Price)} no período.");
        }

        sentences.AddRange(priceAnswer);
        faq.Add(new FaqEntry($"Qual o preço de {product}?", string.Join(" ", priceAnswer)));

        if (min.Price != max.Price)
        {
            var lowest = $"O menor preço registrado foi {BrazilianFormat.Money(min.Price)}, em {BrazilianFormat.Date(min.Date)}, na loja {min.Store}.";
            var highest = $"O maior preço registrado foi {BrazilianFormat.Money(max.Price)}, em {BrazilianFormat.Date(max.Date)}, na loja {max.Store}.";
            sentences.Add(lowest);
            sentences.Add(highest);
            faq.Add(new FaqEntry($"Qual o menor preço de {product}?", lowest));
        }

        // Today's best offer: latest observation per store, then the cheapest.
        var latestPerStore = points
            .GroupBy(p => p.Store, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.MaxBy(p => p.Date)!)
            .ToList();
        var best = latestPerStore.MinBy(p => p.Price)!;
        if (stores > 1)
        {
            var offer = $"A oferta mais barata no momento é {BrazilianFormat.Money(best.Price)}, na loja {best.Store}.";
            sentences.Add(offer);
            faq.Add(new FaqEntry($"Onde comprar {product} mais barato?", offer));
        }

        return new PriceNarrative(sentences, faq);
    }
}
