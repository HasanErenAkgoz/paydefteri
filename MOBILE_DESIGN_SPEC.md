# 📱 PayDefteri — Mobil UI/UX Tasarım & Sistem Spesifikasyonu (Open Design Prompt & Brief)

Bu doküman, **PayDefteri** mobil uygulamasının (iOS & Android) **Open Design** ve tasarım araçları ile uçtan uca modern, estetik ve profesyonel bir şekilde tasarlanması için hazırlanmış tam ürün spesifikasyonudur.

---

## 1. 🎯 Ürün Özeti & Amacı

**PayDefteri**, ortaklaşa girilen taksitli finansman planlarını (FuzulEv, Eminevim, Birevim vb. tasarruf finansman modelleri, konut/araç kredileri, ortak borçlar) ve ortak harcamaları (tatil, ev gideri, proje maliyeti) takip eden **yeni nesil ortak bütçe ve borç defteri** uygulamasıdır.

### Temel Değer Önermesi:
- Excel tabloları ve WhatsApp mesajları arasındaki karışıklığı bitirmek.
- *"Kim ne kadar ödedi?", "Kalan payım ne?", "Teslimata kaç gün kaldı?", "Kim kime borçlu?"* sorularını tek ekranda netleştirmek.

---

## 2. 🎨 Tasarım Dili & Görsel Estetik (Design Tokens)

### 2.1. Renk Paleti (Dark OLED First)
- **Arka Plan (Canvas):** `#090D16` (Derin OLED Gece Siyahı)
- **Kart / Yüzey (Surface Cards):** `#131B2E` (Hafif Mavi-Gri Tonlu Koyu Lacivert)
- **Yüzey Hover / Aktif:** `#1C273E`
- **Kenarlıklar (Borders):** `rgba(99, 102, 241, 0.18)` (İnce Indigo Vurgu)
- **Ana Vurgu (Primary Accent):** `#6366F1` (Canlı İndigo) → Gradient: `linear-gradient(135deg, #6366F1, #8B5CF6, #A78BFA)`
- **Başarı (Success):** `#10B981` (Zümrüt Yeşili — Ödendi durumları)
- **Uyarı / Bekleyen (Warning/Pending):** `#F59E0B` (Sıcak Amber — Yaklaşan vadeler & borçlar)
- **Kritik / Gecikmiş (Danger):** `#EF4444` (Mercan Kırmızısı)
- **Bilgi (Info):** `#0284C7` (Gök Mavisi)
- **Tipografi Renkleri:** 
  - Başlıklar / Ana Metin: `#F8FAFC`
  - İkincil / Açıklama Metinleri: `#94A3B8`
  - Pasif / Yardımcı: `#64748B`

### 2.2. Tipografi (Modern & Yüksek Okunabilirlik)
- **Font Ailesi:** `Outfit` (Başlıklar ve Büyük Rakamlar) + `Inter` (Arayüz Metinleri ve Tablolar)
- **Hiyerarşi:**
  - `Hero / Display`: 28px - 34px (Bold, -0.03em tracking)
  - `Title 1`: 22px - 24px (SemiBold)
  - `Title 2`: 18px - 20px (Medium)
  - `Body`: 15px - 16px (Regular / Medium, 1.5 line-height)
  - `Caption / Badge`: 12px - 13px (SemiBold / Medium)

### 2.3. Form & Kart Yapısı
- **Kart Köşe Yuvarlaklığı (Corner Radius):** `18px` – `22px`
- **Dokunma Hedefleri (Touch Targets):** Minimum `48px` yükseklik
- **Derinlik & Gölge:** `0 12px 32px rgba(2, 6, 23, 0.45)` + `1px inner glow border`
- **Buzlu Cam (Glassmorphism):** Alt çubuk ve modal pencerelerde `backdrop-filter: blur(20px)` ve `rgba(15, 23, 42, 0.85)` zemin.

---

## 3. 📱 Ekran Akışları & Sayfa Mimarisi

