# PayDefteri UI/UX İnovasyon & Ürün Yol Haritası (Roadmap)
**Sürüm:** v2.1+ | **Hedef:** Web & Mobil (Android/iOS) Çapraz Platform Mimarisi  
**Tasarım Referansı:** `/ui-ux-pro-max` Fintech Standardı

---

## 📌 1. Vizyon ve Hedef
PayDefteri'ni sadece bir "taksit hesaplama aracı" olmaktan çıkarıp, ortaklar arasındaki finansal sürtünmeyi sıfıra indiren, motive edici, şeffaf ve keyifli bir **yeni nesil ortak finans defterine** dönüştürmek.

---

## 🚀 2. Yol Haritasındaki Temel İnovasyonlar

```
┌────────────────────────────────────────────────────────────────────────┐
│                        PAYDEFTERİ UX ROADMAP                           │
├────────────────────────────────────────────────────────────────────────┤
│  FAZ 1 (Hızlı Kazanım)    FAZ 2 (Görsel & Akış)    FAZ 3 (Tam Motivasyon) │
│  • 👁️ Gizlilik Modu       • ⚡ FAB Hızlı Menü       • 🏆 Kilometre Taşları │
│  • 💬 WhatsApp Hatırlatıcı • 📅 Mini Vade Şeridi    • 🎉 Konfeti Efekti    │
│  • 📱 Web & Mobil Uyum     • 📊 Kategori Grafiği    • 🎖️ Başarı Rozetleri │
└────────────────────────────────────────────────────────────────────────┘
```

---

### 1. 💬 1-Tıkla WhatsApp Ortak Hatırlatıcı & Paylaşım
* **Kullanıcı Problemi:** Ortak taksitlerde en büyük stres kaynağı, ortağa "vade geldi, payın şu kadar" diye mesaj yazma zorunluluğudur.
* **Mobil Deneyim:** Taksit kartında WhatsApp ikonu; tıklandığında WhatsApp uygulamasına hazır şablonla geçiş.
* **Web Deneyim:** WhatsApp Web API (`https://web.whatsapp.com/send?text=...`) veya tek tıkla panoya kopyalama (`navigator.clipboard`).
* **Mesaj Şablonu:**
  ```text
  🏠 PayDefteri: Fuzul Ev Planı
  📅 Sıradaki Vade: 05.09.2026 (#5 Taksit)
  💰 Kişi Başı Pay: ₺16.875,00
  🔗 Defteri İncele: https://paydefteri.com/p/fuzul-ev
  ```

---

### 2. 👁️ Metro & Toplu Taşıma Gizlilik Modu (Privacy Blur)
* **Kullanıcı Problemi:** Halka açık yerlerde, toplu taşımada veya ofiste ekran açıldığında büyük rakamların (`₺1.070.000`, `₺329.375`) başkaları tarafından görünmesi rahatsızlık yaratır.
* **Mobil & Web Tasarımı:**
  - Header'da **👁️ Göz İkonu** (Toggle).
  - Aktif olduğunda tüm parasal değerler `₺ ••••••` formatına dönüşür veya CSS `filter: blur(8px)` ile maskelenir.
  - Tercih `localStorage` üzerinden kalıcı saklanır.

---

### 3. 🏆 Kilometre Taşı & Gamification Rozetleri
* **Kullanıcı Problemi:** Yıllarca süren taksit ödemeleri psikolojik bir borç yükü hissettirir. İlerlemenin ödüllendirilmesi gerekir.
* **Tasarım & Rozet Hiyerarşisi:**
  - 🥉 **%25 Çeyrek Yol:** *"Tasarruf temeli atıldı!"*
  - 🥈 **%50 Yolun Yarısı:** *"Zirve aşıldı, ev teslimi yaklaştı!"*
  - 🥇 **%75 Son Düzlük:** *"Özgürlüğe son adımlar!"*
  - 👑 **%100 Borçsuzluk Zaferi:** *"Plan başarıyla tamamlandı!"*
* **Ödeme Kutlama Animasyonu:**
  - Taksit ödendiğinde hafif **haptik titreşim** (`Capacitor Haptics`) ve ekranda mikro **konfeti animasyonu** (`canvas-confetti`).

---

### 4. ⚡ Hızlı İşlem Butonu (FAB - Floating Action Button)
* **Mobil Tasarımı:** Sağ altta başparmak hizasında yüzen `+` butonu; tıklandığında alt çekmece (Bottom Sheet) açılır.
* **Web Tasarımı:** Sağ üst köşede veya header'da **"Hızlı İşlemler"** açılır menüsü (Dropdown).
* **Menü Seçenekleri:**
  - 📸 *Hızlı Dekont / Fiş Tara*
  - 💸 *Ekstra Gider / Harcama Ekle*
  - 🔄 *Mahsuplaşma Hesabı Kapat*
  - 💬 *Ortaklara Durum Özeti Gönder*

---

### 5. 📅 Yatay Mini Vade Ufku (Cashflow Horizon Bar)
* **Kullanıcı Değeri:** Gelecek 3 ayın nakit ihtiyacını tek bakışta planlama.
* **Mobil & Web Tasarımı:**
  - Dashboard'un üst kısmında 3'lü yatay kart şeridi:
    - *Bu Ay (Eylül):* **₺33.750** (18 Gün Kaldı)
    - *Gelecek Ay (Ekim):* **₺33.750**
    - *Sonraki Ay (Kasım):* **₺33.750**

---

### 6. 📊 Kategori Dağılımı (Mini Donut & Harcama Analizi)
* **Kapsam:** Taksitler, Organizasyon Bedeli, Noter Masrafları, Tapu ve Ekstra Harcamaların görsel pasta/donut grafik dağılımı.
* **Tasarım:** Saf CSS / SVG hafif Donut grafik bileşeni; harici ağır kütüphanelere ihtiyaç duymadan sıfır yük ile çalışır.

---

## 🛠️ 3. Fazlı Uygulama Takvimi (Release Plan)

| Faz | İnovasyon Kalemleri | Web Uyumluluğu | Mobil Uyumluluğu | Öncelik |
| :--- | :--- | :--- | :--- | :--- |
| **Faz 1** | 👁️ Gizlilik Modu + 💬 WhatsApp Paylaşım | ✅ Web API / Kopyalama | ✅ Doğrudan Intent URL | 🔥 Yüksek |
| **Faz 2** | ⚡ Hızlı FAB + 📅 Mini Vade Şeridi | ✅ Header Quick Action | ✅ Alt Çekmece (Drawer) | ⚡ Orta |
| **Faz 3** | 🏆 Kilometre Taşları + 🎉 Konfeti Efekti + 📊 Kategori Donut | ✅ Canvas Animasyonu | ✅ Native Haptics + Canvas | 💎 İnovasyon |

---

## 🎨 4. Tasarım Token'ları (`/ui-ux-pro-max`)

```scss
// PayDefteri Fintech Tasarım Tokenları
--pd-bg-oled: #090D16;
--pd-card-surface: #131B2E;
--pd-card-border: rgba(99, 102, 241, 0.18);
--pd-primary-indigo: #6366F1;
--pd-accent-violet: #8B5CF6;
--pd-success-emerald: #10B981;
--pd-warning-amber: #F59E0B;
--pd-danger-rose: #EF4444;

// Dokunma & Ergonomi
--pd-touch-target-min: 44px;
--pd-border-radius-card: 18px;
--pd-border-radius-pill: 20px;
--pd-blur-backdrop: blur(20px);
```
