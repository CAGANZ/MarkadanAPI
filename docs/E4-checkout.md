# GÖREV: E4 — Checkout

**DURUM:** ✅ Tamamlandı  
**Commit:** `4fc0817`  
**Tarih:** 2026-06-10

---

## NE YAPILDI

- `ICheckoutService` interface'i tanımlandı (`CheckoutAsync(userId, addressId)`)
- `CheckoutService` transaction içinde 5 adım uygulandı:
  1. Active sepet + ürünler yüklendi (tracking)
  2. Boş sepet kontrolü → `BusinessRuleException` (409)
  3. Fiyat snapshot doğrulaması — `UnitPriceSnapshot != Product.Price` ise 409
  4. Adres kullanıcıya ait mi kontrolü → `KeyNotFoundException` (404)
  5. Koşullu stok düşme: `ExecuteUpdateAsync WHERE Stock >= Quantity` — 0 satır etkilenirse yetersiz stok 409
- `Status=Ordered`, `OrderedAtUtc`, `UpdatedAt` set edildi
- `OrderNumber` kriptografik rastgele `MRK-XXXXXXXX` formatında üretildi
- Adres alanları checkout anında Cart'a kopyalandı (immutable snapshot: Street, City, State, PostalCode, Country)
- `POST /me/checkout` endpoint'i `[Authorize]` ile eklendi
- DI kaydı: `AddScoped<ICheckoutService, CheckoutService>()`

---

## KABUL KRİTERİ

| Kriter | Sonuç |
|---|---|
| `dotnet build` 0 hata 0 uyarı | ✅ Geçti |
| Transaction atomik: stok düşme + sepet güncelleme | ✅ |
| Fiyat değişiminde 409 | ✅ |
| Koşullu stok düşme (race condition güvenli) | ✅ |
| Adres snapshot — sipariş geçmişi immutable | ✅ |

---

**SIRADAKI:** E5