```
[Onboarding / Tanıtım]
       ↓
[Giriş & Kayıt Ekranı]
       ↓
[Planlarım (Plan Listesi & Seçici)] ──── (Yeni Plan / PDF-Excel Import)
       ↓
┌───────────────────────────────────────────────────────────┐
│              SABİT ALT MENÜ (BOTTOM NAV BAR)              │
├─────────────┬─────────────┬─────────────┬────────────┬────┤
│  📊 Takip   │ ⚙️ Kurulum  │  💾 Yedek   │ 📁 Planlar │ 👤 │
│ (Dashboard) │  (Setup)    │   (Data)    │  (List)    │ Prf│
└─────────────┴─────────────┴─────────────┴────────────┴────┘
```

---

### EKRAN 1: Tanıtım & Karşılama (Onboarding)
- **Görsel Düzen:** Minimalist, kartlı kaydırma (carousel) yapısı. Ortada karmaşık ekran görüntüleri yerine modern 3D/vektörel finans ikonları ve net tipografi.
- **İçerik:**
  1. *“Ortak plan, tek defter”* — Taksit ve giderleri tek ekranda toplama.
  2. *“‘Sen ödedin mi?’ sorusu tarihe karışsın”* — Otomatik pay ve mahsuplaşma.
  3. *“Fişini çek, gideri bölüş”* — Kamerayla anında harcama kaydı.
- **Aksiyonlar:** Ekran altında *“Ücretsiz Başla”* (Vurgulu Buton) ve sağ üstte *“Giriş”* linki.

---

### EKRAN 2: Giriş & Kayıt (Authentication)
- **Bileşenler:**
  - Modern koyu zemin, merkezde şık form kartı.
  - E-posta ve şifre giriş alanları (48px yükseklik, ikonlu, şifre göster/gizle butonu).
  - *“Beni Hatırla”* ve *“Şifremi Unuttum”* satırı.
  - *“Giriş Yap”* (Gradient Mor Buton) & *“Google ile Devam Et”* seçeneği.
  - Güvenlik rozeti: *“256-bit SSL şifreleme ile güvende”*.

---

### EKRAN 3: Plan Listesi (Planlarım & Şablonlar)
- **Bileşenler:**
  - **Aktif Plan Kartları:** Plan adı, toplam bütçe, tamamlanma yüzdesi çubuğu (%45 tamamlandı), ortak sayısı avatarları (`[E] [Y]`).
  - **Hızlı Başlangıç Kartı:** *"Yeni Plan Oluştur"* veya *"Ödeme Planı Yükle (PDF / Excel / CSV)"*.
  - **Hazır Şablonlar:** 120 Aylık Konut, 24 Aylık Araç, Ortak Tatil Bütçesi.

---

### EKRAN 4: Ana Takip Ekranı (Dashboard — Finans Defteri)
*Uygulamanın kalbi olan ekran.*
- **Üst Özet Çipleri (Stat Widgets):**
  - 👤 **Benim Payım:** Kalan açık borç tutarı, ödenen oran, sonraki taksit tarihi.
  - 🏠 **Teslimat / Tahsisat Geri Sayımı:** Kalan ay/gün göstergesi ve dairesel ilerleme halkası.
  - 🔄 **İç Borç / Mahsuplaşma:** Ortaklar arası net borç-alacak dengesi (*"Eren, Yusuf'a ₺4.500 borçlu"*).
- **İnteraktif Taksit Listesi (Installment Timeline):**
  - Ay bazlı kartlar: Taksit No, Vade Tarihi, Tutar (TL), Durum Rozeti (`✓ Ödendi` - Yeşil / `⏳ Bekliyor` - Amber).
  - Her taksitin altında **ortak bazlı ödeme butonları** (Tek dokunuşla *"Eren ödedi"* / *"Yusuf ödedi"* işaretleme).
  - Geciken taksitler için kırmızı uyarı vurgusu.

---

### EKRAN 5: Ortak Giderler & Harcamalar (Expenses)
- **Gider Akışı (Expense Feed):**
  - Kategori ikonları (Market 🛒, Fatura ⚡, Yakıt ⛽, Tadilat 🔨, Yemek 🍽️).
  - Harcama başlığı, tutar, harcamayı yapan ortağın avatarı ve bölüşüm detayı (*"Eren ödedi — 50/50 bölüşüldü"*).
  - Fiş/Fatura önizleme ikonu (Fiş eklenmişse küçük kamera rozeti).
