# WHAT — Markadan Nedir, Neden Böyle Tasarlandı

## Ne bu proje

Markadan, küçük işletme sahiplerine (esnaf/KOBİ) kendi e-ticaret mağazalarını kurma
imkânı veren white-label bir platformdur. Her müşteriye ayrı paketlenip verilir —
müşteri kendi alan adında, kendi markasıyla mağaza açar.

Pazaryerleri (Trendyol, Hepsiburada) %15-25 komisyon alıyor. Bu platform,
işletme sahiplerinin kendi müşterileriyle doğrudan ilişki kurmasını ve komisyon
ödemeden satış yapmasını sağlıyor. 2-3 müşteriyle başlayıp 50-100'e çıkması hedefleniyor.

## Mimari model

**Multi-instance, single-tenant:**
Her müşteri ayrı bir sunucuda (Docker container) + ayrı veritabanında çalışır.

**Neden bu model?**
Multi-tenant (tek DB, TenantId kolonu) daha az kaynak kullanır ama:
- Bir bug veya veri sızıntısı tüm müşterileri etkiler
- Müşteriler verilerinin başkasıyla paylaşıldığını kabul etmez
- Her müşteri için özelleştirme (tema, ayarlar, iş kuralları) zorlaşır

**Trade-off:** Her yeni müşteri için ayrı deploy gerekiyor.
50-100 müşteri bandında bu yönetilebilir — deploy playbook yazılacak.

## Teknik stack

| Katman | Teknoloji | Versiyon | Neden |
|--------|-----------|----------|-------|
| Backend | ASP.NET Core Web API | .NET 9 | Güçlü tip sistemi, onion mimari |
| Frontend | Next.js | 15 | SSR + BFF pattern, SEO |
| Veritabanı | SQL Server | 2022 | Docker ile izole, EF Core 9 |
| Auth | JWT + httpOnly cookie | — | Token browser JS'e ulaşmaz |
| Deploy | Docker Compose | — | Instance başına izolasyon |
| E-posta | MailKit | 4.12.0 | Terk edilen sepet, bildirimler |

## Kritik mimari kararlar

### httpOnly Cookie + BFF Pattern
**Ne:** Access token hiçbir zaman frontend JS'ine ulaşmıyor. Next.js BFF katmanı
cookie'yi yönetiyor, backend'e proxy'liyor.
**Neden:** XSS saldırısıyla token çalınamaz. localStorage güvensiz.
**Dikkat:** `mk_at` (access) ve `mk_rt` (refresh) cookie'leri — client kodu token görmez.

### Elle Yazılan Migration'lar
**Ne:** `dotnet ef migrations add` kullanılmıyor. Her migration dosyası elle yazılıyor.
**Neden:** `dotnet` CLI geliştirme ortamında (WSL) mevcut değil.
**Dikkat:** Elle yazılan dosyalara şu attribute'lar eklenmeli, yoksa EF Core görmez:
```csharp
[DbContext(typeof(MarkadanDbContext))]
[Migration("YYYYMMDDHHMMSS_MigrationAdi")]
```

### Single-Row Config (StoreSettings)
**Ne:** Mağaza ayarları tek satır DB tablosunda tutuluyor. Startup'ta yoksa otomatik oluşturuluyor.
**Neden:** Her deploy bir mağazaya ait — birden fazla satıra gerek yok.

### Fire-and-Forget Bildirimler
**Ne:** Düşük stok uyarısı, favori bildirimleri ana iş akışını bloklamıyor.
**Neden:** E-posta hatası sipariş akışını kesmemeli.

## Repolar

- Backend: `git@github.com:CAGANZ/MarkadanAPI.git` (branch: main)
- Frontend: `git@github.com:CAGANZ/MarkadanUI.git` (branch: Main)
- Şablonlar: `git@github.com:CAGANZ/claude-templates.git`

**Çalışan servisler:**
- API: `http://localhost:8080` + Swagger: `http://localhost:8080/swagger`
- DB: `localhost:1433` (Docker, SQL Server 2022)
- Admin hesabı: `admin@markadan.com` / şifre `.env`'de

## Dosya haritası

```
Markadan.API/Controllers/          → HTTP uç noktaları — iş mantığı yok
Markadan.Application/Abstractions/ → Interface'ler
Markadan.Application/DTOs/         → Request/Response modelleri
Markadan.Infrastructure/Services/  → Gerçek implementasyonlar
Markadan.Infrastructure/Migrations/→ Elle yazılan migration'lar
Markadan.Domain/Models/            → Entity'ler
docker-compose.yml                 → api (8080) + db (1433)
.env                               → Gerçek değerler (gitignore'da)
REFACTOR-PLAN.md                   → Tüm görev listesi ve yol haritası

docs/DURUM-RAPORU.md (MarkadanUI)  → Frontend ekibi handoff belgesi
src/lib/client/api.js  (MarkadanUI)→ Client API wrapper
src/components/ui/     (MarkadanUI)→ UI kit (Button, Modal, Input...)
```

## Bilinen sorunlar ve geçici çözümler

**409 sonrası sepet snapshot'ı tazelemiyor:**
Backend fiyat değişikliği sonrası `GET /me/cart` snapshot'ı güncellemiyor.
Geçici: frontend `acceptPriceChanges` fonksiyonu satırı silip yeniden ekliyor.
Kalıcı: `POST /me/cart/accept-prices` backend ucu yazılacak (F2).
