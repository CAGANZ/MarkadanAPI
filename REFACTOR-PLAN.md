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
- Swagger (C4 ile AutoMapper kaldırıldı)

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

### A1. Admin endpoint'lerini yetkilendir ✅
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

### A2. DebugController'ı kaldır veya kilitle ✅
- **Ne:** `Markadan.API/Controllers/DebugController.cs` ya tamamen silinmeli ya da yalnızca
  Development ortamında ve `AdminOnly` ile erişilebilir olmalı. Tercih: **sil** (DB sayıları zaten
  Swagger/admin uçlarından görülebilir).
- **Neden:** `/debug/db` anonim olarak DB sunucu adını (`DataSource`) ve veritabanı adını sızdırıyor.
  Müşteriye paketlenen her kopyada bu bilgi ifşası tekrarlanır.
- **Kabul kriteri:** `/debug/db` 404 dönmeli (veya korunmuş olmalı); build temiz.

### A3. Sırları konfigürasyondan/repodan çıkar ✅
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

### A4. CORS origin'lerini config'e taşı ✅
- **Ne:** `Program.cs:45-47`'deki sabit `localhost:3000` origin listesi `appsettings` →
  `Cors:AllowedOrigins` (string dizisi) üzerinden okunmalı.
- **Neden:** Her firmanın frontend'i kendi domain'inde çalışacak; origin deploy başına değişen bir
  değerdir, kod değişikliği gerektirmemeli.
- **Kabul kriteri:** Origin listesi config'ten geliyor; config boşsa anlamlı varsayılan/uyarı var.

---

## FAZ B — Bug ve veri bütünlüğü

### B1. Brand isim benzersizliği bug'ı ✅
- **Ne:** `Markadan.Infrastructure/Configurations/Brand_Cfg.cs` içinde `Name` için önce
  `HasIndex(...).IsUnique()` sonra **aynı property'ye** `HasIndex(...).IsUnique(false)` çağrılıyor;
  ikincisi birinciyi ezer. İkinci `HasIndex` satırını sil, unique index'i koru. Migration üret.
  `Category_Cfg.cs` ve diğer `*_Cfg.cs` dosyalarında aynı hatanın olup olmadığını kontrol et.
- **Neden:** Son commit'in amacı (be0e7bf: Türkçe harf farkından kaynaklı mükerrer isim engeli)
  Brand tablosunda fiilen devre dışı — DB'de unique constraint yok, korumayı yalnızca servis
  içindeki `AnyAsync` kontrolü sağlıyor; bu da eşzamanlı isteklerde (race condition) yarılabilir.
- **Kabul kriteri:** Migration'da Brands.Name üzerinde unique index oluşmalı; aynı isimle (büyük/
  küçük harf veya Türkçe karakter varyantıyla) ikinci brand eklenememeli.

### B2. GovId ve refresh token saklama güvenliği ✅
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

### B3. Refresh token reuse tespiti ✅
- **Ne:** `AuthService.RefreshAsync`'te revoke edilmiş bir token'la gelinirse şu an sadece 401
  dönüyor. Revoke edilmiş token kullanımı **çalıntı şüphesidir**: bu durumda o kullanıcının aktif
  tüm refresh token'ları revoke edilmeli (zincir invalidasyonu).
- **Neden:** Rotasyonun amacı tam da budur — eski token'ın tekrar kullanılması, token'ın iki kişide
  olduğunu kanıtlar; tek 401 saldırganın elindeki güncel zinciri yaşatır.
- **Kabul kriteri:** Revoke'lu token ile refresh denemesi sonrası, aynı kullanıcının elindeki güncel
  refresh token da geçersiz olmalı.

---

## FAZ C — Katman temizliği ve kod kalitesi

### C1. API → Infrastructure sızıntılarını kapat ✅
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

### C2. Validation'ı exception'dan ayır ✅
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

### C3. Çöp ve isimlendirme temizliği ✅
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

### C4. AutoMapper kararı: ya kullan ya kaldır ✅
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

