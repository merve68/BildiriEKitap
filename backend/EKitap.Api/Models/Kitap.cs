namespace EKitap.Api.Models;

public class Kitap
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public KitapDurumu Durum { get; set; } = KitapDurumu.Taslak;
    public string? PdfYolu { get; set; }
    public string? HataMesaji { get; set; }
    public string? Asama { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public DateTime GuncellemeTarihi { get; set; }

    public ICollection<Bildiri> Bildiriler { get; set; } = new List<Bildiri>();
}
