# GÖREV: E6 — Admin Sipariş Uçları

**DURUM:** ✅ Tamamlandı  
**Commit:** `5f3d117`  
**Tarih:** 2026-06-10

---

## NE YAPILDI

- `IAdminOrderReadService` tanımlandı:
  - `GetOrdersAsync(status?, dateFrom?, dateTo?)` — opsiyonel filtrelerle tüm siparişler
  - `GetOrderAsync(orderId)` — kullanıcı bilgisi (UserId, UserEmail) dahil detay
- `IAdminOrderCommandService` tanımlandı:
  - `UpdateStatusAsync(orderId, status)` — durum güncelleme; ileride `Shipped/Delivered` eklemeye açık
  - `Active`'e geçiş engellendi (409)
  - `Cancelled`'a geçişte transaction içinde stok iadesi
- DTO'lar: `AdminOrderSummaryDTO` (UserId + UserEmail dahil), `AdminOrderDTO`, `UpdateOrderStatusDTO`
- `AdminOrdersController` eklendi — 3 endpoint:
  - `GET /admin/orders?status=&dateFrom=&dateTo=`
  - `GET /admin/orders/{id}`
  - `PUT /admin/orders/{id}/status`
- Hepsi `[Authorize(Policy="AdminOnly")]`
- DI kaydı: `IAdminOrderReadService`, `IAdminOrderCommandService`

---

## KABUL KRİTERİ

| Kriter | Sonuç |
|---|---|
| `dotnet build` 0 hata 0 uyarı | ✅ Geçti |
| Token'sız istek 401, User rollü 403, Admin rollü 200 | ✅ |
| `Active`'e geçiş engellendi (409) | ✅ |
| `Cancelled`'a geçişte stok iadesi transaction içinde | ✅ |
| Filtre parametreleri opsiyonel, hepsi birlikte çalışır | ✅ |

---

**SIRADAKI:** E7
