# PayDefteri Frontend UX Audit Sunumu

Tarih: 31 Ağustos 2026

Hazırlayan yaklaşım:
- `ecc:frontend-design-direction` ile ürün odaklı tasarım değerlendirmesi
- `ecc:browser-qa` ile canlı arayüz doğrulaması
- Kod incelemesi + canlı akış testi + responsive kontrol

## 1. Kapsam

İncelenen yüzeyler:
- Landing
- Login / Register
- Planlar
- Kurulum
- Taksit Dashboard
- Gider Takip
- Yedek & Rapor
- Harcama Analizi
- Profil

Canlı test ortamı:
- Frontend: `http://localhost:4200`
- API: `http://localhost:5096`
- Test hesabı ile gerçek onboarding yapıldı
- İki örnek plan üretildi:
  - `couple` gider planı
  - `fuzul` taksit planı

## 2. Yönetici Özeti

Genel sonuç:
- Ürün işlev olarak güçlü, ekran kapsamı geniş ve ürün değeri net.
- Görsel dil tutarlı ama fazla “tek katmanlı”; neredeyse her yerde aynı koyu kart + mor vurgu sistemi kullanılıyor.
- Masaüstünde bilgi yoğunluğu güçlü bir “power user” hissi veriyor.
- Mobilde temel kullanım mümkün; ancak bazı ekranlarda bilişsel yük artıyor ve öncelik sırası bulanıklaşıyor.
- En kritik UX fırsatı: bilgi mimarisini sadeleştirip her ekranda “ilk bakışta ne yapmalıyım?” sorusunu daha net cevaplamak.

Skor kartı:

| Alan | Skor / 10 | Not |
|---|---:|---|
| Görsel tutarlılık | 8 | Dil tutarlı ama tek tonlu |
| Bilgi hiyerarşisi | 6 | Özellikle dashboard ve setup yoğun |
| Yeni kullanıcı onboarding | 7 | Landing ve auth güçlü, içeride yönlendirme zayıflıyor |
| Masaüstü verimliliği | 8 | Güçlü ama bazı ekranlarda kalabalık |
| Mobil kullanılabilirlik | 6 | Çalışıyor ama önceliklendirme ve taşma riskleri var |
| Erişilebilirlik temeli | 5 | Label/name eksikleri gözlendi |
| Güven duygusu / finans ürünü hissi | 7 | Güçlü ama daha rafine olabilir |

## 3. Güçlü Taraflar

### 3.1 Ürün değeri ilk ekrandan anlaşılabiliyor
- Landing mesajı net: “ortak plan, tek defter”.
- Kullanım senaryoları soyut değil; gerçek örneklerle anlatılıyor.
- Auth sayfaları ürün bağlamını kaybetmiyor.

### 3.2 Şablon yaklaşımı çok doğru
- Hazır şablonlar ürünün “boş ekran korkusunu” azaltıyor.
- Plan türlerinin hem günlük gider hem uzun vadeli taksit senaryolarını kapsaması güçlü.
- Özellikle ilk kullanım için değer kanıtı hızlı geliyor.

### 3.3 Masaüstünde operasyonel derinlik iyi
- Dashboard, gider takibi ve kurulum ekranları tek yerde çok iş yaptırabiliyor.
- Filtreler, durum rozetleri ve özet kartları tekrar kullanım için doğru bir temel oluşturuyor.

### 3.4 Finans ürününe uygun güven sinyalleri var
- Gizlilik modu
- Güvenlik vurguları
- PDF / dışa aktarma / rapor
- AI koçu için veri paylaşımı açıklaması

## 4. Kritik Bulgular

### 4.1 Bilgi yoğunluğu bazı ekranlarda görev odağını gölgeliyor

En çok etkilenen alanlar:
- `dashboard`
- `setup`
- `expenses`

Sorun:
- Ekrana aynı anda çok fazla eşit ağırlıklı kutu, filtre, tablo ve eylem yükleniyor.
- Kullanıcı hangi sırayla ilerleyeceğini kendi çözmek zorunda kalıyor.

Etkisi:
- Yeni kullanıcıda öğrenme yükü artıyor.
- Mobilde tarama maliyeti yükseliyor.
- Güçlü özellikler var ama keşfedilebilirlik düşüyor.

