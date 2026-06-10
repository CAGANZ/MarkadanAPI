# GÖREV: E7 — xUnit Test İskeleti

**DURUM:** ✅ Tamamlandı  
**Commit:** `36a01bc`  
**Tarih:** 2026-06-10

---

## NE YAPILDI

- `Markadan.Tests` (xUnit) projesi oluşturuldu ve solution'a eklendi
- **Teknik seçimler:**
  - SQLite in-memory — EF Core'un `ExecuteUpdateAsync` desteği için (EF InMemory desteklemez)
  - `Turkish_100_CI_AI` collation SQLite'a `CreateCollation` ile kayıt edildi
  - `IdentityServiceFactory` — AuthService testleri için UserManager/RoleManager/DataProtection ortamı
  - `TestDataSeeder` — checkout testleri için tekrar kullanılabilir veri tohumu

- **12 test, 12 yeşil:**

| Test | Konu |
|---|---|
| `AuthService.Refresh_RotatesToken_OldTokenRevoked` | Eski token revoke edilir, yeni token verilir |
| `AuthService.Refresh_ReuseDetection_RevokesEntireChain` | Revoke token reuse → tüm zincir iptal, DB'de aktif token=0 |
| `BrandCommandService.CreateAsync_DuplicateName` | Mükerrer isim → BusinessRuleException (409) |
| `BrandCommandService.UpdateAsync_DuplicateName` | Güncelleme sırasında isim çakışması → BusinessRuleException (409) |
| `ApiExceptionFilter.BusinessRuleException_Returns409` | BusinessRuleException → 409 |
| `ApiExceptionFilter.KeyNotFoundException_Returns404` | KeyNotFoundException → 404 |
| `ApiExceptionFilter.ArgumentException_Returns400` | ArgumentException → 400 |
| `ApiExceptionFilter.DbUpdateException_Returns409` | DbUpdateException → 409 |
| `CheckoutService.PriceChanged` | Snapshot ≠ güncel fiyat → BusinessRuleException |
| `CheckoutService.InsufficientStock` | Stok < miktar → BusinessRuleException |
| `CheckoutService.AlreadyOrdered` | Active sepet yok (reuse) → BusinessRuleException |
| `CheckoutService.StockRace` | Kullanıcı A alır (stok 1→0), kullanıcı B BusinessRuleException alır |

---

## KABUL KRİTERİ

| Kriter | Sonuç |
|---|---|
| `dotnet test` çözüm kökünde çalışır | ✅ Geçti |
| Tüm testler yeşil | ✅ 12/12 |
| `dotnet build` 0 hata 0 uyarı | ✅ |

---

**E1–E7 tamamlandı. Checkout epiği bitirildi.**
