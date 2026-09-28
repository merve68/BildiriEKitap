using System.IO.Packaging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace EKitap.Api.Services;

public record WordParagraph(string Text, bool IsHeading);

public record WordDocumentContent(string Title, IReadOnlyList<WordParagraph> Paragraphs);

public interface IWordReader
{
    WordDocumentContent Read(string path, string fileName);
}

public class WordReader : IWordReader
{
    public WordDocumentContent Read(string path, string fileName)
    {
        WordprocessingDocument document;
        try
        {
            document = WordprocessingDocument.Open(path, false);
        }
        catch (Exception ex) when (ex is FileFormatException or OpenXmlPackageException or InvalidDataException)
        {
            throw new InvalidOperationException($"{fileName} açılamadı. Dosya bozuk veya geçerli bir Word belgesi değil.");
        }

        using (document)
        {
        var body = document.MainDocumentPart?.Document?.Body
            ?? throw new InvalidOperationException($"{fileName} okunamadı. Belge gövdesi boş.");

        var paragraphs = new List<WordParagraph>();
        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph paragraph:
                    AppendParagraph(paragraphs, paragraph);
                    break;
                case Table table:
                    AppendTable(paragraphs, table);
                    break;
            }
        }

        var cleaned = paragraphs
            .Select(p => new WordParagraph(ContactCleaner.Clean(p.Text), p.IsHeading))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        if (cleaned.Count == 0)
            throw new InvalidOperationException($"{fileName} içinde kullanılabilir metin bulunamadı.");

        var title = ResolveTitle(cleaned, fileName);
        return new WordDocumentContent(title, cleaned);
        }
    }

    private static void AppendParagraph(List<WordParagraph> target, Paragraph paragraph)
    {
        var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text)).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
        var isHeading = style.Contains("Heading", StringComparison.OrdinalIgnoreCase)
            || style.Contains("Başlık", StringComparison.OrdinalIgnoreCase)
            || style.Contains("Baslik", StringComparison.OrdinalIgnoreCase)
            || style.Equals("Title", StringComparison.OrdinalIgnoreCase);

        target.Add(new WordParagraph(text, isHeading));
    }

    private static void AppendTable(List<WordParagraph> target, Table table)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>()
                .Select(cell => string.Concat(cell.Descendants<Text>().Select(t => t.Text)).Trim())
                .Where(cell => !string.IsNullOrWhiteSpace(cell));
            var line = string.Join("  |  ", cells);
            if (!string.IsNullOrWhiteSpace(line))
                target.Add(new WordParagraph(line, false));
        }
    }

    private static string ResolveTitle(IReadOnlyList<WordParagraph> paragraphs, string fileName)
    {
        var heading = paragraphs.FirstOrDefault(p => p.IsHeading && p.Text.Length <= 180);
        if (heading is not null)
            return heading.Text;

        var first = paragraphs.FirstOrDefault(p => p.Text.Length is >= 8 and <= 180);
        if (first is not null)
            return first.Text;

        var name = Path.GetFileNameWithoutExtension(fileName).Trim();
        return string.IsNullOrWhiteSpace(name) ? "Bildiri" : name;
    }
}
