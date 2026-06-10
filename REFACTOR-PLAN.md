# Markadan — Mimari Analiz ve Refaktör Planı

> Bu doküman bir "yönetici brief"idir. Görevleri uygulayacak model/geliştirici bu dosyayı tek başına
> okuyup işe başlayabilmelidir. Görevler fazlara ayrılmıştır; her görevde **Ne / Neden / Dosyalar /
> Kabul Kriteri** bulunur. Görevler kod içermez; uygulayıcı kodu kendisi yazar.

---

## 0. Proje Bağlamı

**Markadan**, butiği olan firmalara hazır mağaza olarak satılacak bir **white-label e-ticaret
backend**'idir. Dağıtım modeli **multi-instance**'tır: her firmaya **ayrı deploy, ayrı veritabanı**
verilir. Multi-tenancy (paylaşımlı DB + TenantId) **hedef DEĞİLDİR** — kimse TenantId eklemesin.

Bu modelin mimari sonucu: izolasyon fizikseldir, asıl hedef **"yeni firma kurulumu = config doldur +
tek komut"** seviyesinde paketlenebilirliktir. Instance'a özgü hiçbir değer (DB bağlantısı, JWT
anahtarı, CORS origin, marka bilgisi) koda gömülü olamaz.

### Stack
- .NET 9, ASP.NET Core Web API, EF Core 9 + SQL Server
- ASP.NET Identity (int PK) + JWT access token + refresh token rotasyonu
- AutoMapper 15 (kayıtlı, az kullanılıyor), Swagger

### Katmanlar
```
Markadan.Domain          → Entity'ler (Product, Brand, Category, Cart, CartItem, Address, AppUser, AppRole, RefreshToken)
Markadan.Application     → Arayüzler (I*ReadService / I*CommandService, IAuthService), DTO'lar, JwtOptions, BusinessRuleException
Markadan.Infrastructure  → MarkadanDbContext, EF konfigürasyonları (Configurations/*_Cfg.cs), servis implementasyonları, Migrations
Markadan.API             → Controller'lar, ApiExceptionFilter, Program.cs
```
Bağımlılık yönü: `API → Infrastructure → Application → Domain`.

### Korunacak güçlü yönler (DOKUNMA)
- Read/Command servis ayrımı (CQRS-lite) ve `AsNoTracking` + DTO'ya `Select` projeksiyonu deseni
- Refresh token rotasyonu mekanizması (`AuthService.RefreshAsync` akışı)
- Entity konfigürasyonlarının ayrı dosyalarda olması + `ApplyConfigurationsFromAssembly`
- `ApiExceptionFilter` ile ProblemDetails hata sözleşmesi
- `Program.cs`'teki açılışta otomatik `db.Database.Migrate()` — multi-instance modelde her instance
  kendi DB'sini günceller; bu bilinçli bir tercihtir, kaldırılmayacak
- Türkçe collation (`Turkish_100_CI_AI`) kullanımı

---

## Uygulayıcı için genel kurallar

1. **Görev kapsamı dışına çıkma.** Bir görevi yaparken gördüğün başka sorunu düzeltme; bu dosyanın
   sonundaki "Notlar" bölümüne madde olarak ekle.
2. Her görev **ayrı commit** olmalı; commit mesajı Türkçe, mevcut commit üslubuna uygun.
3. Her görevden sonra `dotnet build Markadan.sln` temiz geçmeli. Migration gerektiren görevlerde
   `dotnet ef migrations add <Ad> --project Markadan.Infrastructure --startup-project Markadan.API`
   ile migration üret ve repo'ya dahil et.
4. Projede test altyapısı yok (bkz. Görev D1); o gelene kadar doğrulama derleme + Swagger üzerinden
   manuel kontroldür.
5. Mevcut kod stilini koru (dosya düzeni, isimlendirme); toplu reformat yapma.

---

## FAZ A — Güvenlik (acil, önce bu)

### A1. Admin endpoint'lerini yetkilendir
- **Ne:** `AdminProductsController`, `AdminBrandsController`, `AdminCategoriesController`
  sınıflarına `AdminOnly` policy'siyle yetkilendirme ekle. Policy `Program.cs:71`'de zaten tanımlı
  (`options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"))`) ama hiçbir yerde kullanılmıyor.
