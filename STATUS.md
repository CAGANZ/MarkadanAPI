# STATUS — Markadan Şu An Neredeyiz

**Son güncelleme:** 2026-06-22
**Güncelleyen:** Claude (Çağan ile oturum)

---

## Şu an ne durumda

Backend ayakta (Docker, port 8080). **11 migration** uygulanmış, DB sağlıklı.
iyzico entegrasyonu tamamlandı — sandbox hesabında "Checkout Form" aktif edilmesi bekliyor (Çağan'ın yapacağı).
Bu oturumda G9 (kupon), G5 (kargo takip), G7 (CSV export) tamamlandı.
UI ekibi GÖREV K–O için hazır; DURUM-RAPORU.md güncellenmiş durumda.

---

## Son oturumda ne yapıldı

**2026-06-22**
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

- **iyzico sandbox aktivasyonu**: `sandbox-merchant.iyzipay.com`'a gir → "Checkout Form" ürününü aktif et → `POST /me/checkout/initiate` uçtan uca test edilecek. Teknik düzeltmeler tamam, sadece hesap aktivasyonu bekliyor.

---

## Sıradaki (öncelik sırasıyla)

1. **iyzico sandbox key al** → `.env`'e ekle → test et
2. **G3 Stok gelince haber ver** — `StockNotification` entity + stok artışında mail
3. **G4 Tekrar sipariş ver** — `POST /me/orders/{id}/reorder`
4. **Deploy playbook** — 50-100 instance yönetimi
5. **D1 test coverage**

---

## Tamamlananlar

| Tarih | Görev | Notlar |
|-------|-------|--------|
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
| Çağan | Backend mimari + uygulama | iyzico sandbox aktivasyonu |
| Frontend ekibi | Next.js UI | DURUM-RAPORU.md GÖREV M (kupon UI), N (kargo takip), O (CSV buton) |
| Claude (yüksek model) | Mimari tasarım, karmaşık logic | İstenince |
| Claude (düşük model) | Rutin kodlama, refaktör | İstenince |

---

## Dikkat — Unutma

- Elle yazılan her migration'a `[Migration("...")]` + `[DbContext(...)]` attribute'u ekle
- `dotnet` CLI WSL'de yok — migration'lar elle yazılır
- Frontend `api()` wrapper'ı FormData ile otomatik `Content-Type` ayarlamıyor olabilir —
  CSV yükleme görevinde frontend ekibi doğrudan fetch kullanabilir
- Migration geçmişi: `__EFMigrationsHistory` tablosunda **11 kayıt** var (son: `20260622000000_G11_ProductSlug`)
