namespace EKitap.Api.Models;

public class Bildiri
{
    public Guid Id { get; set; }
    public Guid KitapId { get; set; }
    public Kitap Kitap { get; set; } = null!;
    public int Sira { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string? Baslik { get; set; }
    public string OrijinalDosyaYolu { get; set; } = string.Empty;
    public int? SayfaBaslangic { get; set; }
}
