using EKitap.Api.Services;

namespace EKitap.Tests;

public class EbookPdfTests
{
    [Fact]
    public void Table_of_contents_uses_real_start_pages()
    {
        var papers = Enumerable.Range(1, 10)
            .Select(i => new EbookPaper($"Bildiri {i} başlığı", ["Kısa gövde metni."]))
            .ToList();

        var result = new EbookPdfService().Create("Sempozyum", papers);

        Assert.Equal(10, result.StartPages.Count);
        Assert.Equal(3, result.StartPages[0]);
        for (var i = 1; i < result.StartPages.Count; i++)
            Assert.True(result.StartPages[i] > result.StartPages[i - 1]);
        Assert.True(result.Pdf.Length > 1000);
    }

    [Fact]
    public void Long_titles_shift_start_pages_when_toc_overflows()
    {
        var longTitle = string.Join(" ", Enumerable.Repeat("Uzun bildiri başlığı satırı", 40));
        var papers = Enumerable.Range(1, 10)
            .Select(i => new EbookPaper($"{i}. {longTitle}", ["Kısa gövde."]))
            .ToList();

        var result = new EbookPdfService().Create("Sempozyum", papers);

        Assert.True(result.StartPages[0] > 3);
        for (var i = 1; i < result.StartPages.Count; i++)
            Assert.Equal(result.StartPages[i - 1] + 1, result.StartPages[i]);
    }
}
