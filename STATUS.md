# STATUS — Markadan Şu An Neredeyiz

**Son güncelleme:** 2026-06-12
**Güncelleyen:** Claude (Çağan ile oturum)

---

## Şu an ne durumda

Backend ayakta (Docker, port 8080). 8 migration uygulanmış, DB sağlıklı.
Frontend ekibi handoff belgesiyle çalışıyor (`docs/DURUM-RAPORU.md`).
Aktif backend geliştirmesi yok — UI ekibinin GÖREV 7-9'u tamamlaması bekleniyor.

---

## Son oturumda ne yapıldı

**2026-06-11 / 2026-06-12**
- G1: Terk edilen sepet e-posta bildirimi (BackgroundService, 2 saat eşiği)
- G2: Favori listesi (WishlistItem) + fiyat/stok düşünce e-posta bildirimi
- G6: Düşük stok admin e-posta uyarısı (checkout sonrası fire-and-forget)
- G14: Toplu CSV ürün yükleme (`POST /admin/products/bulk`, max 10MB)
- G17: Mağaza ayarları (StoreSettings tek satır config tablosu)
- Bug: Migration Designer.cs yokken `[Migration]` attribute eklenmesi gerekiyor — düzeltildi
- Bug: Checkout sonrası cart guard yanlış yönlendiriyordu — frontend `ordered` flag ile düzeltti
- Frontend handoff raporuna GÖREV 7 (CSV), 8 (favori), 9 (mağaza ayarları) eklendi
- Her şey commit + push edildi

---

## Devam Eden

Yok — aktif çalışma duraksatıldı, UI ekibi çalışıyor.

---

## Sıradaki (öncelik sırasıyla)

1. **G16 iyzico ödeme entegrasyonu** — checkout'u tamamlıyor, müşteri talebi
2. **G9 kupon / indirim kodu** — satış arttırıcı
3. **G5 kargo takip kodu + müşteri bildirimi** — sipariş sonrası UX
4. **G7 sipariş listesi CSV export** — admin operasyonel ihtiyaç
5. **Deploy playbook** — 50-100 instance yönetimi için script/dokümantasyon
6. **F2 `POST /me/cart/accept-prices`** — frontend workaround var, düşük öncelik
7. **D1 test coverage** — 12 test var, genişletilecek

---

## Tamamlananlar

| Tarih | Görev | Notlar |
|-------|-------|--------|
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
| Çağan | Backend mimari + uygulama | Beklemede |
| Frontend ekibi | Next.js UI | DURUM-RAPORU.md GÖREV 7-9 |
| Claude (yüksek model) | Mimari tasarım, karmaşık logic | İstenince |
| Claude (düşük model) | Rutin kodlama, refaktör | İstenince |

---

## Dikkat — Unutma

- Elle yazılan her migration'a `[Migration("...")]` + `[DbContext(...)]` attribute'u ekle
- `dotnet` CLI WSL'de yok — migration'lar elle yazılır
- Frontend `api()` wrapper'ı FormData ile otomatik `Content-Type` ayarlamıyor olabilir —
  CSV yükleme görevinde frontend ekibi doğrudan fetch kullanabilir
- Migration geçmişi: `__EFMigrationsHistory` tablosunda 8 kayıt var