Örnek:
- Taksit dashboard’unda üst özet, yaklaşan vade, mahsup, teslimat, filtreler, tablo ve aksiyonlar aynı anda yarışıyor.

### 4.2 Mobilde bazı yüzeyler “çalışıyor” ama optimize hissettirmiyor

Canlı gözlem:
- Harcama Analizi boş durumda drop zone genişliği mobil görünümde viewport sınırını hafif aşıyor.
- Ölçüm: mobil görünümde `drop-zone` genişliği yaklaşık `513.9px`, sol taşma `-6.9px`.

Etkisi:
- Görsel hizalama hissi bozuluyor.
- Mobilde güven ve kalite algısı zayıflıyor.

Kaynak:
- [spending-analysis.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/spending-analysis/spending-analysis.component.scss)

### 4.3 Global navigasyon ürün derinliği büyüdükçe yetersiz kalmaya başlıyor

Sorun:
- Header ve alt menü aynı anda çok fazla birincil destinasyon taşıyor.
- “Plan bağlamı içi gezinme” ile “ürün geneli gezinme” aynı seviyede sunuluyor.

Etkisi:
- Kullanıcı plan içinde mi, hesap genelinde mi olduğunu zihninde sürekli yeniden kuruyor.
- Mobilde alt menü alanı dar olduğu için etiketler kısalıyor ama anlam derinliği artıyor.

### 4.4 Erişilebilirlikte temel form sorunları var

Canlı DevTools bulguları:
- Setup ekranında label association eksikleri
- Profile ekranında label association eksikleri
- Dashboard akışında form field `id/name` eksikleri

Etkisi:
- Screen reader deneyimi zarar görür.
- Form alanı anlamı yalnızca görsel bağlama kalır.
- Ürün profesyonelliği ve yasal erişilebilirlik olgunluğu düşer.

İlgili dosyalar:
- [setup.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/setup/setup.component.html)
- [profile.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/profile/profile.component.html)
- [dashboard.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/dashboard/dashboard.component.html)

### 4.5 Görsel sistem tutarlı ama fazla homojen

Sorun:
- Birçok ekranda aynı kart, aynı mor vurgu, aynı koyu zemin dili tekrarlanıyor.
- Bilgi önem derecesi ile görsel ağırlık her zaman eşleşmiyor.

Etkisi:
- Güçlü ürün alanları birbirinden ayrışamıyor.
- Landing, setup ve operasyon ekranları arasında ton farkı zayıf kalıyor.

Kaynaklar:
- [styles.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/styles.scss)
- [app.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/app.component.scss)

## 5. Ekran Bazlı Değerlendirme

### 5.1 Landing

Artılar:
- Mesaj net.
- Hero ürün odaklı.
- Mobil onboarding akışı fikri iyi.

Fırsatlar:
- Mobil onboarding ile klasik landing iki farklı hikaye anlatıyor; marka sesi bir miktar bölünüyor.
- Değer önerisi net ama güven/kanıt katmanı daha güçlü olabilir.

Öneriler:
- “Kimler için?” ve “hangi senaryoda?” bloklarını daha kısa, daha kanıt odaklı hale getirin.
- Hero altına canlı metrik, örnek çıktı veya mini workflow kanıtı ekleyin.

### 5.2 Login / Register

Artılar:
- Ürün bağlamı korunuyor.
- Form düzeni temiz.
- Güven mesajı mevcut.

Fırsatlar:
- Sol taraftaki preview dekoratif; gerçek ürün akışı hissini daha az taşıyor.
- İlk kez gelen kullanıcı için “neden şimdi hesap açmalıyım?” mesajı biraz statik.

Öneriler:
- Preview’ı gerçek ekran kesiti veya mikro başarı hikayesine çevirin.
- Register sayfasında 3 adımlık kurulum beklentisini önden gösterin.

### 5.3 Planlar

Artılar:
- Şablon yaklaşımı güçlü.
- Kart mantığı anlaşılır.
- Davet ve arşiv mantığı aynı yüzeyde toplanmış.

