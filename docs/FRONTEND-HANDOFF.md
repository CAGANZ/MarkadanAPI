# Markadan API — Frontend Teknik Handoff

**Hazırlayan:** Backend Ekibi | **Tarih:** 2026-06-10 | **API Versiyonu:** v1

---

## 1. Bağlantı

| Ortam | Base URL |
|---|---|
| Local (Docker) | `http://localhost:8080` |
| Production | `https://api.magazaniz.com` (deploy'da belirlenir) |
| Swagger (Geliştirme) | `http://localhost:8080/swagger` — yalnızca `ASPNETCORE_ENVIRONMENT=Development` |

> Docker'ı ayağa kaldırmak için: `cp .env.example .env` → değerleri doldur → `docker compose up -d`

---

## 2. Kimlik Doğrulama

**Yöntem:** JWT Bearer Token

Tüm korumalı isteklerde:
```
Authorization: Bearer <access_token>
```

### Akış

```
POST /auth/register  →  LoginResultDTO (token + refresh token)
POST /auth/login     →  LoginResultDTO
POST /auth/refresh   →  LoginResultDTO  (her refresh'te yeni çift verilir)
GET  /auth/me        →  MeDTO           (geçerli token gerekli)
```

### `LoginResultDTO`
```json
{
  "accessToken":   "eyJ...",
  "expiresAtUtc":  "2026-06-10T23:00:00Z",
  "refreshToken":  "abc123...",
  "userId":        1,
  "name":          "Ahmet",
  "surname":       "Yılmaz",
  "email":         "ahmet@test.com",
  "roles":         ["User"],
  "isAdmin":       false
}
```

### Token Stratejisi (önerilir)
- `accessToken` → memory'de tut (localStorage'a koyma)
- `refreshToken` → `httpOnly` cookie veya güvenli storage
- `expiresAtUtc` yaklaşınca `POST /auth/refresh` ile yenile
- Refresh başarısız (401) → kullanıcıyı login'e yönlendir

### `POST /auth/register`
```json
{
  "email":       "ahmet@test.com",
  "userName":    "ahmet",
  "password":    "Test123",
  "phoneNumber": "5551234567",
  "name":        "Ahmet",
  "surname":     "Yılmaz",
  "govId":       "12345678901",
  "birthday":    "1990-01-01T00:00:00Z"
}
```

### `POST /auth/login`
```json
{ "userNameOrEmail": "ahmet@test.com", "password": "Test123" }
```

### `POST /auth/refresh`
```json
{ "refreshToken": "abc123..." }
```

---

## 3. Genel Yanıt Formatları

### Başarılı
```
200 OK  →  { <DTO içeriği> }
```

### Hata — ProblemDetails formatı (tüm hatalar bu formatta)
```json
{
  "status": 409,
  "title":  "Business rule violated",
  "detail": "Sepet boş."
}
```

| HTTP Kodu | Ne zaman |
|---|---|
| `400` | Eksik/geçersiz alan (DataAnnotations) |
| `401` | Token yok veya süresi dolmuş |
| `403` | Token var ama yetki yok (User → Admin ucu) |
| `404` | Kayıt bulunamadı |
| `409` | İş kuralı ihlali (stok yok, mükerrer isim, fiyat değişti…) |
| `429` | Rate limit aşıldı (public katalog uçları) |
| `500` | Sunucu hatası |

---

## 4. Public Katalog Uçları (Auth gerekmez)

> ⚠️ **Rate limit:** IP başına dakikada 60 istek. Aşılırsa `429` döner.

### Ürünler

#### `GET /products`
```
?categoryId=1   → kategoriye göre filtrele
?brandId=2      → markaya göre filtrele
?q=nike         → başlık/marka/kategori içinde ara
?min=100        → minimum fiyat
?max=500        → maksimum fiyat
?sort=          → price_asc | price_desc | name_asc | name_desc | newest
?page=1         → sayfa no (varsayılan: 1)
?pageSize=12    → sayfa başı kayıt (varsayılan: 12)
```

**Yanıt:**
```json
{
  "total":    150,
  "page":     1,
  "pageSize": 12,
  "items": [
    {
      "id":           1,
      "title":        "Nike Air Max",
      "price":        1299.00,
      "imageUrl":     "https://...",
      "brandId":      1,
      "brandName":    "Nike",
      "categoryId":   2,
      "categoryName": "Ayakkabı"
    }
  ]
}
```

