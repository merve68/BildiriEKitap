using System.Text;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EKitap.Api.Services;

public record EbookPaper(string Title, IReadOnlyList<string> Paragraphs);

public record EbookResult(byte[] Pdf, IReadOnlyList<int> StartPages);

public interface IEbookPdfService
{
    EbookResult Create(string bookTitle, IReadOnlyList<EbookPaper> papers);
}

public class EbookPdfService : IEbookPdfService
{
    public EbookResult Create(string bookTitle, IReadOnlyList<EbookPaper> papers)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var paperPageCounts = papers.Select(CountPaperPages).ToList();
        var coverPages = Measure(column => ComposeCover(column, bookTitle, papers.Count));

        var tocPages = 1;
        var startPages = ComputeStartPages(coverPages, tocPages, paperPageCounts);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var measuredTocPages = Measure(column => ComposeToc(column, papers, startPages));
            if (measuredTocPages == tocPages)
                break;

            tocPages = measuredTocPages;
            startPages = ComputeStartPages(coverPages, tocPages, paperPageCounts);
        }

        var pdf = BuildBook(bookTitle, papers, startPages);
        var expectedPages = coverPages + tocPages + paperPageCounts.Sum();
        var actualPages = CountPages(pdf);
        if (actualPages != expectedPages)
            throw new InvalidOperationException(
                $"Sayfa hesabı tutmadı. Beklenen {expectedPages}, oluşan {actualPages}.");

        return new EbookResult(pdf, startPages);
    }

    private static IReadOnlyList<int> ComputeStartPages(int coverPages, int tocPages, IReadOnlyList<int> paperPageCounts)
    {
        var startPages = new List<int>(paperPageCounts.Count);
        var cursor = coverPages + tocPages + 1;
        foreach (var count in paperPageCounts)
        {
            startPages.Add(cursor);
            cursor += Math.Max(count, 1);
        }

        return startPages;
    }

    private static int Measure(Action<ColumnDescriptor> compose)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPage(page);
                page.Content().Column(compose);
            });
        }).GeneratePdf();

        return Math.Max(1, CountPages(pdf));
    }

    private static int CountPaperPages(EbookPaper paper)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPage(page);
                page.Content().Column(column => ComposePaper(column, paper));
            });
        }).GeneratePdf();

        var count = CountPages(pdf);
        return count < 1 ? 1 : count;
    }

    private static byte[] BuildBook(string bookTitle, IReadOnlyList<EbookPaper> papers, IReadOnlyList<int> startPages)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPage(page);
                page.Content().Column(column =>
                {
                    column.Spacing(0);
                    ComposeCover(column, bookTitle, papers.Count);
                    column.Item().PageBreak();
                    ComposeToc(column, papers, startPages);

                    for (var i = 0; i < papers.Count; i++)
                    {
                        column.Item().PageBreak();
                        ComposePaper(column, papers[i]);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void ApplyPage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(2, Unit.Centimetre);
        page.MarginTop(1.8f, Unit.Centimetre);
        page.MarginBottom(1.6f, Unit.Centimetre);
        page.DefaultTextStyle(style => style.FontSize(11).FontFamily("Lato").LineHeight(1.35f));
        page.Footer().AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken2));
            text.Span("— ");
            text.CurrentPageNumber();
            text.Span(" —");
        });
    }

    private static void ComposeCover(ColumnDescriptor column, string bookTitle, int paperCount)
    {
        column.Item().PaddingTop(90).AlignCenter().Text("E-KİTAP")
            .FontSize(14).FontColor(Colors.Brown.Darken2).LetterSpacing(0.18f);
        column.Item().PaddingTop(18).AlignCenter().Text(bookTitle)
            .FontSize(28).Bold().FontColor(Colors.Grey.Darken4);
        column.Item().PaddingTop(12).AlignCenter().Text($"{paperCount} bildiri")
            .FontSize(12).FontColor(Colors.Grey.Darken1);
        column.Item().PaddingTop(28).AlignCenter().Text(DateTime.Now.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR")))
            .FontSize(11).FontColor(Colors.Grey.Darken1);
    }

    private static void ComposeToc(ColumnDescriptor column, IReadOnlyList<EbookPaper> papers, IReadOnlyList<int> startPages)
    {
        column.Item().Text("İçindekiler").FontSize(22).Bold().FontColor(Colors.Brown.Darken3);
        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Brown.Lighten2);

        for (var i = 0; i < papers.Count; i++)
        {
            var title = papers[i].Title;
            var pageNumber = startPages[i].ToString();
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text(title).FontSize(12);
                row.ConstantItem(40).AlignRight().Text(pageNumber).FontSize(12).FontColor(Colors.Brown.Darken2);
            });
        }
    }

    private static void ComposePaper(ColumnDescriptor column, EbookPaper paper)
    {
        column.Item().Text(paper.Title).FontSize(18).Bold().FontColor(Colors.Grey.Darken4);
        column.Item().PaddingTop(4).PaddingBottom(10).LineHorizontal(0.6f).LineColor(Colors.Brown.Lighten2);

        foreach (var paragraph in paper.Paragraphs)
            column.Item().PaddingBottom(7).Text(paragraph).FontSize(11);
    }

    private static int CountPages(byte[] pdf)
    {
        var content = Encoding.Latin1.GetString(pdf);
        return Regex.Matches(content, @"/Type\s*/Page(?!s)").Count;
    }
}
