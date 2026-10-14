using System.Text;

namespace PtBr.Text.Documents;

internal static class DocumentText
{
    public static string Digits(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    public static bool AreDigits(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            if (!char.IsAsciiDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    public static bool AllSame(string value) => value.Length > 0 && value.All(c => c == value[0]);
}