- **Neden:** Şu an projedeki tek `[Authorize]` `auth/me`'de. Ürün ekleme/silme/güncelleme dahil tüm
  admin uçları **anonim erişime açık**. Policy tanımlamak korumayı başlatmaz; controller'a
  uygulanması gerekir.
- **Dosyalar:** `Markadan.API/Controllers/AdminProductsController.cs`, `AdminBrandsController.cs`,
  `AdminCategoryController.cs`
- **Kabul kriteri:** Token'sız istekte admin uçları 401, "User" rollü token'la 403, "Admin" rollü
  token'la 200 dönmeli. Public uçlar (`/products`, `/brands`, `/categories` GET'leri) anonim kalmalı.

### A2. DebugController'ı kaldır veya kilitle
- **Ne:** `Markadan.API/Controllers/DebugController.cs` ya tamamen silinmeli ya da yalnızca
  Development ortamında ve `AdminOnly` ile erişilebilir olmalı. Tercih: **sil** (DB sayıları zaten
  Swagger/admin uçlarından görülebilir).
- **Neden:** `/debug/db` anonim olarak DB sunucu adını (`DataSource`) ve veritabanı adını sızdırıyor.
  Müşteriye paketlenen her kopyada bu bilgi ifşası tekrarlanır.
- **Kabul kriteri:** `/debug/db` 404 dönmeli (veya korunmuş olmalı); build temiz.

### A3. Sırları konfigürasyondan/repodan çıkar
- **Ne:**
  - `appsettings.json`'daki `Jwt:Key` ve `ConnectionStrings:DefaultConnection` değerlerini kaldır;
    bunlar environment variable / user-secrets'tan gelsin. `appsettings.json`'da anahtarlar boş veya
    placeholder kalabilir, `appsettings.Development.json` da kontrol edilmeli.
  - `Program.cs`'te uygulama açılışında `Jwt:Key` yoksa/32 byte'tan kısaysa anlaşılır bir hatayla
    **fail-fast** yapılmalı.
  - `MarkadanDbContext.OnConfiguring` (satır 21–33) içindeki `CAGANZ` makine adlı hardcoded
    connection string fallback'i tamamen kaldırılmalı. Design-time ihtiyaç varsa
    `IDesignTimeDbContextFactory<MarkadanDbContext>` ile çözülmeli (env'den okur), entity sınıfının
    içinde sabit bağlantı asla durmamalı.
- **Neden:** Multi-instance modelde her firmanın DB'si ve JWT anahtarı farklı olacak. Koda gömülü
  fallback, bir instance'ın yanlış DB'ye bağlanmasına yol açabilir; repodaki gerçek anahtar git
  geçmişinde sonsuza dek kalır ve tüm kopyalarda aynı anahtar riskini doğurur.
- **Dosyalar:** `Markadan.API/appsettings.json`, `appsettings.Development.json`,
  `Markadan.API/Program.cs`, `Markadan.Infrastructure/Data/MarkadanDbContext.cs`
- **Kabul kriteri:** Repo'da gerçek sır kalmamalı; env değişkenleri verilmeden uygulama açıklayıcı
  hata ile durmalı; env verilince çalışmalı; `dotnet ef migrations list` hâlâ çalışabilmeli.

### A4. CORS origin'lerini config'e taşı
- **Ne:** `Program.cs:45-47`'deki sabit `localhost:3000` origin listesi `appsettings` →
  `Cors:AllowedOrigins` (string dizisi) üzerinden okunmalı.
- **Neden:** Her firmanın frontend'i kendi domain'inde çalışacak; origin deploy başına değişen bir
  değerdir, kod değişikliği gerektirmemeli.
- **Kabul kriteri:** Origin listesi config'ten geliyor; config boşsa anlamlı varsayılan/uyarı var.

---

## FAZ B — Bug ve veri bütünlüğü

### B1. Brand isim benzersizliği bug'ı
- **Ne:** `Markadan.Infrastructure/Configurations/Brand_Cfg.cs` içinde `Name` için önce
  `HasIndex(...).IsUnique()` sonra **aynı property'ye** `HasIndex(...).IsUnique(false)` çağrılıyor;
  ikincisi birinciyi ezer. İkinci `HasIndex` satırını sil, unique index'i koru. Migration üret.
  `Category_Cfg.cs` ve diğer `*_Cfg.cs` dosyalarında aynı hatanın olup olmadığını kontrol et.
- **Neden:** Son commit'in amacı (be0e7bf: Türkçe harf farkından kaynaklı mükerrer isim engeli)
  Brand tablosunda fiilen devre dışı — DB'de unique constraint yok, korumayı yalnızca servis
  içindeki `AnyAsync` kontrolü sağlıyor; bu da eşzamanlı isteklerde (race condition) yarılabilir.
- **Kabul kriteri:** Migration'da Brands.Name üzerinde unique index oluşmalı; aynı isimle (büyük/
  küçük harf veya Türkçe karakter varyantıyla) ikinci brand eklenememeli.

