# PayDefteri Mobil Uygulama PRD

**Durum:** Taslak  
**Tarih:** 8 Ağustos 2026  
**Platformlar:** iOS ve Android  
**İlgili teknik tasarım:** [MOBILE-TECHNICAL-DESIGN.md](./MOBILE-TECHNICAL-DESIGN.md)

**Teslim belgeleri:** [Mobil doküman indeksi](./mobile/README.md)

## 1. Ürün özeti

PayDefteri Mobil, mevcut web uygulamasındaki ortak gider ve taksit planı deneyimini iOS ve Android'e taşır. Kullanıcı aynı hesabı, planları ve verileri web ile mobil arasında eş zamanlı kullanır. Görsel dil, terminoloji ve temel ekran akışları korunur; yalnızca kamera, geri tuşu, güvenli alan, klavye ve dokunmatik kullanım gibi mobil gereksinimler için uyarlama yapılır.

## 2. Problem ve fırsat

Giderler çoğunlukla ödeme anında, telefondayken oluşur. Web uygulamasına sonradan veri girmek unutulmaya, fiş kaybına ve ortaklar arasında güncel olmayan bakiyelere neden olur. Mobil uygulama ile kullanıcı:

- Harcamayı gerçekleştiği anda ekleyebilir.
- Fişi kamera ile çekip formu otomatik doldurabilir.
- Ortak bakiyesini ve yaklaşan ödemeleri hızlıca görebilir.
- Davet bağlantısını doğrudan uygulamada açabilir.

## 3. Hedef kullanıcılar

- Eşi veya ev arkadaşıyla ortak gider yöneten kullanıcılar.
- Araç, ev, tatil veya benzeri masrafları taksitli takip eden kullanıcılar.
- Birden fazla planın ödeme ve alacak/borç durumunu yöneten plan sahipleri ve ortaklar.

## 4. Ürün hedefleri

1. Web'deki temel plan ve gider işlemlerinde işlevsel eşdeğerlik sağlamak.
2. Yeni gider ekleme süresini manuel girişte 30 saniyenin, fiş analizi sonrası onayda 20 saniyenin altında tutmak.
3. Web ve mobil arasında veri tutarsızlığı oluşturmamak.
4. Mobil oturumları cihaz üzerinde güvenli biçimde saklamak.
5. İlk sürümü TestFlight ve Google Play kapalı test kanallarına hazır hale getirmek.

## 5. MVP kapsamı

### 5.1 Hesap ve oturum

- Kayıt, giriş, çıkış ve oturum yenileme.
- Mevcut PayDefteri hesabı ve verileriyle çalışma.
- Profil görüntüleme ve desteklenen profil ayarları.
- Süresi dolan oturumun kullanıcıyı gereksiz yere giriş ekranına atmadan güvenli biçimde yenilenmesi.

### 5.2 Planlar

- Planları listeleme, oluşturma, görüntüleme, düzenleme ve arşivleme.
- Taksit planı ve ortak gider planı desteği.
- Plan özeti, katılımcılar, ödeme durumu ve borç/alacak bakiyesi.
- Plan daveti oluşturma, paylaşma ve davet bağlantısını uygulamada kabul etme.

### 5.3 Gider ve ödeme işlemleri

- Manuel gider ekleme, düzenleme ve silme.
- Peşin veya taksitli gider oluşturma; taksit sayısı ve ilk taksit tarihi seçimi.
- Eşit, yüzde, tek ortak ve özel paylaşım yöntemleri.
- Ödendi/planlandı durumu, ödeyen kişi ve ödeme dağılımı.
- Tekrarlayan giderleri ve transferleri görüntüleme/yönetme.
- Taksit planlarında ödeme kaydı ve fiş ekleme.

### 5.4 Kamera ve fiş analizi

- Kullanıcıya `Kamera`, `Fotoğraflar` ve `Vazgeç` seçeneklerini sunma.
- Görseli mevcut Gemini birincil, OpenAI yedekli sunucu analiziyle işleme.
- Analiz sonucunu hiçbir zaman otomatik kaydetmeme; tutar, tarih, açıklama ve kategori kullanıcı tarafından onaylanır.
- Kamera veya dosya seçimi iptal edildiğinde gider formunu açık tutma.

### 5.5 Mobil deneyim

- Mevcut PayDefteri renkleri, tipografi, kartlar, tablolar ve form dili korunur.
- Güvenli alanlar, ekran klavyesi, fiziksel geri hareketi ve en az 44×44 px dokunma hedefleri desteklenir.
- Yükleme, boş durum, hata ve bağlantı yok durumları her ana ekranda gösterilir.
- Son başarıyla alınan plan listesi ve özetler çevrimdışı görüntülenebilir.

