using EKitap.Api.Data;
using EKitap.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EKitap.Api.Services;

public record BildiriDto(Guid Id, int Sira, string DosyaAdi, string? Baslik, int? SayfaBaslangic);

public record KitapDto(
    Guid Id,
    string Ad,
    string Durum,
    string? HataMesaji,
    string? Asama,
    DateTime OlusturmaTarihi,
    IReadOnlyList<BildiriDto> Bildiriler);

public interface IKitapService
{
    Task<KitapDto> CreateAsync(string ad, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken);
    Task<KitapDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<KitapDto> StartGenerationAsync(Guid id, CancellationToken cancellationToken);
    Task<(byte[] Content, string FileName)?> GetPdfAsync(Guid id, CancellationToken cancellationToken);
    Task GenerateAsync(Guid id, CancellationToken cancellationToken);
}

public class KitapService(
    AppDbContext db,
    IWordReader wordReader,
    IEbookPdfService pdfService,
    IWebHostEnvironment environment,
    IKitapGenerationQueue queue) : IKitapService
{
    public const int RequiredFileCount = 10;

    public async Task<KitapDto> CreateAsync(string ad, IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ad) || ad.Trim().Length > 200)
            throw new InvalidOperationException("Kitap adı 1 ile 200 karakter arasında olmalıdır.");

        if (files.Count != RequiredFileCount)
            throw new InvalidOperationException($"Tam olarak {RequiredFileCount} adet .docx dosyası yüklenmelidir.");

        if (files.Any(file => !file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Yalnızca .docx dosyaları kabul edilir.");

        if (files.Any(file => file.Length == 0 || file.Length > 20 * 1024 * 1024))
            throw new InvalidOperationException("Her dosya boş olmamalı ve 20 MB sınırını aşmamalıdır.");

        var kitap = new Kitap
        {
            Id = Guid.NewGuid(),
            Ad = ad.Trim(),
            Durum = KitapDurumu.Taslak,
            Asama = "Dosyalar kaydedildi",
            OlusturmaTarihi = DateTime.UtcNow,
            GuncellemeTarihi = DateTime.UtcNow
        };

        var folder = Path.Combine(environment.WebRootPath, "uploads", kitap.Id.ToString());
        Directory.CreateDirectory(folder);

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var safeName = Path.GetFileName(file.FileName);
            var storedName = $"{index + 1:00}_{safeName}";
            var fullPath = Path.Combine(folder, storedName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream, cancellationToken);

            kitap.Bildiriler.Add(new Bildiri
            {
                Id = Guid.NewGuid(),
                KitapId = kitap.Id,
                Sira = index + 1,
                DosyaAdi = safeName,
                OrijinalDosyaYolu = fullPath
            });
        }

        db.Kitaplar.Add(kitap);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(kitap);
    }

    public async Task<KitapDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var kitap = await db.Kitaplar
            .Include(x => x.Bildiriler)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return kitap is null ? null : ToDto(kitap);
    }

    public async Task<KitapDto> StartGenerationAsync(Guid id, CancellationToken cancellationToken)
    {
        var kitap = await db.Kitaplar.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Kitap bulunamadı.");

        if (kitap.Durum == KitapDurumu.Isleniyor)
            return (await GetAsync(id, cancellationToken))!;

        kitap.Durum = KitapDurumu.Isleniyor;
        kitap.HataMesaji = null;
        kitap.Asama = "Sıraya alındı";
        kitap.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        queue.Enqueue(id);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task<(byte[] Content, string FileName)?> GetPdfAsync(Guid id, CancellationToken cancellationToken)
    {
        var kitap = await db.Kitaplar.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (kitap?.PdfYolu is null || !File.Exists(kitap.PdfYolu))
            return null;

        var bytes = await File.ReadAllBytesAsync(kitap.PdfYolu, cancellationToken);
        var fileName = $"{SanitizeFileName(kitap.Ad)}.pdf";
        return (bytes, fileName);
    }

    public async Task GenerateAsync(Guid id, CancellationToken cancellationToken)
    {
        var kitap = await db.Kitaplar
            .Include(x => x.Bildiriler)
            .FirstAsync(x => x.Id == id, cancellationToken);

        try
        {
            kitap.Durum = KitapDurumu.Isleniyor;
            kitap.Asama = "Word belgeleri okunuyor";
            kitap.GuncellemeTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var papers = new List<EbookPaper>();
            foreach (var bildiri in kitap.Bildiriler.OrderBy(x => x.Sira))
            {
                kitap.Asama = $"Bildiri {bildiri.Sira}/10 okunuyor";
                kitap.GuncellemeTarihi = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                var content = wordReader.Read(bildiri.OrijinalDosyaYolu, bildiri.DosyaAdi);
                bildiri.Baslik = content.Title;
                papers.Add(new EbookPaper(
                    content.Title,
                    content.Paragraphs.Select(p => p.Text).ToList()));
            }

            kitap.Asama = "PDF oluşturuluyor";
            kitap.GuncellemeTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            var ebook = pdfService.Create(kitap.Ad, papers);
            var ebookDir = Path.Combine(environment.WebRootPath, "ebooks");
            Directory.CreateDirectory(ebookDir);
            var pdfPath = Path.Combine(ebookDir, $"{kitap.Id}.pdf");
            await File.WriteAllBytesAsync(pdfPath, ebook.Pdf, cancellationToken);

            var ordered = kitap.Bildiriler.OrderBy(x => x.Sira).ToList();
            for (var i = 0; i < ordered.Count && i < ebook.StartPages.Count; i++)
                ordered[i].SayfaBaslangic = ebook.StartPages[i];

            kitap.PdfYolu = pdfPath;
            kitap.Durum = KitapDurumu.Tamamlandi;
            kitap.Asama = "Tamamlandı";
            kitap.HataMesaji = null;
            kitap.GuncellemeTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            kitap.Durum = KitapDurumu.Failed;
            kitap.Asama = "Hata";
            kitap.HataMesaji = ex.Message;
            kitap.GuncellemeTarihi = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static KitapDto ToDto(Kitap kitap) => new(
        kitap.Id,
        kitap.Ad,
        kitap.Durum.ToString(),
        kitap.HataMesaji,
        kitap.Asama,
        kitap.OlusturmaTarihi,
        kitap.Bildiriler
            .OrderBy(x => x.Sira)
            .Select(x => new BildiriDto(x.Id, x.Sira, x.DosyaAdi, x.Baslik, x.SayfaBaslangic))
            .ToList());

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "ekitap" : cleaned;
    }
}
