using EKitap.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EKitap.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Kitap> Kitaplar => Set<Kitap>();
    public DbSet<Bildiri> Bildiriler => Set<Bildiri>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Kitap>(entity =>
        {
            entity.ToTable("Kitaplar");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ad).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Durum).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.PdfYolu).HasMaxLength(500);
            entity.Property(x => x.HataMesaji).HasMaxLength(2000);
            entity.Property(x => x.Asama).HasMaxLength(200);
            entity.HasMany(x => x.Bildiriler)
                .WithOne(x => x.Kitap)
                .HasForeignKey(x => x.KitapId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bildiri>(entity =>
        {
            entity.ToTable("Bildiriler");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DosyaAdi).HasMaxLength(260).IsRequired();
            entity.Property(x => x.Baslik).HasMaxLength(300);
            entity.Property(x => x.OrijinalDosyaYolu).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.KitapId, x.Sira }).IsUnique();
        });
    }
}
