# GÖREV: E5 — Sipariş Görüntüleme ve İptal

**DURUM:** ✅ Tamamlandı  
**Commit:** `eb75bee`  
**Tarih:** 2026-06-10

---

## NE YAPILDI

- `IOrderReadService` tanımlandı:
  - `GetOrdersAsync(userId)` — kullanıcının tüm siparişleri (`Status != Active`), azalan tarih sırası
  - `GetOrderAsync(userId, orderId)` — sipariş detayı; başkasının siparişi 404 döner
- `IOrderCommandService` tanımlandı:
  - `CancelAsync(userId, orderId)` — yalnızca `Ordered` durumu iptal edilebilir; `Cancelled/Active` ise 409
- İptal akışında transaction içinde stok iadesi: `ExecuteUpdateAsync Stock + Quantity`
- DTO'lar: `OrderSummaryDTO` (liste), `OrderDTO` (detay), `OrderItemDTO`
- `MeOrdersController` eklendi — 3 endpoint:
  - `GET /me/orders`
  - `GET /me/orders/{id}`
  - `POST /me/orders/{id}/cancel`
- Hepsi `[Authorize]`, kullanıcı yalnızca kendi siparişlerine erişir
- DI kaydı: `IOrderReadService`, `IOrderCommandService`

---

## KABUL KRİTERİ

| Kriter | Sonuç |
|---|---|
| `dotnet build` 0 hata 0 uyarı | ✅ Geçti |
| Başkasının siparişi 404 | ✅ |
| `Ordered` olmayan sipariş iptalinde 409 | ✅ |
| İptal transaction içinde stok iadesiyle atomik | ✅ |

---

**SIRADAKI:** E6