Fırsatlar:
- Sekme sayısı artınca yatay şerit yönetimi mobilde yorucu olabilir.
- “Planlarım”, “Hazır Şablonlar”, “Belgeden Aktar”, “Sıfırdan Aç” aynı önem katmanında.

Öneriler:
- “Yeni plan oluştur” tek ana CTA olsun.
- Altında yöntem seçimi sheet/modal veya stepper ile açılsın.

### 5.4 Kurulum

Artılar:
- Ürün gücünü gösteren kritik ekran.
- Tekrarlayan işlerin merkezi olması doğru.

Fırsatlar:
- Genel, ortaklar, taksitler, araçlar sekmeleri işlevsel ama onboarding açısından “workflow” hissettirmiyor.
- Kullanıcıyı kurulum tamamlama sırasına yönlendiren net ilerleme göstergesi eksik.

Öneriler:
- Üstte “Kurulum tamamlanma yüzdesi” ve sonraki önerilen adım gösterin.
- Owner ve non-owner deneyimini daha sert biçimde ayırın.

### 5.5 Taksit Dashboard

Artılar:
- Finansal görünürlük güçlü.
- Yaklaşan vade, teslimat, ilerleme gibi kritik metrikler iyi seçilmiş.

Fırsatlar:
- Masaüstünde iyi, mobilde uzun liste ve tekrar eden satır aksiyonları yorucu.
- “Hangi taksit bugün aksiyon gerektiriyor?” sinyali daha da öne çıkmalı.

Öneriler:
- Mobilde varsayılan görünüm “Aksiyon Gerektirenler” filtresi ile açılsın.
- Tüm taksit listesi yerine önce özetlenmiş kartlar, sonra detay listesi gelsin.

### 5.6 Gider Takip

Artılar:
- Operasyonel güç yüksek.
- Filtre derinliği iyi.
- Özet ve mahsup sekmeleri mantıklı.

Fırsatlar:
- Filtre yoğunluğu ilk kullanımda korkutucu olabilir.
- Liste görünümünde satır başına karar verme yükü fazla.

Öneriler:
- “Basit görünüm / gelişmiş filtreler” ayrımı ekleyin.
- Mobilde ilk etapta sadece arama + durum filtresi açık, diğerleri açılır panelde olsun.

### 5.7 Harcama Analizi

Artılar:
- Güven mesajı iyi.
- AI koçu için veri açıklaması olumlu.
- İçerik yapısı olgunlaşmaya uygun.

Fırsatlar:
- Boş durumun drop zone alanı mobilde daha kontrollü olmalı.
- Bu ekran, diğer plan akışlarından ayrı bir ürün gibi hissetmeye başlıyor.

Öneriler:
- Analizi “kişisel finans laboratuvarı” gibi ayrı bir alt ürün diliyle rafine edin.
- İlk kullanımda örnek ekstre veya demo sonuç gösterin.

### 5.8 Profil

Artılar:
- Ayarlar hub yaklaşımı iyi.
- Hesap, güvenlik, cihaz ayrımı mantıklı.

Fırsatlar:
- Kapsam küçük olmasına rağmen görsel yapı büyük ve biraz ağır.
- Mobilde tab satırı çalışıyor ama içerik önem derecelerini daha sakin sunmak mümkün.

Öneriler:
- Hesap ekranında daha kompakt düzen kullanılabilir.
- Güvenlik bölümünde inline yardım metinleri sadeleştirilebilir.

## 6. Mobil vs Masaüstü

### Masaüstü
- Güçlü taraf: yoğun iş akışları destekleniyor.
- Zayıf taraf: bazı ekranlarda her öğe fazla “birincil” davranıyor.

### Mobil
- Güçlü taraf: temel mimari responsive; alt menü ve kart yapıları genel olarak ayakta.
- Zayıf taraf: uzun finans tabloları, büyük formlar ve çok sayıda kontrol aynı anda gelince öncelik sırası kayboluyor.

Mobil için net karar:
- Mobilde masaüstünün küçültülmüş hali değil, görev öncelikli özet yüzeyler gerekli.

## 7. Tasarım Yönü Önerisi

