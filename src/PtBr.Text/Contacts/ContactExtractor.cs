using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using PtBr.Text.Documents;

namespace PtBr.Text.Contacts;

/// <summary>Kind of contact found in text.</summary>
public enum ContactKind
{
    /// <summary>Brazilian phone number.</summary>
    Phone,

    /// <summary>E-mail address.</summary>
    Email,

    /// <summary>Web address.</summary>
    Url,
}

/// <summary>A contact found in text.</summary>
/// <param name="Kind">Phone, e-mail or URL.</param>
/// <param name="Text">The text exactly as it appears.</param>
/// <param name="Normalized">Formatted phone (<c>(11) 98765-4321</c>), lower-case e-mail, or absolute URL.</param>
/// <param name="Href">Value for an HTML link: <c>tel:+55...</c>, <c>mailto:...</c> or the URL.</param>
/// <param name="Index">Position in the source text.</param>
public sealed record Contact(ContactKind Kind, string Text, string Normalized, string Href, int Index)
{
    /// <summary>Length of <see cref="Text"/>.</summary>
    public int Length => Text.Length;
}

/// <summary>Options for <see cref="ContactExtractor"/>.</summary>
public sealed class ContactOptions
{
    /// <summary>DDD assumed for 8- or 9-digit numbers written without one (e.g. "3456-7890"). Default: none (ignored).</summary>
    public string? DefaultAreaCode { get; set; }

    /// <summary>Recognise bare domains (<c>loja.com.br</c>) and not only <c>http://</c> and <c>www.</c> addresses. Default: true.</summary>
    public bool BareDomains { get; set; } = true;
}

/// <summary>
/// Finds phones, e-mails and web addresses in free text — the kind of messy text found in "Fale conosco"
/// pages, SAC fields and store footers — and turns them into links.
/// </summary>
public static class ContactExtractor
{
    private static readonly RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    private static readonly Regex Email = new("[A-Z0-9._%+-]+@[A-Z0-9-]+(?:\\.[A-Z0-9-]+)*\\.[A-Z]{2,24}", Options, Timeout);

    private static readonly Regex Url = new(
        "(?:https?://|www\\.)[^\\s<>\"']+" +
        "|(?<![@\\w.-])[a-z0-9][a-z0-9-]*(?:\\.[a-z0-9-]+)*\\.(?:com\\.br|net\\.br|org\\.br|gov\\.br|edu\\.br|ind\\.br|com|net|org|br|io|app|shop|store|online|site|dev)(?![\\w-])(?:/[^\\s<>\"']*)?",
        Options, Timeout);

    private static readonly Regex PhoneCandidate = new(
        "(?<![\\d])(?:0[3589]00[\\s.-]?\\d{3}[\\s.-]?\\d{4}" +
        "|[34]00\\d[\\s.-]?\\d{4}" +
        "|(?:\\+?55[\\s.-]*)?(?:\\(?0?\\d{2}\\)?[\\s.-]*)?9?[\\s.]?\\d{4}[\\s.-]?\\d{4})(?![\\d])",
        Options, Timeout);

    private const string TrailingPunctuation = ".,;:!?)]}'\"";

    /// <summary>Finds every contact in <paramref name="text"/>, in order of appearance, without duplicates.</summary>
    /// <param name="text">Free text.</param>
    /// <param name="options">Options; null = defaults.</param>
    public static IReadOnlyList<Contact> Extract(string? text, ContactOptions? options = null)
    {
        options ??= new ContactOptions();
        var found = new List<Contact>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return found;
        }

        var taken = new bool[text.Length];

        foreach (Match m in Email.Matches(text))
        {
            var email = m.Value.ToLowerInvariant();
            Add(found, taken, new Contact(ContactKind.Email, m.Value, email, "mailto:" + email, m.Index));
        }

        foreach (Match m in Url.Matches(text))
        {
            var value = m.Value.TrimEnd(TrailingPunctuation.ToCharArray());
            if (value.Length == 0 || Overlaps(taken, m.Index, value.Length) || (!options.BareDomains && !LooksExplicit(value)))
            {
                continue;
            }

            var absolute = value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value : "https://" + value;
            Add(found, taken, new Contact(ContactKind.Url, value, absolute, absolute, m.Index));
        }

        foreach (Match m in PhoneCandidate.Matches(text))
        {
            var value = m.Value.Trim();
            var index = m.Index + m.Value.IndexOf(value, StringComparison.Ordinal);
            if (Overlaps(taken, index, value.Length))
            {
                continue;
            }

            var phone = Phone.Parse(value);
            if (phone is null && options.DefaultAreaCode is { } ddd)
            {
                var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
                if (digits.Length is 8 or 9)
                {
                    phone = Phone.Parse(ddd + digits);
                }
            }

            if (phone is not null)
            {
                Add(found, taken, new Contact(ContactKind.Phone, value, phone.Formatted, phone.TelUri, index));
            }
        }

        return found
            .GroupBy(c => (c.Kind, c.Normalized))
            .Select(g => g.First())
            .OrderBy(c => c.Index)
            .ToList();
    }

    /// <summary>
    /// Returns <paramref name="text"/> as HTML, with contacts turned into links and everything else HTML-encoded.
    /// </summary>
    /// <param name="text">Free text.</param>
    /// <param name="options">Options; null = defaults.</param>
    /// <param name="urlRel">Value of the <c>rel</c> attribute for web links. Default: <c>nofollow noopener</c>.</param>
    public static string Linkify(string? text, ContactOptions? options = null, string urlRel = "nofollow noopener")
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var all = AllOccurrences(text, options);
        var builder = new StringBuilder(text.Length + (all.Count * 40));
        var position = 0;
        foreach (var contact in all)
        {
            builder.Append(WebUtility.HtmlEncode(text[position..contact.Index]));
            var rel = contact.Kind == ContactKind.Url ? $" rel=\"{WebUtility.HtmlEncode(urlRel)}\"" : string.Empty;
            builder.Append("<a href=\"").Append(WebUtility.HtmlEncode(contact.Href)).Append('"').Append(rel).Append('>')
                .Append(WebUtility.HtmlEncode(contact.Text)).Append("</a>");
            position = contact.Index + contact.Length;
        }

        builder.Append(WebUtility.HtmlEncode(text[position..]));
        return builder.ToString();
    }

    private static List<Contact> AllOccurrences(string text, ContactOptions? options)
    {
        // Extract() removes duplicates; for linkify every occurrence must become a link.
        var unique = Extract(text, options);
        var all = new List<Contact>();
        var taken = new bool[text.Length];
        foreach (var contact in unique.OrderByDescending(c => c.Length))
        {
            var start = 0;
            while ((start = text.IndexOf(contact.Text, start, StringComparison.Ordinal)) >= 0)
            {
                if (!Overlaps(taken, start, contact.Length))
                {
                    Add(all, taken, contact with { Index = start });
                }

                start += contact.Length;
            }
        }

        return all.OrderBy(c => c.Index).ToList();
    }

    private static bool LooksExplicit(string url) =>
        url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("www.", StringComparison.OrdinalIgnoreCase);

    private static void Add(List<Contact> list, bool[] taken, Contact contact)
    {
        list.Add(contact);
        Array.Fill(taken, true, contact.Index, contact.Length);
    }

    private static bool Overlaps(bool[] taken, int index, int length)
    {
        for (var i = index; i < index + length && i < taken.Length; i++)
        {
            if (taken[i])
            {
                return true;
            }
        }

        return false;
    }
}