- **Yüzen Hızlı Ekleme Butonu (FAB):** Ekranın sağ altında `+ Harcama Ekle` butonu.
- **Fiş Tarama Modalı (Bottom Sheet):** Kameradan veya galeriden fiş yükleyip yapay zeka ile tutarı otomatik doldurma.

---

### EKRAN 6: Kurulum & Ortak Yönetimi (Setup)
- **Ortak Kartları:** Ortak adı, e-posta, katılım durumu (Davet Edildi / Aktif).
- **Hisse & Paylaşım Oranları:** Slider veya sayısal yüzde girişi (%50 - %50, %70 - %30).
- **Taksit Tablosu Düzenleme:** Başlangıç tarihi, aylık artış oranı (enflasyon/vade artışı), teslimat ayı seçimi.

---

### EKRAN 7: Veri & Raporlama (Data / Yedek)
- **PDF Hesaplaşma Raporu:** Tek tuşla resmi döküm oluşturma ve WhatsApp/Mail ile paylaşma (`ShareSheet`).
- **Excel İçe / Dışa Aktarma:** `.xlsx` formatında defteri yedekleme veya sıfırlama.

---

### EKRAN 8: Profil & Güvenlik (Profile)
- Kullanıcı adı, e-posta, kayıtlı cihazlar ve aktif mobil oturumlar listesi (Oturumu Kapat seçeneği ile).
- Çıkış Yap butonu.

---

## 4. 🧭 Navigasyon & Ergonomi Prensipleri

1. **Sabit Alt Navigasyon Çubuğu (Bottom Tab Bar):**
   - 5 Ana Buton: `Takip`, `Kurulum`, `Yedek`, `Planlar`, `Profil`.
   - İkon + 11px etiket.
   - Aktif sekmede mor neon ışıma efekti (`box-shadow: 0 0 12px rgba(99, 102, 241, 0.5)`).
   - Alt kısımda `env(safe-area-inset-bottom)` boşluğu.

2. **Alt Çekme Pencereleri (Bottom Sheets):**
   - Tüm formlar ve detaylar tam ekran yerine ekranın altından yukarı kayan, üstünde tutma çubuğu (drag handle) olan pencerelerle açılır.

3. **Dokunma Haptikleri (Haptic Feedback):**
   - Ödeme yapıldığında ve onay kutularına basıldığında hafif titreşim geribildirimi.
   - Butonlara basıldığında `%97` oranında içe esneme (`transform: scale(0.97)`).

---

## 5. 🤖 Open Design İçin Doğrudan Prompt Taslağı (Kopyalanabilir)

Aşağıdaki metni **Open Design** komut satırına veya istem kutusuna yapıştırarak tasarımı doğrudan ürettirebilirsiniz:

```text
Create a modern, ultra-clean mobile app UI/UX design for "PayDefteri" (a shared installment and group expense tracking application).

Design Style & Theme:
- Deep OLED Dark Theme: Canvas #090D16, Card Surfaces #131B2E with subtle 1px border rgba(99,102,241,0.2).
- Accent Colors: Electric Indigo #6366F1, Vivid Violet #8B5CF6, Emerald Green #10B981 for paid states, Warm Amber #F59E0B for pending debts.
- Typography: Outfit for headers & numbers, Inter for UI labels.
- Layout: 8pt grid, rounded 20px card boundaries, frosted glass Bottom Navigation Bar (5 tabs: Tracker, Setup, Backup, Plans, Profile).

Key Screens to Design:
1. Onboarding Screen: Clean 3-step value carousel with modern minimal vectors and "Get Started" gradient CTA.
2. Login / Register: Sleek authentication card with email/password inputs, biometric prompt, and Google login.
3. Dashboard (Finans Defteri): 
   - Top metric chips: "My Share" (Remaining balance vs paid), "Delivery Countdown Ring" (Target month), and "Settlement Balance" (Who owes whom).
   - Installment timeline cards: Payment checkbox per partner with instantaneous status tags (Paid / Pending).
4. Expenses Feed: Transaction cards with category badges, payer avatar chips, and a Floating Action Button to scan receipt.
5. Setup & Partners: Partner cards with percentage allocation sliders and plan configuration.
6. Bottom Sheet Modals: Smooth swipe-down sheet for adding expenses and editing installments.
```