### D2. Docker paketleme ✅
- **Ne:** `Markadan.API` için çok aşamalı (multi-stage) Dockerfile + örnek `docker-compose.yml`
  (API + SQL Server) + `.env.example` (tüm instance-bazlı değişkenlerin listesi: connection string,
  JWT ayarları, CORS origin'leri). README'ye "yeni firma kurulumu" bölümü eklenecek.
- **Neden:** "Her firmaya ayrı paket" hedefinin somut hâli budur: yeni müşteri = `.env` doldur +
  `docker compose up`. A3/A4 tamamlanmadan bu görev başlatılamaz (config'ler env'den okunabilir
  olmalı).
- **Kabul kriteri:** Temiz bir makinede `.env` doldurup compose ile ayağa kaldırınca migration'lar
  uygulanmış, Swagger erişilebilir, login akışı çalışır olmalı.

### D3. İlk admin kullanıcısı (seed) stratejisi ✅
- **Ne:** Şu an "Admin" rolünü alan bir mekanizma yok (register herkese "User" verir). Açılışta
  config'ten (`Seed:AdminEmail`, `Seed:AdminPassword` — env'den) ilk admin kullanıcıyı ve "Admin"
  rolünü idempotent şekilde oluşturan bir seed adımı eklenmeli.
- **Neden:** A1 sonrası admin uçları kilitlenecek; yeni kurulan her firma instance'ında bir admin'in
  var olması kurulumun parçası olmalı, elle DB'ye kayıt atmak ürünleşmeyle bağdaşmaz.
- **Kabul kriteri:** Boş DB ile açılan instance'ta config'teki admin login olup admin uçlarına
  erişebilmeli; ikinci açılışta mükerrer kayıt oluşmamalı.

### D4. Sipariş/checkout eksiği — analiz görevi ✅ (çıktı: aşağıdaki "D4 Analizi" bölümü)
- **Ne:** `Cart`/`CartItem` var, `CartStatus.Ordered` enum'u var ama Cart/sipariş için **hiçbir
  controller ve servis yok**; Address de hiçbir uçtan kullanılmıyor. Bu görev analiz görevidir:
  sepet → sipariş akışının eksik parçalarını (endpoint listesi, stok düşme, fiyat snapshot kullanımı)
  çıkarıp ayrı bir tasarım dokümanı öner. Kod yazma.
- **Neden:** E-ticaret ürününün satılabilir olması için katalogdan fazlası gerekir; bu, planın bir
  sonraki büyük epiğidir ve ayrıca planlanmalı.

---

## Önerilen sıra ve bağımlılıklar

```
A1 ✅ → A2 ✅ → A3 ✅ → A4 ✅
B1 ✅, B2 ✅, B3 ✅
C1 ✅, C2 ✅, C3 ✅, C4 ✅
D3 ✅ → D2 ✅  (Docker testi geçti: migration, login, JWT doğrulandı)

D4 ✅ (analiz tamamlandı — bkz. "D4 Analizi" bölümü)

── Kalan görevler ──────────────────────────────────────────────
C5 ✅  analiz tamamlandı — bkz. Notlar bölümü
D1     test iskeleti (xUnit) — iskelet var, kapsam genişletilecek
E1–E7  checkout epiği (D4 analizinde tanımlandı; E4'ten önce D1 şart)
G3–G13 ürün yol haritası görevleri (bkz. "Ürün Yol Haritası" bölümü)
```

## Delegasyon notu (yönetici → uygulayıcı modeller)

- Her görev tek oturumda, bu dosya bağlam verilerek delege edilebilir. Görev ID'siyle çalışılmalı
  ("REFACTOR-PLAN.md'deki A1'i uygula" gibi).
- Faz A ve B görevleri davranış değiştirir; uygulayıcı, kabul kriterlerini Swagger/manuel istekle
  doğrulayıp sonucu raporlamalı.
- C5 ve D4 kod üretmeyen analiz görevleridir; çıktıları bu dosyanın Notlar bölümüne eklenir.

## D4 Analizi — Sepet → Sipariş (Checkout) Akışı Tasarımı

> Bu bölüm D4 analiz görevinin çıktısıdır (2026-06-10). Kod yazılmamıştır; bir sonraki epik
> buradaki görev sırasıyla planlanabilir.

