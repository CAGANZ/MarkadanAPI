# Markadan — Claude Giriş Noktası

## Önce bunları oku (sırasıyla)

1. **`WHO.md`** — Çağan kim, nasıl çalışmak istiyor
2. **`WHAT.md`** — Markadan ne, mimari neden böyle, kararlar neden alındı
3. **`STATUS.md`** — Şu an neredeyiz, ne bitti, sırada ne var

Detaylı görev listesi ve yol haritası: **`REFACTOR-PLAN.md`**
Frontend handoff belgesi: **`~/MarkadanUI/docs/DURUM-RAPORU.md`**

---

## Hızlı başlangıç

```bash
# Tüm servisleri başlat
docker compose up -d

# Sadece API'yi yeniden build et
docker compose up -d --build api

# Logları izle
docker compose logs api -f
```

**Kritik — bunları unutma:**
- Elle yazılan her migration'a `[Migration("...")]` + `[DbContext(...)]` attribute'u ekle (Designer.cs yok)
- JWT key ve connection string `.env`'den gelir — hardcoded olmaz
- `.env` asla commit'lenmez

---

## Oturum sonu kontrol

- [ ] STATUS.md güncellendi
- [ ] REFACTOR-PLAN.md güncellendi
- [ ] Commit atıldı + push edildi
- [ ] Yeni backend uçları DURUM-RAPORU.md'ye eklendi