## 6. MVP dışı kapsam

- Çevrimdışıyken gider/ödeme oluşturup daha sonra senkronize etme.
- Banka veya kredi kartı hesabından otomatik hareket çekme.
- Kullanıcı onayı olmadan AI sonucunu kaydetme.
- İlk sürümde push bildirimleri, biyometrik giriş ve tablet için özel yerleşim.
- Çoklu para birimi, toplu taksit düzenleme ve taksit grubunu tek işlemle silme.
- Yönetici veya `superpassword` ekranlarının mobil uygulamaya taşınması.

## 7. Temel kullanıcı akışları

### İlk kullanım

1. Kullanıcı uygulamayı açar ve giriş yapar veya hesap oluşturur.
2. Planı varsa plan listesine, yoksa plan oluşturma yönlendirmesine gider.
3. Uygulama oturumu güvenli cihaz deposunda saklar.

### Fişten gider ekleme

1. Kullanıcı ortak gider planında `Gider ekle` alanını açar.
2. `Fişten doldur` ve ardından kamera veya fotoğraf arşivini seçer.
3. Analiz sonucu forma yerleştirilir ve belirsiz alanlar işaretlenir.
4. Kullanıcı paylaşım ve ödeme bilgilerini kontrol ederek kaydeder.

### Davet kabul etme

1. Kullanıcı `https://paydefteri.com/invite/{token}` bağlantısına dokunur.
2. Uygulama yüklüyse ilgili davet ekranı açılır; değilse web akışı çalışır.
3. Giriş gerekiyorsa davet bilgisi korunarak giriş sonrasında işleme devam edilir.

## 8. İş kuralları ve hata davranışı

- Mobil uygulama backend iş kurallarını yeniden üretmez; API sonucunu esas alır.
- Para girişleri iki ondalık basamakla gösterilir, hesaplamalar sunucuda yapılır.
- Yazma işlemlerinde çift dokunma ve tekrarlanan istekler engellenir.
- Bağlantı yokken yazma aksiyonları devre dışı bırakılır ve açıklayıcı mesaj gösterilir.
- Yetki, doğrulama ve çakışma hataları genel mesaj yerine kullanıcıya eyleme dönük Türkçe metinle sunulur.
- Fiş görselleri analiz amacıyla gönderilir; mevcut politika gereği kalıcı olarak saklanmaz.

## 9. Başarı ölçütleri

- Crash-free oturum oranı en az `%99,5`.
- Başarılı giriş oranı en az `%98`.
- Ana ekranın sıcak açılış süresi P95'te 1 saniyenin, soğuk açılışı 2,5 saniyenin altında.
- Başlatılan gider ekleme akışlarının en az `%85`'inin başarıyla tamamlanması.
- Mobil kaynaklı yinelenen gider veya ödeme kaydı oluşmaması.
- Mağaza kapalı testinde kritik/seviye-1 hata bulunmaması.

## 10. Analitik ve izleme

Kişisel finansal değerler analitik sistemine gönderilmez. Yalnızca ekran görüntüleme, akış başlangıcı/tamamlanması, hata sınıfı, uygulama sürümü ve platform gibi teknik olaylar izlenir. Fiş görseli, erişim anahtarı, davet token'ı, açıklama ve tutar loglanmaz.

## 11. Yayın planı

1. Geliştirici cihazlarında iOS ve Android doğrulaması.
2. Ekip içi TestFlight ve Google Play Internal Testing.
3. Sınırlı kullanıcıyla kapalı beta ve geri bildirim turu.
4. Crash, giriş ve gider oluşturma metrikleri kabul seviyesindeyse mağaza yayını.

Yayın öncesinde gizlilik politikası, kullanım koşulları, hesap/veri silme yönlendirmesi, kamera ve fotoğraf izin açıklamaları ile mağaza ekran görüntüleri tamamlanmalıdır.

## 12. Varsayımlar ve açık kararlar

- Uygulama adı `PayDefteri`, paket kimliği `com.paydefteri.app` olacaktır.
- İlk sürüm Türkçe, online-first ve telefon odaklıdır.
- Minimum hedefler iOS 16 ve Android 10/API 29'dur.
- Üretim API adresi `https://paydefteri.com/api` olarak kalacaktır.
- Push bildirimleri ve biyometrik kilit, MVP geri bildirimlerine göre ikinci fazda değerlendirilecektir.
