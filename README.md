# Bildiri E-Kitap

On Word bildirisini yükleyip tek bir PDF e-kitap üreten uygulama. React arayüz, ASP.NET Core Web API ve MSSQL (Entity Framework Core) kullanır.

## Kurulum

Gereksinimler: .NET 10 SDK, Node.js 18+, Docker.

```bash
docker compose up -d
cd backend/EKitap.Api
dotnet run
```

API `http://localhost:5141` adresinde açılır. İlk çalıştırmada `BildiriEKitap` veritabanı migration ile oluşturulur.

Bağlantı dizesi `backend/EKitap.Api/appsettings.json` içindedir. Varsayılan, `docker-compose.yml` ile ayağa kalkan SQL Server 2022 içindir (`sa` / `EKitap_Dev1!`). Kendi sunucunuzu kullanacaksanız bu dizeyi değiştirin. Şema migration ile kurulur:

```bash
dotnet ef database update --project backend/EKitap.Api
```

Arayüz:

```bash
cd frontend
npm install
npm run dev
```

Arayüz `http://localhost:5173` adresindedir ve `/api` isteklerini API'ye iletir.

## Dosya saklama

Orijinal `.docx` dosyaları değiştirilmez. `wwwroot/uploads/{kitapId}/` altına sıra numarasıyla kaydedilir. Üretilen PDF `wwwroot/ebooks/{kitapId}.pdf` yoluna yazılır. Veritabanında yalnızca yollar tutulur.

## Tablolar

- `Kitaplar`: ad, durum (`Taslak`, `Isleniyor`, `Tamamlandi`, `Failed`), PDF yolu, hata, aşama.
- `Bildiriler`: kitapla bire-çok ilişki, sıra, dosya adı, başlık, orijinal dosya yolu, başlangıç sayfası.

## İşleme akışı

1. Kullanıcı kitap adı ve tam 10 `.docx` yükler. Sıra arayüzde değiştirilebilir.
2. Kayıt `Taslak` olarak yazılır.
3. "Kitabı Oluştur" üretimi kuyruğa alır. Arayüz durumu yaklaşık 1 saniyede bir sorar.
4. Open XML SDK paragrafları ve tablo satırlarını okur. Başlık, Heading stilinden veya ilk uygun paragraftan alınır.
5. E-posta ve telefonlar sunucuda, PDF metninden temizlenir. Word dosyası aynı kalır.
6. QuestPDF kapak, içindekiler ve bildirileri aynı sayfa düzeniyle ölçer. Kapak veya içindekiler birden fazla sayfa sürerse başlangıç numaraları buna göre kayar. Altbilgide kitap sayfa numarası vardır.
7. Hata olursa kitabın durumu `Failed` olur ve mesaj arayüzde gösterilir.

## İletişim temizliği

`ContactCleaner` e-posta adreslerini ve Türkiye telefon biçimlerini (boşluklu hat, `+90`, bitişik cep) siler. Yıl, cadde ve kapı numarası korunur.

Örnekler:

- `İnfo@akap.tr - Yıldızevler Mah. 708. Cad. No:14/3` → adres kalır, e-posta gider.
- `0312 343 10 33` gider.
- `+90 532 111 22 33` ve `05321234567` gider.
- `Sempozyum 2026, 708. Cadde No:14` aynı kalır.

Testler: `dotnet test`.

## Kütüphaneler

- Backend: ASP.NET Core, EF Core SQL Server, DocumentFormat.OpenXml, QuestPDF (Community lisansı).
- Frontend: React, TypeScript, Vite, react-pdf / PDF.js.

## Tasarım

Masaüstünde solda ad, dosya bırakma alanı, ilerleme çubuğu ve sıralanabilir bildiri listesi vardır; sağda kitap açılır. Üstte üç adım (ad, 10 bildiri, PDF) tamamlanan işi gösterir. 800 pikselin altında paneller alt alta geçer ve oluşturma düğmesi ekranın altında sabit kalır. PDF sayfa içinde görüntülenir ve indirilebilir.