### Mevcut durum tespiti

| Parça | Durum |
|---|---|
| `Cart` entity | Var — `AppUserId`, `Status`, `CreatedAt/UpdatedAt`, `Items`. Sipariş alanları **yok** (adres FK, sipariş no, sipariş tarihi, toplam tutar) |
| `CartItem` entity | Var — `Quantity`, `UnitPriceSnapshot` alanı **tanımlı ama dolduran kod yok**; Product'a navigation/FK konfigürasyonu yok (sadece index var) |
| `CartStatus` enum | `Active = 0`, `Ordered = 1` (plan girişinde "Open" yazıyordu; gerçek ad **Active**). `Cancelled` ve teslimat durumları yok |
| `Address` entity | Var, user'a bağlı, **hiçbir uçtan/serviste kullanılmıyor**; Cart ile ilişkisi yok |
| `Product.Stock` | Var, ama düşüren kod yok; eşzamanlılık koruması (rowversion) yok |
| Servis/Controller | Cart, Address, Checkout için **hiçbiri yok** — sepet oluşturma ve ürün ekleme dahi mevcut değil |

### Model kararı: ayrı Order entity yerine "Cart = Order" yaklaşımı

Mevcut tasarım, sepetin `Status = Ordered`'a çevrilmesiyle siparişe dönüşmesini ima ediyor
(`UnitPriceSnapshot` bunun kanıtı). Bu yaklaşım korunabilir — küçük ölçekli butik mağaza için
yeterli ve ekstra tablo/kopyalama gerektirmez. Ancak şu eklemeler **şarttır**:

- `Cart`'a: `OrderedAtUtc (DateTime?)`, `ShippingAddressId (int?)` + navigation, `OrderNumber (string?)`
  (kullanıcıya gösterilecek, tahmin edilemez — örn. `MRK-{8 haneli rastgele}`)
- `CartStatus`'a: `Cancelled = 2` (ileride `Shipped/Delivered` eklenebilir; byte enum buna açık)
- `CartItem`'a: `Product` navigation + `HasOne(...).WithMany().OnDelete(Restrict)` konfigürasyonu
  (sipariş geçmişindeki ürün silinememeli) ve `(CartId, ProductId)` unique index (aynı ürün iki
  satır olmamalı, miktar artmalı)
- Adres ilişkisi kurulunca `Address_Cfg`'deki `Cascade` delete gözden geçirilmeli: siparişe bağlı
  adres silinirse sipariş kaydı adressiz kalır → `Restrict` veya adresin snapshot'ı (öneri:
  checkout anında adres alanlarını Cart'a kopyala — kullanıcı adresi sonradan değiştirse de
  sipariş kaydı bozulmaz)

### Fiyat snapshot neden önemli

`CartItem.UnitPriceSnapshot` ürün **sepete eklendiği andaki** fiyatı saklar. Bunsuz iki sorun olur:
1. Admin fiyat güncellerse müşterinin sepetindeki/geçmiş siparişindeki tutar değişir — geçmiş
   sipariş kaydı muhasebe açısından değişmez (immutable) olmalıdır.
2. `Product` silinir/değişirse sipariş geçmişi yeniden hesaplanamaz.

Kural: snapshot **sepete eklemede** yazılır; **checkout anında** güncel fiyatla karşılaştırılır —
fark varsa müşteriye "fiyat değişti" yanıtı dönülür (409) ve snapshot güncellenip onay istenir.
Sipariş verildikten sonra snapshot asla değişmez.

### Stok düşme stratejisi

- Stok **sepete eklemede düşülmez** (sepette bekletme stoku kilitlemez; terk edilmiş sepetler
  stoku süresiz rehin alır). Sepete eklemede yalnızca `Stock > 0` kontrolü yapılır (bilgilendirme
  amaçlı, garanti değil).