### B2. GovId ve refresh token saklama güvenliği
- **Ne:** İki ayrı iş: (a) `RefreshToken.Token` DB'ye **hash'lenerek** (SHA-256 yeterli) yazılmalı;
  doğrulamada gelen token hash'lenip aranmalı. (b) `AppUser.GovId` (TC kimlik no) için en azından
  "neden saklanıyor?" kararı verilmeli — gerekliyse şifrelenerek (ASP.NET Data Protection)
  saklanmalı, gereksizse alan kaldırılmalı. **(b) için önce ürün kararı gerekir; uygulayıcı bu
  görevde yalnızca (a)'yı yapsın, (b)'yi Notlar'a yazsın.**
- **Neden:** DB sızıntısında düz metin refresh token'lar oturum çalmaya, düz metin TC kimlik
  numaraları KVKK ihlaline doğrudan dönüşür. Refresh token, parola ile aynı muameleyi hak eder.
- **Dosyalar:** `Markadan.Infrastructure/Services/AuthService.cs`, `Markadan.Domain/Models/RefreshToken.cs`
- **Kabul kriteri:** DB'de ham token bulunmamalı; login → refresh → refresh akışı çalışmaya devam
  etmeli; eski düz metin token'lar için migration stratejisi (tabloyu boşaltmak kabul — kullanıcılar
  re-login olur) belirtilmeli.

### B3. Refresh token reuse tespiti
- **Ne:** `AuthService.RefreshAsync`'te revoke edilmiş bir token'la gelinirse şu an sadece 401
  dönüyor. Revoke edilmiş token kullanımı **çalıntı şüphesidir**: bu durumda o kullanıcının aktif
  tüm refresh token'ları revoke edilmeli (zincir invalidasyonu).
- **Neden:** Rotasyonun amacı tam da budur — eski token'ın tekrar kullanılması, token'ın iki kişide
  olduğunu kanıtlar; tek 401 saldırganın elindeki güncel zinciri yaşatır.
- **Kabul kriteri:** Revoke'lu token ile refresh denemesi sonrası, aynı kullanıcının elindeki güncel
  refresh token da geçersiz olmalı.

---

## FAZ C — Katman temizliği ve kod kalitesi

### C1. API → Infrastructure sızıntılarını kapat
- **Ne:**
  - `AdminProductsController` constructor'ından `MarkadanDbContext` bağımlılığını kaldır (inject
    ediliyor ama hiçbir action kullanmıyor).
  - `AuthController`'daki `using Markadan.Infrastructure.Services;` satırını kaldır (gereksiz —
    controller zaten `IAuthService` kullanıyor).
  - Hedef: API projesindeki hiçbir controller `Markadan.Infrastructure.*` namespace'ine doğrudan
    referans vermesin (Program.cs/DI kayıtları hariç).
- **Neden:** Katmanlı mimarinin değeri üst katmanın alt detayı bilmemesidir; DbContext'in
  controller'a sızması test edilebilirliği bozar ve "servis katmanını atla" kısayolunu davet eder.
- **Kabul kriteri:** `grep -rn "Infrastructure" Markadan.API/Controllers/` yalnızca boş dönmeli;
  build temiz.