#### `GET /products/{id}`
```json
{
  "id":           1,
  "title":        "Nike Air Max",
  "price":        1299.00,
  "imageUrl":     "https://...",
  "brandId":      1,
  "brandName":    "Nike",
  "categoryId":   2,
  "categoryName": "Ayakkabı",
  "description":  "..."
}
```

> **Not:** Stok bilgisi public uçlarda **yoktur** — kasıtlı tasarım kararı.

### Markalar — `GET /brands` / `GET /brands/{id}`
```json
{ "id": 1, "name": "Nike", "description": "...", "imageUrl": "https://..." }
```

### Kategoriler — `GET /categories` / `GET /categories/{id}`
```json
{ "id": 1, "name": "Ayakkabı" }
```

---

## 5. Kullanıcı Uçları `[Authorize]`

### Adres Defteri `/me/addresses`

| Method | Uç | Açıklama |
|---|---|---|
| GET | `/me/addresses` | Liste |
| GET | `/me/addresses/{id}` | Detay |
| POST | `/me/addresses` | Yeni adres |
| PUT | `/me/addresses/{id}` | Güncelle |
| DELETE | `/me/addresses/{id}` | Sil |

**POST/PUT body:**
```json
{
  "addressName": "Ev",
  "street":      "Bağdat Caddesi No:1",
  "city":        "İstanbul",
  "state":       "Kadıköy",
  "postalCode":  "34710",
  "country":     "Türkiye"
}
```

---

### Sepet `/me/cart`

| Method | Uç | Açıklama |
|---|---|---|
| GET | `/me/cart` | Aktif sepeti getir |
| POST | `/me/cart/items` | Ürün ekle / miktar artır |
| PUT | `/me/cart/items/{id}` | Miktar değiştir (0 → satır silinir) |
| DELETE | `/me/cart/items/{id}` | Satır çıkar |
| DELETE | `/me/cart` | Sepeti tamamen boşalt |

**`GET /me/cart` yanıtı:**
```json
{
  "id":             5,
  "status":         "Active",
  "items": [
    {
      "id":                1,
      "productId":         3,
      "title":             "Nike Air Max",
      "imageUrl":          "https://...",
      "unitPriceSnapshot": 1299.00,
      "currentPrice":      1399.00,
      "priceChanged":      true,
      "quantity":          2,
      "subtotal":          2598.00
    }
  ],
  "total":           2598.00,
  "hasPriceChanges": true
}
```

> `priceChanged: true` varsa kullanıcıya **"Fiyat güncellendi"** uyarısı göster.
> Checkout'a geçmeden önce kullanıcının onaylaması gerekir (checkout 409 döner, sepet GET ile tazelenir).

**`POST /me/cart/items` body:**
```json
{ "productId": 3, "quantity": 2 }
```

**`PUT /me/cart/items/{id}` body:**
```json
{ "quantity": 0 }
```
> `quantity: 0` → satır silinir.

---

### Checkout & Siparişler

#### `POST /me/checkout`
```json
{ "addressId": 7 }
```

Başarılı → `200` + sipariş haline gelmiş CartDTO

Hatalar:
- `409` — sepet boş
- `409` — fiyat değişti (`detail` hangi ürünlerin değiştiğini yazar)
- `409` — yetersiz stok
- `404` — adres bulunamadı (başkasının adresi)

---

#### `GET /me/orders`
```json
[
  {
    "id":           5,
    "orderNumber":  "MRK-A3BX92KL",
    "status":       "Ordered",
    "orderedAtUtc": "2026-06-10T20:00:00Z",
    "total":        2598.00,
    "itemCount":    2
  }
]
```

#### `GET /me/orders/{id}`
```json
{
  "id":             5,
  "orderNumber":    "MRK-A3BX92KL",
  "status":         "Ordered",
  "orderedAtUtc":   "2026-06-10T20:00:00Z",
  "total":          2598.00,
  "items": [
    {
      "productId":         3,
      "title":             "Nike Air Max",
      "imageUrl":          "https://...",
      "unitPriceSnapshot": 1299.00,
      "quantity":          2,
      "subtotal":          2598.00
    }
  ],
  "shippingStreet":     "Bağdat Caddesi No:1",
  "shippingCity":       "İstanbul",
  "shippingState":      "Kadıköy",
  "shippingPostalCode": "34710",
  "shippingCountry":    "Türkiye"
}
```

#### `POST /me/orders/{id}/cancel`
- `409` → yalnızca `Ordered` siparişler iptal edilebilir