- Stok **checkout anında, tek transaction içinde** düşülür. Eşzamanlı iki checkout'un aynı stoku
  düşürmemesi için iki güvenli seçenek (öneri: ilki):
  1. **Koşullu UPDATE:** `UPDATE Products SET Stock = Stock - @q WHERE Id = @id AND Stock >= @q`
     (EF Core'da `ExecuteUpdateAsync` ile); etkilenen satır 0 ise yetersiz stok → transaction
     rollback + 409.
  2. `Product`'a `rowversion` concurrency token ekleyip `DbUpdateConcurrencyException` yakalamak.
- Sipariş iptalinde (`Cancelled`) stok aynı transaction içinde geri eklenir.

### Address ne zaman devreye girer

Adres CRUD'u checkout'tan **bağımsız ve önce** gelir (kullanıcı profilinde adres defteri).
Checkout isteği `addressId` taşır; servis adresin **o kullanıcıya ait olduğunu** doğrular
(başkasının adres ID'si → 404, enumeration'a karşı 403 değil). Sipariş kaydına adres,
yukarıdaki karara göre kopyalanarak (snapshot) bağlanır.

### Gerekli endpoint listesi

```
— Adres defteri (hepsi [Authorize], kullanıcı kendi kayıtları) —
GET    /me/addresses             adres listesi
POST   /me/addresses             yeni adres
PUT    /me/addresses/{id}        güncelle
DELETE /me/addresses/{id}        sil (aktif siparişte kullanılıyorsa engelle)

— Sepet ([Authorize]; kullanıcının tek Active sepeti olur, yoksa otomatik açılır) —
GET    /me/cart                  aktif sepeti getir (item'lar + güncel fiyat karşılaştırması)
POST   /me/cart/items            ürün ekle { productId, quantity } — varsa miktar artır
PUT    /me/cart/items/{id}       miktar değiştir (0 → satırı sil)
DELETE /me/cart/items/{id}       satırı çıkar
DELETE /me/cart                  sepeti boşalt

— Checkout / Sipariş ([Authorize]) —
POST   /me/checkout              { addressId } → stok düş + snapshot doğrula + Status=Ordered
GET    /me/orders                kendi sipariş listesi (Status != Active)
GET    /me/orders/{id}           sipariş detayı
POST   /me/orders/{id}/cancel    iptal (yalnızca henüz kargolanmamışsa; stok iade)

— Admin ([Authorize(Policy="AdminOnly")]) —
GET    /admin/orders             tüm siparişler (filtre: durum, tarih)
GET    /admin/orders/{id}        detay
PUT    /admin/orders/{id}/status durum güncelle (ileride Shipped/Delivered için)
```

`/me/*` prefix'i mevcut route stilini (`auth/me`) takip eder ve "kendi kaynağın" semantiğini
URL'de netleştirir.

### Servis katmanı (mevcut CQRS-lite desenine uygun)

- `IAddressService` (tek servis yeterli; read/command ayrımına gerek yok, basit CRUD)
- `ICartService` — `GetActiveCartAsync`, `AddItemAsync`, `UpdateItemQuantityAsync`,
  `RemoveItemAsync`, `ClearAsync`
- `ICheckoutService` — `CheckoutAsync(userId, addressId)`: tek transaction içinde
  (1) sepet boş mu, (2) fiyat snapshot ≟ güncel fiyat, (3) koşullu stok düşme,
  (4) adres doğrulama + kopyalama, (5) `Status=Ordered` + `OrderedAtUtc` + `OrderNumber`
- `IOrderReadService` / iptal için `IOrderCommandService`

### Uygulama sırası (her madde ayrı görev/commit)

```
E1. Domain + migration    Cart/CartItem/CartStatus/Address model eklemeleri (yukarıdaki karar listesi)
E2. Adres defteri         IAddressService + /me/addresses uçları
E3. Sepet                 ICartService + /me/cart uçları (snapshot'ı yazan yer burası)
E4. Checkout              ICheckoutService + POST /me/checkout (transaction + koşullu stok düşme)
E5. Sipariş görüntüleme   IOrderReadService + /me/orders, iptal akışı
E6. Admin sipariş uçları  /admin/orders
E7. Testler               D1 iskelesi üzerine: eşzamanlı checkout stok yarışı, fiyat değişimi
                          senaryosu, reuse edilen sepetin durumu (D1 tamamlanmadan E4 merge edilmemeli)
```

Bağımlılık: E1 hepsinden önce; E2 ile E3 paralel; E4, E2+E3'e bağımlı; E5/E6, E4'ten sonra.

---

## FAZ F — Üretim hazırlığı (2026-06-11 analizi)

### F1. Data Protection key kalıcılığı (KRİTİK — veri kaybı riski) ✅
- **Ne:** `Program.cs:50`'deki çıplak `AddDataProtection()` çağrısına key persistence ekle:
  `.PersistKeysToFileSystem(new DirectoryInfo("/app/dp-keys"))` + `.SetApplicationName("Markadan")`.
  Dizin config'ten okunabilir olmalı (`DataProtection:KeysPath`, env: `DataProtection__KeysPath`,
  varsayılan `/app/dp-keys`). `docker-compose.yml`'de api servisine named volume bağla:
  `dpkeys:/app/dp-keys` (volumes listesine `dpkeys:` ekle). `.env.example`'a değişkeni belgele.