Önerilen yön:
- Ton: güven veren, operasyonel, rafine
- Hedef: “Excel yerine geçen ciddi finans çalışma alanı”
- Ana ilke: önce aksiyon, sonra detay
- Ayırt edici detay: plan bazlı durum merkezi

Bu yön için yapılması gerekenler:
- Her sayfada tek baskın birincil CTA
- İlk viewport’ta karar verdiren özet
- Detayların progressive disclosure ile açılması
- Landing ile uygulama içi ton arasında daha net bağ kurulması

## 8. Önceliklendirilmiş Yol Haritası

### P1 — Hızlı kazanımlar
- Mobil drop zone taşmasını düzelt
- Form `label`, `id`, `name` eksiklerini kapat
- Mobil filtre yoğunluğunu azalt
- Dashboard’da “aksiyon gereken” görünümü varsayılanlaştır

### P2 — Ürün deneyimi iyileştirmeleri
- Kurulum ekranını stepper mantığına yaklaştır
- Global nav ile plan içi nav’ı ayır
- Plan oluşturmayı tek ana akış altında topla

### P3 — Tasarım sistemi olgunlaştırma
- Kart yoğunluğunu azaltıp önem katmanları oluştur
- Mor ağırlıklı paleti daha dengeli hale getir
- Landing, auth ve app içi ekranlar arasında ton geçişini rafine et

## 9. Somut Uygulama Önerileri

1. `dashboard` için:
- “Bugün ilgilenmen gerekenler”
- “Yaklaşan vadeler”
- “Tüm taksitler”
şeklinde 3 seviyeli yapı kurun.

2. `setup` için:
- üstte checklist gösterin:
  - plan adı
  - ortaklar
  - teslimat ayı
  - ilk taksitler

3. `expenses` için:
- varsayılan filtreleri sadeleştirip “gelişmiş filtreler” drawer’ına taşıyın.

4. `spending-analysis` için:
- boş durumda örnek dosya ve demo rapor CTA’sı ekleyin.

5. `profile` için:
- daha kompakt, daha utility-first bir düzen tercih edin.

## 10. Teknik Gözlemler

Canlı testte kritik hata görülmedi:
- Landing console temiz
- Auth akışı çalıştı
- Plan üretimi çalıştı
- Örnek şablon seed akışları çalıştı

Gözlenen teknik notlar:
- Angular build sırasında template nullability kaynaklı uyarılar mevcut
- DevTools a11y/issues panelinde form alanı ilişkilendirme eksikleri görüldü

## 11. Sonuç

PayDefteri’nin frontend’i zayıf değil; aksine ürün kapsamı ve pratik değeri yüksek. Asıl ihtiyaç “yeniden yapmak”tan çok “önceliklendirmek, sadeleştirmek ve katmanlamak”. Ürün şu anda işlev açısından güçlü bir MVP+ seviyesinde; iyi bir bilgi mimarisi, erişilebilirlik düzeltmeleri ve mobil öncelikli yüzey sadeleştirmesi ile çok daha güven veren ve tekrar kullanımı yüksek bir deneyime dönüşebilir.

## 12. Appendix

İncelenen ana dosyalar:
- [styles.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/styles.scss)
- [app.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/app.component.html)
- [app.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/app.component.scss)
- [landing.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/landing/landing.component.html)
- [landing.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/landing/landing.component.scss)
- [login.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/auth/login/login.component.html)
- [login.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/auth/login/login.component.scss)
- [plan-list.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/plans/plan-list.component.html)
- [plan-list.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/plans/plan-list.component.scss)
- [setup.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/setup/setup.component.html)
- [setup.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/setup/setup.component.scss)
- [dashboard.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/dashboard/dashboard.component.html)
- [dashboard.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/dashboard/dashboard.component.scss)
- [expenses.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/expenses/expenses.component.html)
- [spending-analysis.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/spending-analysis/spending-analysis.component.html)
- [spending-analysis.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/spending-analysis/spending-analysis.component.scss)
- [profile.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/profile/profile.component.html)
- [profile.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/profile/profile.component.scss)
- [data.component.html](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/data/data.component.html)
- [data.component.scss](/Users/yusuf/Desktop/paydefteri/src/web/src/app/features/data/data.component.scss)
