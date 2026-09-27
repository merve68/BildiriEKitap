using EKitap.Api.Services;

namespace EKitap.Tests;

public class ContactCleanerTests
{
    [Fact]
    public void Removes_email_and_keeps_address()
    {
        const string input = "İnfo@akap.tr - Yıldızevler Mah. 708. Cad. No:14/3 Çankaya-ANKARA";
        var cleaned = ContactCleaner.Clean(input);

        Assert.DoesNotContain("@", cleaned);
        Assert.Contains("Yıldızevler Mah. 708. Cad. No:14/3 Çankaya-ANKARA", cleaned);
    }

    [Fact]
    public void Removes_spaced_landline()
    {
        var cleaned = ContactCleaner.Clean("Sekreterya 0312 343 10 33 adres aynı kalsın.");
        Assert.DoesNotContain("0312", cleaned);
        Assert.Contains("adres aynı kalsın", cleaned);
    }

    [Fact]
    public void Removes_international_mobile()
    {
        var cleaned = ContactCleaner.Clean("GSM: +90 532 111 22 33");
        Assert.DoesNotContain("532", cleaned);
    }

    [Fact]
    public void Removes_compact_mobile()
    {
        var cleaned = ContactCleaner.Clean("Ara 05321234567");
        Assert.DoesNotContain("05321234567", cleaned);
        Assert.Contains("Ara", cleaned);
    }

    [Fact]
    public void Keeps_year_and_street_number()
    {
        const string input = "Sempozyum 2026, 708. Cadde No:14 bildirisi.";
        var cleaned = ContactCleaner.Clean(input);
        Assert.Equal(input, cleaned);
    }
}