- **Neden:** GovId alanları Data Protection ile şifreleniyor. Key'ler şu an container dosya
  sistemine yazılıyor; `docker compose down` sonrası kaybolur → mevcut DB'deki tüm GovId'ler
  **kalıcı olarak çözülemez** hale gelir. Multi-instance modelde bu her müşteri için veri kaybıdır.
- **Dikkat:** Container non-root `app` kullanıcısıyla çalışıyor (Dockerfile `USER app`); named
  volume'un yazılabilir olduğunu doğrula — gerekirse Dockerfile'da dizini oluşturup `chown app` yap.
- **Kabul kriteri:** `docker compose down && up` (volume SİLMEDEN) sonrası eski kullanıcıyla
  login + register akışı çalışmalı; `dpkeys` volume'unda `key-*.xml` dosyası görünmeli;
  api loglarında "may not be persisted" uyarısı kaybolmalı.

### F2. POST /me/cart/accept-prices — fiyat değişikliği onayı ✅

### G1. Terk edilen sepet e-postası ✅
- Cart'a `AbandonedReminderSentAt` alanı eklendi.
- `AbandonedCartBackgroundService`: her 30 dakikada kontrol, 2 saatten eski Active sepetlere
  ürün listesiyle hatırlatma maili gönderir. Config: `AbandonedCart:ThresholdHours/CheckIntervalMinutes`.

### G2. Favori / İstek listesi ✅
- `WishlistItem` entity + `IWishlistService` + `MeWishlistController` (GET/POST/DELETE /me/wishlist/items).
- `ProductCommandService.UpdateAsync` hook'u: fiyat düşünce veya stok sıfırdan artınca favorileyen
  kullanıcılara otomatik mail.

### Email altyapısı ✅
- `IEmailService` / `SmtpEmailService` (MailKit); config: `Email:SmtpHost/Port/User/Password/FromAddress`.
- `.env.example`'a Brevo örnek yapılandırması eklendi.

