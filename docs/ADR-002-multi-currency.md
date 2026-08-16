# ADR-002: Çok para birimi ve kur (multi-currency / FX)

**Durum:** Proposed (henüz uygulanmadı)  
**Tarih:** 2026-08-03  
**Karar sahipleri:** ürün  
**İlgili:** [ADR-001](ADR-001-expense-plans.md) — gider planlarında para birimi bilinçli dışarıda bırakılmıştı.

## Bağlam

Kullanıcı geri bildirimi (tatil / yurt dışı): harcamalar USD, EUR, GBP olabilir; mahsup çoğu zaman TRY ister. “Google’daki kur” önerisi pratik görünür ama scrape yasal/kırılgan; resmi kur kaynağı gerekir.

Bugün tüm tutarlar TRY varsayımıyla (`tryCurrency`, settlement decimal) tutuluyor.

## Karar (v2 hedefi)

1. **Plan rapor para birimi:** Plan düzeyinde `ReportingCurrency` (varsayılan `TRY`). Net bakiye ve transferler bu birimde gösterilir.
2. **Gider satırı para birimi:** `Expense.Currency` + isteğe bağlı `FxRate` + `FxDate` + `AmountInReportingCurrency`.
   - Kullanıcı yabancı tutarı girer → kur (manuel veya API) ile rapor tutarına çevrilir.
   - Settlement / bakiye **yalnızca rapor tutarı** ile hesaplanır (mevcut matematiği kırmamak için).
3. **Kur kaynağı:** Önce **manuel kur** (yeterli MVP). Sonra TCMB veya ücretli FX API. **Google scrape yok.**
4. **Installment planları:** İlk sürümde TRY kalsın; çok para birimi öncelikle Expense.
5. **Kapsam dışı (v2):** Banka entegrasyonu, yemek kartı API, otomatik kur güncelleme job’u.

## Sonuçlar

- Artı: Yurt dışı tatil senaryosu; ADR-001 mahsup formülü korunur.
- Eksi: UI karmaşıklığı (birim seçici, kur tarihi); geçmiş satırlarda kur değişince yeniden değerleme politikası gerekir (öneri: satırda kilitli `FxRate`, değiştirilmez).
- Alternatif reddedildi: Tüm sistemi multi-currency floating’e çevirmek; Google scrape.

## Uygulama sırası (onay sonrası)

1. Domain: `ReportingCurrency` on Plan; `Currency` / `FxRate` / `FxDate` / `AmountInReportingCurrency` on Expense (+ migration)
2. Application: create/update validation; settlement uses reporting amount
3. Web: gider formunda birim + kur alanları; bakiyede TRY (veya plan birimi) etiketi
4. Opsiyonel: TCMB/FX provider abstraction + günlük cache
5. Test: aynı bakiyenin TRY-only ile birebir; karma birimli örnek golden case
