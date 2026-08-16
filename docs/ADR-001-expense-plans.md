# ADR-001: Ortak gider planları (Expense plans)

**Durum:** Accepted (canlıda)  
**Tarih:** 2026-08-02  
**Karar sahipleri:** ürün

## Bağlam

PayDefteri bugün tasarruf/taksit planı odaklı. Kullanıcı ihtiyacı: çift / ev ortaklığında fatura–market gibi günlük ortak harcamaları takip etmek (kim ödedi, nasıl paylaşıldı, kim kime borçlu).

## Karar

1. **Ayrı plan tipi:** `PlanType = Installment | Expense`. Mevcut planlar `Installment` kalır; gider planı ayrı aggregate davranışı ve UI rotası kullanır.
2. **Aynı çekirdek:** Partner, davet, üyelik, `ShareType` (default / equal / custom), mahsup matematiği yeniden kullanılır.
3. **Yeni bounded satırlar (Expense planında):**
   - `Expense` — ad, tutar, tarih, kategori, pay tipi, durum
   - `ExpenseShare` — özel pay satırları
   - `ExpensePayment` — kasada kim ne kadar ödedi (partner başına bir satır; Paid iken toplam = tutar)
   - `ExpenseCategory` — plan bazlı (veya sistem varsayılanları + özel)
   - `ExpenseRecurrence` — tekrarlayan fatura şablonu (aylık vb.) → dönemsel `Expense` üretir
   - `SettlementTransfer` — nakit/havale ile gerçek mahsup kaydı (tutar, kimden, kime, tarih, not)
4. **Pay ≠ ödeme:** Paylar borç paylaşımını, `ExpensePayment` satırları kasaya kim ne kadar verdiğini tutar. Tek ödeyen hâlâ desteklenir (tek satır / UI “Tek ödeme”).
5. **Mahsup:** Expense planında `balance += paidAmount − shareAmount`, ardından transferler. Installment planında mevcut davranış değişmez.
6. **Kapsam:** gider CRUD + çoklu/tek ödeyen + eşit/yüzde/özel pay + kategori + tekrarlayan + transfer + net bakiye UI. Banka entegrasyonu / çok para birimi **dışarıda** — bkz. [ADR-002](ADR-002-multi-currency.md).

## Sonuçlar

- Artı: Taksit UX’i kirlenmez; Splitwise benzeri ihtiyaç karşılanır; tek Identity + davet modeli.
- Eksi: İki ürün yüzeyi (dashboard vs expenses); settlement API’si tip’e göre dallanır; tekrarlayan iş için background veya “generate now” komutu gerekir.
- Alternatif reddedildi: Installment satırını genelleştirmek (gelecek ay kilidi / teslimat / şablon çakışması).

## Uygulama sırası (onay sonrası)

1. Domain + migration (`PlanType`, Expense*, SettlementTransfer)
2. Application/API + plan tipi guard’ları
3. Web: plan oluştururken tip seçimi; `/plans/:id/expenses` feature
4. Recurrence üretimi + transfer + birleşik bakiye
5. Test + deploy