### F2. POST /me/cart/accept-prices — fiyat değişikliği onayı (önceki F2)
- **Ne:** `ICartService`'e `AcceptPriceChangesAsync(int userId, CancellationToken)` ekle:
  kullanıcının Active sepetindeki tüm satırların `UnitPriceSnapshot`'ını ürünün güncel
  `Price`'ına eşitler, `UpdatedAt`'i günceller, güncel `CartDTO` döner (`HasPriceChanges`
  artık `false` olmalı). `MeCartController`'a `POST me/cart/accept-prices` action'ı ekle
  (`[Authorize]`, diğer action'lardaki `TryGetUserId` deseniyle). Aktif sepet yoksa mevcut
  `GetOrCreate` davranışıyla tutarlı şekilde boş sepet dön (hata değil).
- **Neden:** Checkout fiyat değişiminde 409 dönüyor; UI şu an workaround olarak satırı silip
  yeniden ekliyor (2 istek + yarış riski). UI ekibi bu ucu açıkça talep etti
  (MarkadanUI/docs/DURUM-RAPORU.md §2). Tek atomik istek hem basit hem güvenli.
- **Kabul kriteri:** Admin fiyat değiştir → `GET /me/cart` `hasPriceChanges: true` →
  `POST /me/cart/accept-prices` → yanıtta `hasPriceChanges: false` ve snapshot'lar güncel →
  `POST /me/checkout` artık 409 fiyat hatası vermemeli. Build + mevcut 12 test yeşil;
  mümkünse CartService'e bir test ekle.

Sıra: F1 önce (bağımsız, kritik), sonra F2. Her biri ayrı commit.

---

## Ürün Yol Haritası (2026-06-11)

> Bu bölüm teknik görev değil, ürün kararıdır. Her özellik için **müşteriye/mağaza sahibine ne kazandırır**,
> **öncelik** ve **backend efor** verilmiştir. Öncelik sırası ürün sahibiyle netleştirilmeli; görev
> haline gelenler ilgili Faz'a taşınır.

---

### ACI 1 — Müşteri kaybı (sepeti terk etme, tekrar alışveriş yok)

#### G1. Terk edilen sepet e-postası
- **Kazanım:** Sepetini bırakıp çıkan müşteriye X saat sonra otomatik hatırlatma maili gider.
  Butik e-ticarette en yüksek ROI'li pazarlama aracıdır; dönüşüm oranını %5–15 artırdığı
  ölçülmüştür. Mağaza sahibinden sıfır efor.
- **Öncelik:** Yüksek
- **Backend efor:** Orta (arka plan job/Hangfire + mail gönderimi; sepet `UpdatedAt` + `Status=Active`
  kontrolü yeterli)
- **Not:** SMTP config zaten `.env`'e eklenebilir; tasarım/içerik mağaza sahibine ait.

#### G2. Favori / İstek listesi
- **Kazanım:** Müşteri beğendiği ürünü "şimdi değil ama yakında" diye işaretler; stok veya fiyat
  değişince bildirim alır. Geri dönüş oranını artırır, sepet boyutunu büyütür.
- **Öncelik:** Orta
- **Backend efor:** Küçük (yeni `Wishlist` + `WishlistItem` entity; CRUD uçları; stok/fiyat değişim
  hook'u e-posta tetikler)

#### G3. Stok gelince haber ver
- **Kazanım:** Tükenmiş ürünü görmek yerine e-posta bırakan müşteri rakibe gitmiyor.
  Mağaza sahibi talebi önceden görüp üretim/sipariş kararı verebilir.
- **Öncelik:** Orta
- **Backend efor:** Küçük (`StockNotification` kayıt tablosu; stok artışında toplu mail)

#### G4. Tekrar sipariş ver
- **Kazanım:** Müşteri geçmiş siparişinden tek tıklamayla aynı ürünleri sepete ekler.
  Düzenli müşteri deneyimini büyük platform seviyesine taşır.
- **Öncelik:** Orta
- **Backend efor:** Küçük (`POST /me/orders/{id}/reorder` — CartItem'ları kopyalar, güncel fiyat + stok
  kontrol eder)

---

### ACİ 2 — Operasyonel yük (manuel kargo, fatura, stok takibi)

#### G5. Kargo takip kodu + müşteri bildirimi
- **Kazanım:** Admin sipariş durumunu "Kargoda" yaparken takip kodu girer; müşteriye otomatik
  e-posta gider. "Kargom nerede?" soruları müşteri hizmetlerinden %60-80 düşer.
- **Öncelik:** Yüksek
- **Backend efor:** Küçük (siparişe `TrackingNumber (string?)` + `TrackingUrl (string?)` alanı;
  durum güncellemesinde mail tetikle)
- **Not:** Kargo firması API entegrasyonu bu bölümün dışında tutuldu — kargo firması seçimi
  mağaza sahibine göre değişir; URL alanı tüm firmalara çalışır.

#### G6. Stok uyarısı — admin'e e-posta ✅
- **Kazanım:** Stok eşiğin altına düşünce admin e-posta alır. Stoksuz satış riskini ortadan
  kaldırır; mağaza sahibi stok takibini her gün Excelden yapmak zorunda kalmaz.
- **Öncelik:** Yüksek
- **Backend efor:** Küçük — checkout'ta stok düştükten sonra eşik kontrolü + admin'e mail.
  Config: `Inventory:LowStockThreshold` (varsayılan 5), `Inventory:AdminEmail`.

#### G7. Sipariş listesi CSV/Excel export
- **Kazanım:** Admin tüm siparişleri (tarih aralığıyla) indirir; muhasebeci/muhasebe yazılımına
  aktarır. "Her siparişi tek tek açıp yazıyorum" acısını sona erdirir.
- **Öncelik:** Yüksek
- **Backend efor:** Küçük (`GET /admin/orders/export?from=&to=` → CSV; `CsvHelper` paketi yeterli)

#### G8. e-Fatura / e-Arşiv entegrasyonu
- **Kazanım:** Sipariş onaylanınca fatura otomatik kesilir; hem yasal zorunluluk karşılanır hem
  manuel girişten kaynaklanan hatalar biter.
- **Öncelik:** Orta
- **Backend efor:** Büyük (GİB entegrasyonu veya aracı (Logo, Parasut, e-Logo) API'si; mağaza
  sahibinin vergi yapısına göre değişir — bu özellik satış öncesi netleştirilmeli)
- **Not:** MVP için "sipariş onaylanınca admin'e fatura PDF gönder" yeterli olabilir; GİB
  entegrasyonu ikinci aşama.

---

### ACİ 3 — Rekabet (büyük platformlarla nasıl rekabet eder)

#### G9. Kupon / İndirim kodu
- **Kazanım:** "İlk alışverişe %10 indirim" gibi kampanyalar mağaza sahibinin en doğrudan
  müşteri edinme aracıdır. Büyük platformlarda standart; olmayınca mağaza eksik görünür.
- **Öncelik:** Yüksek
- **Backend efor:** Orta (`Coupon` entity — kod, tür (yüzde/sabit), min tutar, kullanım limiti,
  geçerlilik tarihi; checkout'ta uygulama; admin CRUD)

#### G10. Ürün yorumları ve puanlama — İSTENMİYOR
- Ürün sahibi kararı: bu özellik kapsam dışı bırakıldı (2026-06-11).

#### G11. SEO: Ürün slug'ı ve meta tag yönetimi
- **Kazanım:** `/products/nike-air-max-2024` hem Google'da sıraya girer hem paylaşılabilir.
  Organik trafik, reklam maliyeti sıfır. C5 analizinde önerilmişti; burada önceliklendirildi.
- **Öncelik:** Yüksek
- **Backend efor:** Küçük (`Product`'a `Slug (string)` + `MetaDescription (string?)` alanı;
  migration; admin formuna eklenir; frontend route güncellenir)

#### G12. Benzer ürünler / Çapraz satış
- **Kazanım:** "Bunu alanlar şunu da aldı" yerine "aynı kategoriden öneriler" — sepet ortalamasını
  artırır. Büyük platformun en görünür özelliklerinden biri; küçük mağazada yoksa boşluk hissedilir.
- **Öncelik:** Düşük
- **Backend efor:** Küçük (aynı kategori/brand'dan rastgele/popüler X ürün dönen endpoint;
  ML gerekmez, kural tabanlı yeterli)

#### G13. WhatsApp sipariş bildirimi (Türkiye'ye özel)
- **Kazanım:** Türkiye'de müşterilerin büyük çoğunluğu e-posta yerine WhatsApp'ı açar.
  Sipariş onayını ve kargo kodunu WhatsApp'tan almak müşteri memnuniyetini belirgin artırır;
  mağaza sahibini rakipten farklılaştırır.
- **Öncelik:** Orta
- **Backend efor:** Küçük (WhatsApp Business API veya Twilio/Telnyx üzerinden; phone number
  `AppUser`'da zaten var — sadece gönderim katmanı eklenir)

---

### Öncelik özeti

| # | Özellik | Öncelik | Backend Efor |
|---|---|---|---|
| G1 | Terk edilen sepet e-postası | Yüksek | Orta |
| G5 | Kargo takip kodu + bildirim | Yüksek | Küçük |
| G6 | Stok uyarısı | Yüksek | Küçük |
| G7 | Sipariş CSV export | Yüksek | Küçük |
| G9 | Kupon / indirim kodu | Yüksek | Orta |
| G10 | Ürün yorumları | Yüksek | Orta |
| G11 | SEO slug + meta tag | Yüksek | Küçük |
| G2 | Favori / istek listesi | Orta | Küçük |
| G3 | Stok gelince haber ver | Orta | Küçük |
| G4 | Tekrar sipariş ver | Orta | Küçük |
| G13 | WhatsApp bildirimi | Orta | Küçük |
| G8 | e-Fatura entegrasyonu | Orta | Büyük |
| G12 | Benzer ürünler | Düşük | Küçük |
| G14 | Toplu ürün yükleme (CSV) | Yüksek | Küçük |

#### G14. Toplu ürün yükleme (CSV import) ✅
- **Kazanım:** Admin yüzlerce ürünü tek CSV dosyasıyla yükler; yeni mağaza kurulumu veya
  sezon değişiminde saatlerce süren manuel giriş ortadan kalkar.
- **Öncelik:** Yüksek
- **Backend efor:** Küçük (`POST /admin/products/bulk` — multipart CSV; satır satır validate,
  başarılı/başarısız sayısı + hata listesi döner; brand/kategori adıyla eşleştirme)
- **CSV formatı:** `Title,Description,Price,Stock,ImageUrl,BrandName,CategoryName`

**Tavsiye:** G6, G14 bu oturumda implement edildi. G5, G7, G11 sıradaki hızlı kazanımlardır.
G9 (kupon) ürün kararı gerektirir; G10 kapsam dışı.

---

## Notlar (uygulayıcılar buraya ekler)

- **Docker doğrulama (2026-06-10):** `docker compose up -d` ile temiz ortamda ayağa kaldırıldı.
  Migration'lar otomatik uygulandı, admin seed çalıştı, `POST /auth/login` JWT token döndü.
  `ASPNETCORE_ENVIRONMENT=Development` ile Swagger erişilebilir durumda.

- **C5 Analizi — Public uçlarda ID enumeration (2026-06-10):**

  **Mevcut durum:**
  - `/products/{id}`, `/brands/{id}`, `/categories/{id}` sıralı int ID kullanıyor.
  - `ProductListDTO` ve `ProductDetailDTO`'da `Stock` **yok** — stok verisi anonim erişime kapalı.
  - Açık olan: ürün başlığı, fiyat, açıklama, görsel URL, brand/kategori adı.

  **Gerçek risk:**
  - Rakip `GET /products/1..N` tarayarak tüm katalog içeriğini ve fiyat listesini çekebilir.
  - Son geçerli ID katalog büyüklüğünü ele verir.
  - Multi-instance modelde her instance ayrı domain'de — rakip hangi URL'nin hangi mağazaya ait
    olduğunu bilmek zorunda, bu da pratikte riski düşürür.
  - Stok verisi kapalı olduğu için satış hızı analizi yapılamaz (en kritik ticari sır korunuyor).

  **Risk seviyesi: Düşük-Orta.** Butik mağaza genellikle 10–300 ürün barındırır; katalog
  içeriği zaten mağaza vitrininden görünür durumdadır.

  **Önerilen aksiyon (öncelik sırasıyla):**

  1. **Rate limiting ekle (önerilir, kod değişikliği yok):** ASP.NET Core 8+ built-in rate
     limiting middleware (`AddRateLimiter`) ile public uçlara IP başına dakikalık limit koy.
     Otomatik taramayı engeller, meşru kullanıcıyı etkilemez.

  2. **`Product`'a `Slug` ekle (SEO da kazanılır, opsiyonel):** `Title`'dan üretilen benzersiz
     slug alanı (`nike-air-max-2024`). Route'lar hem `/{id}` hem `/{slug}` destekleyebilir.
     Butik için SEO değeri yüksek; migration gerektirir.

  3. **Opak ID (UUID/ULID) — şimdi gerekmez:** B2B veya API-first senaryolarda anlamlı;
     butik mağaza için URL okunabilirliği bozar, maliyet faydasını karşılamaz.

  **Karar: Ürün sahibi rate limiting ile başlayıp SEO ihtiyacı doğunca slug ekleyebilir.
  Int ID'leri değiştirmek için acil gereksinim yok.**
