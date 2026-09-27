using System.Text.RegularExpressions;

namespace EKitap.Api.Services;

public static partial class ContactCleaner
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = EmailRegex().Replace(text, " ");
        cleaned = PhoneRegex().Replace(cleaned, " ");
        cleaned = CompactMobileRegex().Replace(cleaned, " ");
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();
        cleaned = EmptyLabelRegex().Replace(cleaned, " ").Trim();
        return WhitespaceRegex().Replace(cleaned, " ").Trim(' ', '-', ':', '|', '/');
    }

    public static bool ContainsContact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return EmailRegex().IsMatch(text)
            || PhoneRegex().IsMatch(text)
            || CompactMobileRegex().IsMatch(text);
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-İıÖöÜüŞşĞğÇç]+@[A-Za-z0-9.\-İıÖöÜüŞşĞğÇç]+\.[A-Za-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(
        @"(?<!\d)(?:(?:\+|00)\s?90[\s.\-]?)?(?:0[\s.\-]?)?\(?[2-5]\d{2}\)?(?:[\s.\-]+\d{3}[\s.\-]+\d{2}[\s.\-]+\d{2}|\d{7})(?!\d)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+90|0090|0)?5\d{9}(?!\d)")]
    private static partial Regex CompactMobileRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(
        @"(?i)\b(e-?posta|email|mail|tel|telefon|gsm|phone|fax|faks)\b\s*[:\-]?\s*(?=$|\|)",
        RegexOptions.IgnoreCase)]
    private static partial Regex EmptyLabelRegex();
}
