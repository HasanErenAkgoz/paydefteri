# Mobil UX ve Tasarım Handoff'u

## Tasarım ilkesi

Mobil uygulama yeni bir görsel ürün değildir. PayDefteri'nin mevcut renkleri, tipografisi, kartları, form dili ve durum mesajları korunur. Native uyarlamalar yalnız mobil kullanım ergonomisi, erişilebilirlik ve işletim sistemi davranışları içindir.

## Bilgi mimarisi

- Oturum yok: `Giriş` → `Kayıt` → gerekirse `Davet`.
- Oturum var: `Planlar` → seçilen planın `Dashboard` veya `Giderler` ekranı.
- Plan içi ikincil alanlar: `Kurulum`, `Yedek ve Rapor`.
- Global alan: `Profil` ve `Çıkış`.

Mevcut route'lar korunur: `/login`, `/register`, `/invite/:token`, `/plans`, `/plans/:id/dashboard`, `/plans/:id/expenses`, `/plans/:id/setup`, `/plans/:id/data`, `/profile`.

## Ekran davranışları

### Açılış

1. Splash yalnız uygulama başlatılırken gösterilir; yapay bekleme eklenmez.
2. Oturum yenileme denenirken nötr yükleme ekranı gösterilir.
3. Oturum geçerliyse son kullanılan plan veya plan listesi açılır.
4. Oturum yoksa giriş ekranı açılır; native uygulamada pazarlama landing'i gösterilmez.

### Planlar ve plan içi gezinme

- Kart içeriği web ile aynı sırada kalır.
- Ana CTA ekranın güvenli alt alanının üzerinde ve tek elle erişilebilir konumda olmalıdır.
- Tablo içerikleri küçük ekranda mevcut kart görünümüne dönüşür; yatay kaydırma kritik bilgi için kullanılmaz.
- Navigasyonda web başlığı korunabilir; platformun fiziksel/gesture geri davranışı route geçmişiyle uyumlu çalışır.

### Gider ekleme

- Form varsayılan `Peşin`; `Taksitli` seçilince taksit sayısı/önizleme görünür.
- Ortak gider planında `Gider ekle` alanı varsayılan açık gelir.
- `Kamera ile çek`, `Fotoğraf seç` ve manuel giriş aynı formu kullanır.
- Medya seçimi iptal edilirse form kapanmaz ve girilmiş alanlar sıfırlanmaz.
- AI sonucu kaydedilmez; alanlara taslak olarak uygulanır, düşük güvenli alanlar görünür biçimde işaretlenir.
- Analiz hatasında kullanıcıya `Manuel devam et` yolu bırakılır.

### Davet

- Deep link ile açıldığında plan/davet özeti token değeri gösterilmeden sunulur.
- Oturum yoksa `Giriş yap` ve `Kayıt ol` sonrasında davete otomatik dönülür.
- Süresi dolmuş/kullanılmış/geçersiz davetler farklı, eyleme dönük mesajlara sahiptir.

## Durum matrisi

Her veri ekranı aşağıdaki durumları tasarlar:

| Durum | Beklenen UX |
|---|---|
| İlk yükleme | Skeleton veya odaklı progress; boş ekran yok |
| Yenileme | Mevcut içerik korunur, küçük progress gösterilir |
| Boş | Neden ve birincil CTA gösterilir |
| Doğrulama hatası | Alan yanında mesaj ve ilk hataya odak |
| API hatası | Genel olmayan, güvenli ve eyleme dönük mesaj |
| Çevrimdışı | Sabit bant, cache zamanı, yazma aksiyonları disabled |
| Yetkisiz | Girişe yönlendirme; devam edilecek route güvenli saklanır |
| İzin reddi | Gerekçe ve gerekiyorsa sistem ayarlarına gitme aksiyonu |

## Mobil etkileşim kuralları

- Minimum dokunma hedefi 44×44 px.
- Hover'a bağlı bilgi veya aksiyon kullanılmaz.
- Birincil aksiyon işlem sürerken disabled olur; çift kayıt engellenir.
- Klavye aktif alanı ve submit aksiyonunu örtmez.
- Android geri tuşu sırasıyla açık modal/dropdown'ı, sonra route'u kapatır; ana ekranda ikinci basışla çıkış yapılır.
- Safe-area için `env(safe-area-inset-top|right|bottom|left)` kullanılır.
- Para ve tarih girişlerinde uygun mobil klavye türü kullanılır.

## Erişilebilirlik

- Metin ve anlamlı ikonlar erişilebilir isim taşır.
- Sadece renk ile durum aktarılmaz; metin/ikon eşlik eder.
- Sistem font büyütmesi ana akışları kırmaz.
- Ekran okuyucu sırası görsel sırayla uyumludur.
- Odak, modal açılış/kapanış ve hata sonrası kontrollü taşınır.
- Kontrast en az WCAG AA hedefini karşılar.
- Reduced motion tercihinde animasyonlar azaltılır.

## İçerik standardı

- Butonlar kısa fiil kullanır: `Kaydet`, `Davet et`, `Tekrar dene`.
- Teknik sağlayıcı adı kullanıcı hatasında gösterilmez.
- Başarı mesajı yapılan işlemi doğrular; hata mesajı güvenli bir sonraki adım verir.
- `Fiş analiz servisine ulaşılamadı. Manuel girişe devam edebilirsiniz.` tercih edilen fallback mesajıdır.

## Tasarım QA kabulü

- iPhone küçük/büyük ekran ve en az bir orta seviye Android cihaz doğrulanır.
- Light/dark tema varsa iki tema da kontrol edilir.
- Klavye, safe-area, orientation lock, izin reddi ve uzun Türkçe metin senaryoları geçer.
- Ekran görüntüsü farkları mevcut web tasarımından bilinçli sapma olarak belgelenir.