### C2. Validation'ı exception'dan ayır
- **Ne:** Servislerdeki (`AuthService.RegisterAsync`, `*CommandService.CreateAsync/UpdateAsync`)
  girdi doğrulamaları `InvalidOperationException` fırlatıyor. DTO'lara DataAnnotations
  (`[Required]`, `[MinLength]`, `[Range]` vb.) eklenerek format/zorunluluk kontrolleri model
  binding'e taşınmalı (ApiController zaten otomatik 400 üretir); servislerde yalnızca **iş kuralları**
  kalmalı ve bunlar `BusinessRuleException` (veya yeni bir `ValidationException`) kullanmalı.
- **Neden:** `InvalidOperationException` programlama hatalarının da tipidir; iş kuralıyla aynı tipi
  paylaşması `ApiExceptionFilter`'da gerçek bug'ların 400 olarak maskelenmesine yol açar.
- **Dosyalar:** `Markadan.Application/DTOs/**`, `Markadan.Infrastructure/Services/*CommandService.cs`,
  `AuthService.cs`, gerekirse `ApiExceptionFilter.cs`
- **Kabul kriteri:** Boş/eksik alanlı isteklere ProblemDetails formatında 400; iş kuralı ihlaline
  409; servislerde format kontrolü kalmamalı.

### C3. Çöp ve isimlendirme temizliği
- **Ne:**
  - `Markadan.API/WeatherForecast.cs` sil (şablon artığı).
  - `Markadan.API/Controllers/BrandsController .cs` → dosya adındaki boşluğu kaldır
    (`git mv` ile, geçmiş korunarak).
  - `AdminCategoryController.cs` dosyasını içindeki sınıf adıyla eşleştir
    (`AdminCategoriesController.cs` olarak yeniden adlandır).
  - `Markadan.Domain/Markadan.Domain.csproj` içindeki mükerrer `<TargetFramework>` satırını sil.
  - `Markadan.API.csproj.user` dosyasını repo'dan çıkar ve `.gitignore`'a `*.csproj.user` ekle.
- **Neden:** Müşteriye paketlenecek bir üründe şablon artığı ve bozuk dosya adları profesyonellik
  sorunudur; `*.user` dosyaları kişisel IDE ayarıdır, repo'ya ait değildir.
- **Kabul kriteri:** Build temiz; `git status` temiz; route'lar değişmemiş olmalı.

### C4. AutoMapper kararı: ya kullan ya kaldır
- **Ne:** AutoMapper üç projeye paket olarak ekli ve `CatalogProfile` kayıtlı, ancak servisler DTO
  projeksiyonunu elle `Select` ile yapıyor. Karar: **elle projeksiyon korunacak** (bkz. korunacaklar
  listesi), dolayısıyla AutoMapper'ın gerçekten kullanıldığı yer var mı tara; kullanılmıyorsa paketi
  ve profili kaldır. Bir-iki yerde kullanılıyorsa o kullanım da `Select` projeksiyonuna çevrilip
  paket yine kaldırılmalı.
- **Neden:** Yarı kullanılan mapping kütüphanesi iki zihinsel model yaratır; ayrıca AutoMapper 15
  lisans/sürüm maliyeti tartışmalıdır. Elle `Select` projeksiyonu EF'in SQL'e çevirebildiği en
  verimli yoldur.
- **Kabul kriteri:** Çözümde AutoMapper referansı kalmamalı (ya da bilinçli olarak tek desen olarak
  her yerde kullanılmalı — varsayılan: kaldır); tüm uçlar aynı yanıtları dönmeli.

### C5. ID enumeration'a karşı public uçlarda karar
- **Ne:** Ürün/brand/category ID'leri sıralı int ve URL'de açık. Bu görev **analiz + öneri**
  görevidir: public mağaza uçları için slug veya opak ID gerekip gerekmediğini, frontend
  beklentisiyle birlikte değerlendirip Notlar'a yaz. Kod değişikliği yapma.
- **Neden:** Tek satıcılı mağazada bu düşük risk; ama rakip, `/products/1..N` tarayarak tüm katalog
  ve stok bilgisini çekebilir. Karar ürün sahibine ait.

---

## FAZ D — Paketleme ve sürdürülebilirlik (ürünleşme)

### D1. Test projesi iskeleti
- **Ne:** `Markadan.Tests` (xUnit) projesi ekle. İlk hedefler: `AuthService` refresh rotasyonu/reuse
  senaryoları (EF InMemory veya SQLite in-memory ile), `BrandCommandService` mükerrer isim kuralı,
  `ApiExceptionFilter` durum kodu eşlemesi. Kapsama hedefi değil, **güvenlik-kritik akışların**
  kilitlenmesi hedeftir.
