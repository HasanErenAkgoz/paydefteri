# PayDefteri — Proje & Tanıtım Dokümanı

> Canlı: [https://paydefteri.com](https://paydefteri.com)  
> Repo: [github.com/HasanErenAkgoz/paydefteri](https://github.com/HasanErenAkgoz/paydefteri)  
> Son güncelleme: Ağustos 2026

---

## 1. Ürün nedir?

**PayDefteri**, birden fazla kişinin ortak girdiği borç / taksit / tasarruf planını tek yerden takip ettiği bir web uygulamasıdır.

Tipik senaryo: iki (veya daha fazla) kişi aynı konut veya araç planına giriyor. Kim hangi taksiti ödedi, kimin payı kaldı, teslimata kaç gün var, hesaplar dengede mi — bunlar Excel ve WhatsApp’ta dağılmasın diye tek ekranda toplanır.

İkinci senaryo: çift / ev ortaklığı — elektrik, market, internet gibi günlük giderler. Kim ne kadar pay aldı, kasada kim ne kadar ödedi, kim kime borçlu — Takip ekranında netleşir.

Üçüncü senaryo: tatil veya arkadaş grubu — birkaç günde birçok harcama, herkes tek tek ödemiyor. Ortak gider planında not + mahsup tek defterde kalır.

Dördüncü senaryo: işyeri ekip yemeği — “şunun yemeği şu kadar” mail listesi yerine kim ödedi / kim borçlu netleşir.

**Tek cümle:** Ortak ödeme planı için ortak defter. Excel + WhatsApp yerine tek ekran.

**Ne değildir?**
- Banka / ödeme altyapısı değil (para çekmez, havale yapmaz)
- Resmi tasarruf finansmanı şirketi ürünü veya ortağı değil
- Büyük “fintech platform” vaadi değil — pratik takip aracı

---

## 2. Hangi sorunu çözüyor?

| Bugün olan | PayDefteri’de |
|---|---|
| Herkes kendi Excel’ine bakıyor | Aynı plan, aynı gerçek |
| “Sen ödedin mi?” mesajları | Ödeme işaretleme + ortak pay özeti |
| Teslimat / vade unutuluyor | Geri sayım + yaklaşan vade uyarısı |
| “Ben ödedim, sen bana borçlusun” tartışması | Mahsuplaşma + PDF hesaplaşma |
| Market / faturayı iki kişi birlikte ödedi | Çoklu ödeyen + pay paylaşımı ayrı |
| Tatil / arkadaş grubunda herkes ayrı not tutuyor | Ortak gider planı + mahsup |
| Ekip yemeğinde mail listesi + “kim ödedi?” | Tek takip tablosu, net bakiye |
| Planı satır satır elle yazmak | Hazır şablon / PDF–Excel içe aktarma |

**Amaç:** İki veya daha fazla kişinin aynı plana bakıp aynı rakamı görmesi. Şeffaf, net, kavgasız.

---

## 3. Temel özellikler

### Taksit / tasarruf planı
- Plan oluşturma ve ortakları e-posta ile davet
- Taksit tablosu: bekleyen / kısmi / ödenen filtreleri
- Ortak payları (eşit, yüzde, özel pay)
- Ortaklar arası mahsuplaşma
- Teslimat / tahsisat geri sayımı
- Yaklaşan vade uyarısı + takvime ekleme (.ics)
- Hazır plan şablonları + PDF/Excel yükleme
- Yedek & rapor özeti
- PDF hesaplaşma raporu
- JWT ile hesap / giriş (Google giriş UI’da; backend yakında)

### Ortak gider planı (Expense)
- Ayrı plan tipi: fatura, market, mutfak gibi günlük ortak harcamalar
- Pay paylaşımı: eşit / varsayılan yüzde / özel tutar / tek ortak
- **Çoklu ödeyen:** paylar kim borçlu olduğunu, ödeme satırları kasada kim ne kadar verdiğini tutar
  - Örnek: Market 300 ₺, eşit pay (150 / 150). Ayşe 200, Mehmet 100 öderse bakiyede Mehmet Ayşe’ye 50 ₺ borçlu kalır
  - Takip’te paydaki kişi sayısı kadar ödeyen inputu (varsayılan); isteğe bağlı **Tek ödeme** ile tek kişi seçimi
- Net bakiye kartları + mahsup transferi (kimden → kime)
- Kategori, not, tekrarlayan gider şablonu
- Çift / ev ortaklığı için hazır “örnekle aç” şablonu
- Tatil / arkadaş grubu ve ekip öğle yemeği örnek şablonları
- (Çok para birimi / kur: v2 — bkz. ADR-002)

---

## 4. Teknik özet

| Katman | Teknoloji |
|---|---|
| Frontend | Angular 19 |
| Backend | ASP.NET Core 8, Clean Architecture, MediatR |
| ORM / DB | Entity Framework Core, PostgreSQL |
| Auth | JWT |
| Deploy | Docker Compose (API + Nginx web + Postgres) |
| Hosting | Hetzner VPS + Caddy reverse proxy |
| Domain | paydefteri.com (Cloudflare) |
| E-posta | Resend SMTP (`info@paydefteri.com`) |

Kod tarafında bazı namespace’ler hâlâ `FuzulTaksitTakip` (legacy); ürün markası **PayDefteri**.

---

## 5. Konumlandırma & hedef kitle

### Kim için?
- Ortak konut / araç / tasarruf planına girmiş 2–N kişi
- Ortak fatura / market takibi yapan çiftler ve ev ortaklıkları
- Tatil veya kısa gezi harcamasını bölen arkadaş grupları
- Ekip yemeği / ofis ortak hesabını netleştirmek isteyen ekipler
- “Excel yetmiyor” diyen küçük gruplar
- Şeffaf hesaplaşma isteyen arkadaş / aile / ortaklıklar

### Ton
- Samimi, yan proje / ürün sahibi dili
- Abartılı startup jargonu yok
- “İşimi görecek şekilde yaptım” — dürüst teknik anlatım LinkedIn’de işe yarıyor

### Marka cümleleri (kısa)
- Ortak plan, tek defter
- Kim ödedi · kalan ne · teslimata kaç gün
- Ortak ödeme ve taksit takibi
- Pay ayrı, ödeme ayrı — kim ne kadar verdi net
- Excel + WhatsApp yerine ortak defter
- Tatil ve ekip yemeği mahsupu da aynı defterde

---

## 6. Tanıtım stratejisi

### 6.1 Genel yaklaşım

1. **Önce LinkedIn** — teknik + kişisel hikâye; developer / ürün kitlesi
2. **Sonra Instagram** — aynı görseller, daha kısa caption
3. **Tekrarlayan içerik** — launch → özellik → sosyal kanıt / PDF → mini story

İlk hedef: bilinirlik + geri bildirim. Satış funnel’ı veya ücretli reklam şart değil; ürün ücretsiz / erken aşamada.

### 6.2 Ana mesaj (3 katman)

| Katman | Mesaj |
|---|---|
| Problem | Ortak taksit Excel + WhatsApp’ta dağılıyor |
| Çözüm | Tek ekranda kim ödedi / kalan / teslimat |
| Kanıt | Dashboard, takip tablosu, PDF rapor, mockup’lar |

### 6.3 Kanal planı

#### LinkedIn (öncelik)
- **İlk post:** Samimi launch metni + laptop mockup (`linkedin-01-hero`)
- **İkinci dalga:** Carousel (dashboard → tablo → şablon → rapor)
- **Takip postları:** Tek özellik (PDF, mahsuplaşma, vade)
- Hashtag abartma: 3–5 yeter (`#PayDefteri #Angular #DotNet #SideProject`)

#### Instagram
- Feed: `ig-feed-01` … `04` kareleri
- Story: teslimat geri sayımı + düzenle ekranı
- Caption kısa; link: paydefteri.com

#### Diğer (opsiyonel)
- GitHub README + repo star / screenshot
- Kişisel site / CV’de yan proje olarak link
- Reddit / Discord / Türk developer toplulukları — soft share, spam değil

### 6.4 Haftalık yayın iskeleti (öneri)

| Gün | Kanal | Ne |
|---|---|---|
| Pazartesi | LinkedIn | Launch / ana metin + hero mockup |
| Salı | Instagram Feed | Aynı hikâye, kısa caption |
| Çarşamba | Instagram Story | Teslimat sayacı |
| Cuma | LinkedIn | Tek özellik (PDF veya mahsuplaşma) |

### 6.5 Hazır LinkedIn metni (kullanılacak)

```
Hafta sonu projesi diye çıktım yola, sonunda yayınladım: paydefteri.com

Konu şu — iki kişi ortak bir taksit planına giriyor (konut, araç vs). İlk başta Excel yetiyor. Sonra “sen ödedin mi”, “benim payım ne kadardı”, “teslimat ne zamandı” derken herkes kendi tablosuna bakıyor. Ben bunu yaşadım, canım sıkıldı, uygulamayı ona yazdım.

PayDefteri’de planı açıyorsun, ortakların kim ödedi kim kaldı görünüyor, vade yaklaşınca uyarı geliyor, istersek PDF çıkarıp hesaplaşmayı netleştiriyorsun. Hazır şablon da var, elindeki PDF/Excel planını yükleyip içeri de alabiliyorsun — sıfırdan satır satır yazmana gerek yok.

Stack tarafı merak eden olursa: frontend Angular 19, backend ASP.NET Core 8 (Clean Architecture), DB PostgreSQL, auth JWT, deploy Docker. Abartılı bir şey değil, işimi görecek şekilde tuttum.

Bakıp yorum yazarsanız sevinirim. Kırık bir yer varsa da söyleyin, düzelteyim.
https://paydefteri.com

#PayDefteri #Angular #DotNet #PostgreSQL #SideProject
```

**Görsel:** `Desktop/Paydefteri /sosyal-medya/04-mockups/linkedin-01-hero-laptop.png`

### 6.6 Marka / hukuki dikkat

Tanıtım metinlerinde **tasarruf finansmanı firmalarının ticari unvanlarını** (Fuzul, Eminevim vb.) öne çıkarmamak daha güvenli.

- Tercih: “hazır şablon”, “PDF/Excel planını yükle”
- Kaçın: “resmi partner / onaylı entegrasyon” izlenimi, logo kullanımı
- Ürün içi şablon isimleri ayrı konu; marketing dili yumuşak tutulmalı

Detaylı görsel seçim / paylaşılmayacak ekranlar: `Desktop/Paydefteri /sosyal-medya/POSTLAR.md`

---

## 7. Görsel envanter

Klasör: `Desktop/Paydefteri /sosyal-medya/`

| Klasör | İçerik |
|---|---|
| `01-linkedin-carousel/` | Ham screenshot carousel adayları |
| `02-instagram-feed/` | Feed kareleri |
| `03-instagram-story/` | Story kareleri |
| `04-mockups/` | Laptop / telefon / kare mockup’lar (yayın için tercih et) |
| `99-kullanma-eski-veya-hassas/` | Paylaşma |
| `POSTLAR.md` | Kopyala-yapıştır metinler + sıralama |

**İlk yayın için tek görsel yeter:** `04-mockups/linkedin-01-hero-laptop.png`

---

## 8. Launch checklist

- [ ] LinkedIn metni + hero mockup yayınla
- [ ] İlk yoruma: “Denemek isteyen yazsın” / geri bildirim çağrısı
- [ ] Instagram feed + story (aynı hafta)
- [ ] paydefteri.com login/register’ın güncel göründüğünü kontrol et
- [ ] Gerçek plan isimleri (Eren & Yusuf) anonimleştirilecek mi — karar ver
- [ ] Gelen DM / yorumları not al (özellik istekleri)

---

## 9. Sonraki ürün / tanıtım adımları (opsiyonel)

**Ürün**
- Google ile giriş (UI hazır, backend yok)
- Şifremi unuttum
- Mobil responsive ince ayar / PWA
- Firma marka adları olmadan nötr şablon isimleri

**Tanıtım**
- 2–3 kullanıcıdan kısa alıntı (“Excel’den geçtik”)
- Kısa ekran kaydı (30 sn): ödeme işaretle → mahsup → PDF
- İngilizce tek post (dev.to / LinkedIn EN) — stack vurgulu

---

## 10. Tek bakışta özet

| | |
|---|---|
| Ürün | Ortak taksit / borç takip defteri |
| URL | https://paydefteri.com |
| Stack | Angular 19 · .NET 8 · PostgreSQL · Docker |
| İlk kanal | LinkedIn (samimi launch) |
| İlk görsel | `linkedin-01-hero-laptop.png` |
| Başarı ölçüsü (erken) | Yorum, DM, kayıt denemesi, bug bildirimi |

Bu doküman hem ürün anlatımı hem tanıtım playbook’u olarak tutulur; metin veya strateji değişince burayı güncelle.

SEO / Google indeksleme: [`docs/SEO.md`](SEO.md)