**Sipariş durum seti:** `Ordered` · `Cancelled`

---

## 6. Admin Uçları `[AdminOnly]`

> Admin token olmadan `401`, User token'la `403` döner.

### Ürün Yönetimi

| Method | Uç |
|---|---|
| GET | `/admin/products?categoryId=&brandId=&q=&min=&max=&sort=&page=&pageSize=` |
| GET | `/admin/products/{id}` |
| POST | `/admin/products` |
| PUT | `/admin/products/{id}` |
| DELETE | `/admin/products/{id}` |

> Admin listesinde public listeden farklı olarak **stok bilgisi** de gelir.

**POST body:**
```json
{
  "title":       "Nike Air Max",
  "description": "...",
  "price":       1299.00,
  "stock":       50,
  "imageUrl":    "https://...",
  "brandId":     1,
  "categoryId":  2
}
```

### Marka & Kategori Yönetimi

| Method | Uç |
|---|---|
| POST/PUT/DELETE | `/admin/brands` |
| POST/PUT/DELETE | `/admin/categories` |

### Sipariş Yönetimi

| Method | Uç | Açıklama |
|---|---|---|
| GET | `/admin/orders?status=&dateFrom=&dateTo=` | Filtreli liste |
| GET | `/admin/orders/{id}` | Detay (kullanıcı e-posta dahil) |
| PUT | `/admin/orders/{id}/status` | Durum güncelle |

**`PUT /admin/orders/{id}/status` body:**
```json
{ "status": "Cancelled" }
```
Geçerli değerler: `Ordered` · `Cancelled`
`Active`'e geçiş her zaman `409` döner.

---

## 7. Frontend Akış Rehberi

```
Ziyaretçi
  └── Katalog gezme            GET /products, /brands, /categories
  └── Kayıt / Giriş            POST /auth/register | /auth/login
        └── Token sakla        accessToken → memory, refreshToken → httpOnly cookie

Giriş yapmış kullanıcı
  └── Adres ekle               POST /me/addresses
  └── Ürün sepete ekle         POST /me/cart/items
  └── Sepeti görüntüle         GET  /me/cart  →  hasPriceChanges kontrolü
  └── Checkout                 POST /me/checkout  { addressId }
        ├── 200 → Sipariş onay sayfası (orderNumber göster)
        ├── 409 fiyat değişti → Sepeti GET ile yenile, uyar, tekrar dene
        └── 409 stok yok → Ürünü çıkar/azalt
  └── Sipariş geçmişi          GET  /me/orders
  └── İptal                    POST /me/orders/{id}/cancel

Admin paneli
  └── Ürün/Marka/Kategori CRUD
  └── Sipariş yönetimi         GET  /admin/orders?status=Ordered
  └── Durum güncelle           PUT  /admin/orders/{id}/status
```

---

## 8. Önemli Kurallar

| Konu | Kural |
|---|---|
| Fiyat | Ödeme her zaman `unitPriceSnapshot` üzerinden hesaplanır, `currentPrice` değil |
| Stok | Public uçlarda stok yok — "Stokta yok" butonu için checkout 409'undan anlaşılır |
| Adres | Checkout sonrası adres snapshot'ı alınır — kullanıcı adresi silse bile sipariş geçmişi bozulmaz |
| Sipariş no | `MRK-XXXXXXXX` formatı — kullanıcıya gösterilir, müşteri hizmetleri referansıdır |
| Token yenileme | Her refresh'te yeni `accessToken` + `refreshToken` çifti gelir — eski refresh token geçersizleşir |
| Güvenlik | Revoke edilmiş token tekrar kullanılırsa tüm oturumlar kapatılır → login sayfasına yönlendir |

---

## 9. Docker ile Yerel Kurulum

```bash
git clone https://github.com/CAGANZ/MarkadanAPI.git
cd MarkadanAPI
cp .env.example .env
# .env dosyasını düzenle
docker compose up -d
# API: http://localhost:8080
# Swagger: http://localhost:8080/swagger  (ASPNETCORE_ENVIRONMENT=Development gerekli)
```

İlk açılışta migration'lar otomatik uygulanır ve `.env`'deki `Seed__AdminEmail` / `Seed__AdminPassword` ile admin hesabı oluşturulur.

---

*Bu doküman backend'in güncel halini yansıtır. Değişiklik talebi için backend ekibiyle iletişime geç.*