- **Neden:** Multi-instance modelde her hata N müşteriye dağıtılır; regresyonu yakalayacak tek
  otomatik mekanizma testtir. Faz A–C değişikliklerinin de doğrulama aracı olur.
- **Kabul kriteri:** `dotnet test` çözüm kökünde çalışmalı ve yeşil olmalı.

### D2. Docker paketleme
- **Ne:** `Markadan.API` için çok aşamalı (multi-stage) Dockerfile + örnek `docker-compose.yml`
  (API + SQL Server) + `.env.example` (tüm instance-bazlı değişkenlerin listesi: connection string,
  JWT ayarları, CORS origin'leri). README'ye "yeni firma kurulumu" bölümü eklenecek.
- **Neden:** "Her firmaya ayrı paket" hedefinin somut hâli budur: yeni müşteri = `.env` doldur +
  `docker compose up`. A3/A4 tamamlanmadan bu görev başlatılamaz (config'ler env'den okunabilir
  olmalı).
- **Kabul kriteri:** Temiz bir makinede `.env` doldurup compose ile ayağa kaldırınca migration'lar
  uygulanmış, Swagger erişilebilir, login akışı çalışır olmalı.

### D3. İlk admin kullanıcısı (seed) stratejisi
- **Ne:** Şu an "Admin" rolünü alan bir mekanizma yok (register herkese "User" verir). Açılışta
  config'ten (`Seed:AdminEmail`, `Seed:AdminPassword` — env'den) ilk admin kullanıcıyı ve "Admin"
  rolünü idempotent şekilde oluşturan bir seed adımı eklenmeli.
- **Neden:** A1 sonrası admin uçları kilitlenecek; yeni kurulan her firma instance'ında bir admin'in
  var olması kurulumun parçası olmalı, elle DB'ye kayıt atmak ürünleşmeyle bağdaşmaz.
- **Kabul kriteri:** Boş DB ile açılan instance'ta config'teki admin login olup admin uçlarına
  erişebilmeli; ikinci açılışta mükerrer kayıt oluşmamalı.

### D4. Sipariş/checkout eksiği — analiz görevi
- **Ne:** `Cart`/`CartItem` var, `CartStatus.Ordered` enum'u var ama Cart/sipariş için **hiçbir
  controller ve servis yok**; Address de hiçbir uçtan kullanılmıyor. Bu görev analiz görevidir:
  sepet → sipariş akışının eksik parçalarını (endpoint listesi, stok düşme, fiyat snapshot kullanımı)
  çıkarıp ayrı bir tasarım dokümanı öner. Kod yazma.
- **Neden:** E-ticaret ürününün satılabilir olması için katalogdan fazlası gerekir; bu, planın bir
  sonraki büyük epiğidir ve ayrıca planlanmalı.

---

## Önerilen sıra ve bağımlılıklar

```
A1 → A2 → A3 → A4        (sıralı; A3 ve A4 bağımsız ama A3 önce)
B1, B2, B3               (A'dan bağımsız başlayabilir; B3, B2'den sonra)
C1, C3                   (her an yapılabilir, küçük)
C2, C4                   (B'lerden sonra; davranış değiştirir, dikkat)
D1                       (C2'den önce başlasa da olur; en geç D2'den önce)
D3 → D2                  (D2, A3+A4+D3'e bağımlı)
C5, D4                   (analiz görevleri, paralel)
```

## Delegasyon notu (yönetici → uygulayıcı modeller)

- Her görev tek oturumda, bu dosya bağlam verilerek delege edilebilir. Görev ID'siyle çalışılmalı
  ("REFACTOR-PLAN.md'deki A1'i uygula" gibi).
- Faz A ve B görevleri davranış değiştirir; uygulayıcı, kabul kriterlerini Swagger/manuel istekle
  doğrulayıp sonucu raporlamalı.
- C5 ve D4 kod üretmeyen analiz görevleridir; çıktıları bu dosyanın Notlar bölümüne eklenir.

## Notlar (uygulayıcılar buraya ekler)

- (boş)
