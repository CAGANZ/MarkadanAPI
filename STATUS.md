# STATUS — Markadan Şu An Neredeyiz

**Son güncelleme:** 2026-06-25
**Güncelleyen:** Claude (Çağan ile oturum)

---

## Şu an ne durumda

Backend ayakta (Docker, port 8080). **13 migration** uygulanmış, DB sağlıklı.
iyzico entegrasyonu tamamlandı (IYZWSv2 imza formatı düzeltildi) — sandbox hesabında "Checkout Form" aktif edilmesi bekliyor (Çağan'ın yapacağı). MockSuccess=true ile geliştirme devam ediyor.
Bu oturumda G3, G4, G12, G13, D1 tamamlandı.
UI ekibi DURUM-RAPORU.md GÖREV K–S için hazır.

---

## Son oturumda ne yapıldı

**2026-06-25**
- F2: `POST /me/cart/accept-prices` — fiyat değişen tüm item snapshot'larını güncel fiyata eşitler
  - UI'ın workaround'u (sil+yeniden ekle) artık gerek yok; atomik tek istek
- T4: `Product.IsActive` alanı
  - `PATCH /admin/products/{id}/active` — toggle endpoint
  - Pasif ürünler public katalogda (`ListAsync`, `GetDetailAsync`, `GetDetailBySlugAsync`, `GetRelatedAsync`) görünmez
  - Admin panel hepsini listeler (`ListAdminAsync` filtre yok)
  - Migration: `20260625000000_T4_ProductIsActive`
  - UI toggle'ı zaten hazırdı — BFF route eklenince aktive olur
- T6: Bulk yükleme güvenlik iyileştirmeleri
  - Satır limiti: maks 5.000 satır (aşılırsa erken 400)
  - Binary içerik kontrolü (MIME sniffing) — controller'da
  - `ImageUrl` şema doğrulaması: yalnızca http/https (SSRF koruması)
  - Rate limit: `bulk-upload` policy, dakikada 5 istek
- T3: Bulk yükleme UPSERT semantiği
  - Eşleştirme: `Title+BrandName` (case-insensitive)
  - Eşleşen ürün güncellenir; eşleşmeyen eklenir; dosyada olmayan silinmez
  - Boş `Description` mevcut açıklamayı silmez
- T2: `GET /admin/products/export` — CSV dışa aktarma
  - `q` ve `sort` parametreli; tüm ürünler (aktif+pasif)
  - BOM'lu UTF-8, formül enjeksiyonu sanitizasyonu
  - T1'in N×detay-çağrısı workaround'unu ortadan kaldırır
  - UI'da tek satır BFF route + `<a download>` yeterli

## Son oturumda ne yapıldı (önceki)

**2026-06-23**
- iyzico: IYZWSv2 imza formatına geçildi
  - Eski IYZWS/SHA256 yerine IYZWSv2: HMACSHA256(randomKey+path+body, secretKey)
  - Authorization header: `IYZWSv2 base64(apiKey:X&randomKey:X&signature:X)`
  - Error 11 hâlâ geliyor — imza doğru, sorun sandbox Checkout Form aktivasyonu
- G13: WhatsApp sipariş bildirimi (Telnyx)
  - `IWhatsAppService` + `TelnyxWhatsAppService` eklendi
  - Sipariş onaylandığında (CheckoutAsync ve ConfirmPaymentAsync) WhatsApp bildirimi gönderilir
  - `WhatsApp__Enabled=false` iken yalnızca log — gerçek mesaj gönderilmez
  - Telnyx hesabı hazır olunca `.env`'de `Enabled=true` yeterli
  - Frontend görevi: DURUM-RAPORU.md GÖREV S

**2026-06-22 (devam)**
- G4: Tekrar sipariş ver
  - `POST /me/orders/{id}/reorder` — tamamlanan siparişin ürünlerini aktif sepete kopyalar
  - Güncel fiyat snapshot alınır; stokta olmayan ürünler atlanır
  - Frontend görevi: DURUM-RAPORU.md GÖREV R
- G12: Benzer ürünler
  - `GET /products/{id}/related?limit=6` — aynı kategoriden, fiyata yakın, stokta olan ürünler
  - Frontend görevi: DURUM-RAPORU.md GÖREV S içinde veya bağımsız bölüm
- D1: Test iskeleti yeşile geçirildi
  - NSubstitute eklendi; CheckoutServiceTests constructor'ı güncellendi
  - `dotnet test` → 12/12 yeşil
- G3: Stok gelince haber ver
  - `StockNotification` entity + `StockNotifications` tablosu eklendi
  - `POST /products/{id}/notify-stock` — giriş gerektirmez, email + opsiyonel isim
  - Aynı email ile tekrar kayıt idempotent (hata vermez)
  - Admin ürün stokunu 0→pozitif güncellediğinde tüm abonelere mail → kayıt silinir
  - Migration: `20260622010000_G3_StockNotifications`
  - Frontend görevi: DURUM-RAPORU.md GÖREV Q
- G11: Ürün SEO slug + meta description
  - `Product`'a `Slug` (unique, Turkish collation) + `MetaDescription` alanları eklendi
  - `GET /products/{slug}` route'u eklendi (`GET /products/{id:int}` çalışmaya devam eder)
  - Create/Update'te başlıktan otomatik slug üretilir (Türkçe char dönüşümü + benzersizlik)
  - `Slug?` / `MetaDescription?` admin form alanları eklendi (isteğe bağlı)
  - Tüm DTO'lara (`ProductDetailDTO`, `ProductListDTO`, admin DTOları) slug eklendi
  - BulkUpload'da her satır için de otomatik slug üretilir
  - Migration: `20260622000000_G11_ProductSlug` — mevcut ürünlere geçici slug = CAST(Id)
  - Frontend görevi: DURUM-RAPORU.md GÖREV P

**2026-06-20**
- G7: Sipariş listesi CSV export
  - `GET /admin/orders/export?status=&dateFrom=&dateTo=` → CSV dosyası
  - Türkçe karakter desteği (BOM), RFC 4180 escape, harici paket yok
- G5: Kargo takip kodu + müşteri bildirimi
  - `TrackingNumber` / `TrackingUrl` Cart'a eklendi
  - `PUT /admin/orders/{id}/status` → status=Shipped iken tracking kabul eder
  - Shipped olunca müşteriye otomatik e-posta
  - Sipariş listesi sorguları PaymentPending'i artık filtreler (bug fix)
- G9: Kupon / indirim kodu sistemi tamamlandı
  - `Coupon` entity + `CouponType` enum (Percentage / FixedAmount)
  - Admin CRUD: `GET/POST/PUT/DELETE /admin/coupons`
  - Kullanıcı uçları: `POST /me/cart/coupon`, `DELETE /me/cart/coupon`
  - `CartDTO` güncellendi: `couponCode`, `discountAmount`, `FinalTotal` (computed)
  - Checkout'ta kupon tutarı düşülür; sipariş onaylanınca `UsageCount++`
  - iyzico'ya gönderilen tutar indirim sonrası; basket item'lar orantılı dağıtılır
  - Migration: `20260620000000_G9_Coupons`

**2026-06-13**
- iyzico sandbox entegrasyon hataları bulundu ve düzeltildi:
  - `.env`'deki `Iyzico__ApiKey__` yazım hatası → `Iyzico__ApiKey`
  - `docker-compose.yml`'de iyzico değişkenleri eksikti → eklendi
  - İmza algoritması yanlıştı (HMAC) → PKI string formatına çevrildi
  - Endpoint path'ları yanlıştı (`/auth/ecom` suffix) → standart path'lara düzeltildi
  - Controller'da IPv6→IPv4 dönüşümü eklendi
- iyzico testi: imza ve path artık doğru, "error 11" hâlâ geliyor — sandbox hesabında "Checkout Form" ürünü aktif edilmemiş
- Frontend ekibine GÖREV K (dinamik mağaza verisi) ve GÖREV L (middleware auth guard) verildi

**2026-06-12**
- G16: iyzico ödeme entegrasyonu backend'e eklendi
  - CartStatus'a PaymentPending/Preparing/Shipped/Delivered eklendi
  - `POST /me/checkout/initiate` → iyzico token döner
  - `POST /me/checkout/confirm` → ödeme doğrular, sipariş oluşturur
  - `POST /payment/iyzico/callback` → production server-to-server callback
  - iyzico SDK yerine HttpClient + REST API (namespace sorunu vardı)
- İptal kuralları güncellendi:
  - PaymentPending → serbest iptal
  - Ordered/Preparing → iyzico iade tetiklenir, stok iade edilir
  - Shipped → müşteri iptal edemez
  - Delivered → iptal yok
- Admin durum geçişleri kısıtlandı (Ordered→Preparing→Shipped→Delivered)
- Frontend handoff raporuna GÖREV I ve GÖREV J eklendi
- Migration 20260612100000_G16_IyzicoPayment uygulandı

**2026-06-11**
- G1, G2, G6, G14, G17 tamamlandı (bkz. önceki kayıt)

---

## Devam Eden

- **iyzico sandbox aktivasyonu**: iyzico desteğine yaz → "Checkout Form" ürününü aktif ettir → `POST /me/checkout/initiate` uçtan uca test edilecek. `MockSuccess=true` ile geliştirme devam ediyor.

---

## Sıradaki (öncelik sırasıyla)

**UI ekibi için:**
1. T4: BFF route `src/app/api/admin/products/[id]/active/route.js` (PATCH passthrough) — UI toggle zaten hazır
2. T2: BFF route `src/app/api/admin/products/export/route.js` + `<a download>` butonu
3. T5: Ürün varyantları — **önce backend mimari kararı bekleniyor** (büyük iş)
4. F2: `POST /me/cart/accept-prices` BFF route + UI güncelleme

**Backend için:**
1. **iyzico sandbox aktivasyonu** — iyzico desteğine yaz
2. **Deploy playbook** — 50-100 instance yönetimi
3. **G8 e-Fatura** — büyük efor, gerektiğinde
4. **D2+ test coverage** — happy-path testleri genişletmek

---

## Tamamlananlar

| Tarih | Görev | Notlar |
|-------|-------|--------|
| 2026-06-25 | F2 accept-prices | `POST /me/cart/accept-prices`, snapshot'ları atomik günceller |
| 2026-06-25 | T4 IsActive | `PATCH /admin/products/{id}/active`, pasif ürün public katalogda gizli |
| 2026-06-25 | T6 Bulk güvenlik | Satır limiti, MIME check, ImageUrl şema, rate limit |
| 2026-06-25 | T3 Bulk UPSERT | Title+Brand eşleşmesi, güncelle/ekle/silme |
| 2026-06-25 | T2 Ürün export | `GET /admin/products/export`, BOM'lu CSV |
| 2026-06-23 | G13 WhatsApp bildirimi | Telnyx, `IWhatsAppService`, sipariş onayında fire-and-forget |
| 2026-06-23 | iyzico IYZWSv2 | Auth header ve imza formatı güncellendi |
| 2026-06-22 | G4 Tekrar sipariş ver | `POST /me/orders/{id}/reorder`, güncel fiyat snapshot, stok kontrolü |
| 2026-06-22 | G12 Benzer ürünler | `GET /products/{id}/related`, aynı kategori, fiyata yakın |
| 2026-06-22 | D1 Test iskeleti | NSubstitute, 12/12 yeşil |
| 2026-06-22 | G3 Stok bildirimi | `POST /products/{id}/notify-stock`, mail on restore, auto-clear |
| 2026-06-22 | G11 SEO slug + meta | Slug, MetaDescription, `GET /products/{slug}`, auto-generate |
| 2026-06-20 | G7 CSV export | `GET /admin/orders/export`, filtreli, BOM, RFC 4180 |
| 2026-06-20 | G5 Kargo takip | TrackingNumber/URL, Shipped e-postası, PaymentPending filtre bug fix |
| 2026-06-20 | G9 Kupon / indirim kodu | Coupon CRUD admin, sepete uygula/kaldır, checkout entegrasyonu |
| 2026-06-12 | G17 Mağaza ayarları | StoreSettings, `GET /store-settings` public, `PUT /admin/settings` |
| 2026-06-11 | G14 CSV toplu yükleme | `POST /admin/products/bulk`, satır bazlı hata raporu |
| 2026-06-11 | G2 Favori listesi | WishlistItem, fiyat/stok düşünce bildirim e-postası |
| 2026-06-11 | G1 Terk edilen sepet | AbandonedCartBackgroundService, 2 saat eşiği |
| 2026-06-11 | G6 Düşük stok uyarısı | Checkout sonrası admin e-posta |
| 2026-06-10 | C5 Rate limiting | Public katalog uçlarına IP bazlı |
| 2026-06-10 | E1-E6 Sipariş akışı | Checkout, sipariş CRUD, adres snapshot |
| 2026-06-10 | A1-A5 Güvenlik | Admin endpoint auth, JWT env, DebugController kaldırıldı |
| 2026-06-10 | F1 dpkeys | Data Protection key persistence, Docker volume |

---

## Kim ne yapıyor

| Kişi / Araç | Sorumluluk | Şu an |
|-------------|------------|-------|
| Çağan | Backend mimari + uygulama | iyzico sandbox aktivasyonu (desteğe yaz) |
| Frontend ekibi | Next.js UI | DURUM-RAPORU.md GÖREV K–S |
| Claude (yüksek model) | Mimari tasarım, karmaşık logic | İstenince |
| Claude (düşük model) | Rutin kodlama, refaktör | İstenince |

---

## Dikkat — Unutma

- Elle yazılan her migration'a `[Migration("...")]` + `[DbContext(...)]` attribute'u ekle
- `dotnet` CLI WSL'de yok — migration'lar elle yazılır
- Frontend `api()` wrapper'ı FormData ile otomatik `Content-Type` ayarlamıyor olabilir —
  CSV yükleme görevinde frontend ekibi doğrudan fetch kullanabilir
- Migration geçmişi: `__EFMigrationsHistory` tablosunda **13 kayıt** var (son: `20260622010000_G3_StockNotifications`)
- iyzico `MockSuccess=true` — `.env`'de; sandbox aktivasyonu sonrası `false` yapılacak
