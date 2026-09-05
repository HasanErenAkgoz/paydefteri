# PayDefteri Production Readiness Audit

Tarih: 2 Eylül 2026

## Durum (5 Eylül 2026 güncellemesi)

Bu rapor tarihsel kayıttır; aşağıdaki karar ve bulgular 2 Eylül tarihindeki koda aittir. O tarihten sonra P0 (`68bdbc2`), P1'ler (`dab0b76`, `4b17530`, `478b17a`, `ae6472b`) ve P2'ler (`0956de4`, `c8ed05b`, `5ea4a5b`) kapatıldı. Güncel durum için `.claude/memory.md` dosyasına bakın.

## Karar

**DO NOT SHIP / Genel kullanıma hazır değil.**

Uygulama derleniyor, otomatik testleri geçiyor ve giriş öncesi canlı sayfalar hızlı ve kararlı çalışıyor. Ancak bir kritik kimlik doğrulama açığı ile iki önemli dosya yükleme tutarsızlığı giderilmeden ürün prod-ready kabul edilmemeli. Oturum içi ekranlar için güvenli test hesabı ve görsel regresyon temeli bulunmadığından bu bölümün tarayıcı doğrulaması sonuçsuzdur.

## Tasarım Yönü

- Amaç: Ortak gider ve taksitleri sık kullanılan, hızlı taranan bir çalışma ekranında yönetmek.
- Kullanıcı: Aynı akışları mobil ve web üzerinde tekrar eden plan sahipleri ve ortaklar.
- Ton: Sakin, yoğun, faydacı ve güven veren.
- Temel ilke: Arama, ödeme ve kayıt ekleme gibi asıl işler ilk bakışta görünmeli; açıklama ve dekorasyon bunların önüne geçmemeli.
- Kısıtlar: Angular 21, Capacitor, WCAG 2.2 AA, 375/768/1440 px kırılımları ve mevcut tasarım tokenları.

## Bulgular

### P0 - Repoda sabit yönetici parolası ve tüm hesaplarda master giriş yetkisi var

`src/api/PayDefteri.Api/appsettings.json:57` altında izlenen yapılandırma dosyasında açık bir SuperAdmin parolası bulunuyor. `src/api/PayDefteri.Infrastructure/Identity/IdentityService.cs:66` bu parolayı herhangi bir e-posta hesabının gerçek parolası yerine kabul ediyor, `:76` giriş yapan kişiye SuperAdmin yetkisi veriyor ve `:140` aynı parola ile herhangi bir kullanıcının parolasını değiştirebiliyor.

Etki: Parolayı bilen veya Git geçmişinden alan biri, e-posta adresini bildiği herhangi bir hesaba girebilir, yönetici yetkisi kazanabilir ve hesabın parolasını değiştirebilir.

Çıkış kriteri: Parolayı hemen döndürün; izlenen yapılandırma ve Git geçmişinden kaldırın; master parola mekanizmasını tamamen silin; yalnız ortam değişkeniyle oluşturulan gerçek yönetici hesabı ve normal Identity doğrulaması kullanın; bu davranış için negatif güvenlik testleri ekleyin.

### P1 - Arayüz 15 MB kabul ederken üretim proxy'si 10 MB'da isteği reddediyor

`src/web/deploy/nginx.conf:7` gövdeyi 10 MB ile sınırlıyor. Buna karşılık `src/web/src/app/features/spending-analysis/spending-analysis.component.ts:236` ve `src/web/src/app/features/expenses/statement-import/statement-import-modal.component.ts:44` kullanıcıya 15 MB'a kadar izin veriyor. API tarafında da spending analysis için 16 MB, gider ekstresi için 15 MB sınırı tanımlı.

Etki: 10-15 MB arasındaki geçerli dosyalar arayüz doğrulamasını geçer, fakat Nginx tarafından API'ye ulaşmadan `413 Request Entity Too Large` ile kesilir. Uygulamanın standart hata mesajı da devreye girmeyebilir.

Çıkış kriteri: Proxy sınırını multipart ek yükünü karşılayacak şekilde API sınırının üzerine çıkarın veya tüm katmanları aynı daha düşük limite indirin. 9.9 MB, 10.1 MB ve üst sınır için gerçek proxy üzerinden entegrasyon testi ekleyin.

### P1 - Ekstre biçimleri iki ekranda farklı davranıyor

Gider planındaki aktarım `src/web/src/app/features/expenses/statement-import/statement-import-modal.component.html:14` üzerinden PDF, XLSX, CSV, JPEG, PNG ve WebP kabul ediyor. Harcama Analizi ise `src/web/src/app/features/spending-analysis/spending-analysis.component.html:10` ve `:36` ile yalnız CSV, XLSX ve PDF seçtiriyor; `spending-analysis.component.ts:230` görselleri ayrıca kod seviyesinde reddediyor.

Etki: Kullanıcı bir ekranda çalışan telefon ekran görüntüsünü veya fotoğraf ekstresini diğer ekranda yükleyemiyor. Aynı isimli “ekstre analizi” akışları farklı kabiliyet sunduğu için ürün bozuk algısı oluşuyor.

Çıkış kriteri: Ürün kararı verip iki giriş noktasını aynı biçim listesine getirin. Görsel desteklenecekse Spending Analysis validator, imza kontrolü, metinler ve testler birlikte güncellenmeli; desteklenmeyecekse diğer ekrandaki ayrım açıkça anlatılmalı.

### P1 - Oturum içi kritik akışların gerçek tarayıcı regresyon güvencesi yok

Frontend'de 26 feature component bulunmasına rağmen yalnız 9 spec dosyasında toplam 18 test var. Playwright/Cypress benzeri bir E2E paketi, güvenli staging hesabı ve 375/768/1440 px görsel baseline bulunmuyor. Bu nedenle giriş, plan oluşturma, gider ekleme, kamera/ekstre aktarımı, ödeme, silme, profil ve mobil alt menü bir release gate içinde uçtan uca doğrulanmıyor.

Etki: Arama/filtre konumu ve profil kayması gibi entegrasyon kaynaklı hatalar birim testleri ve derleme başarılı olduğu halde üretime çıkabiliyor.

Çıkış kriteri: İzole staging ortamı ve seed edilmiş test hesabı oluşturun. En az giriş, plan açma, gider listeleme/arama, gider ekleme, ekstre önizleme, ödeme işaretleme ve profil akışlarını üç kırılımda otomatikleştirin. Görsel baseline yoksa sonuç “başarılı” değil “inconclusive” sayılmalı.

### P1 - Modallar focus trap, ilk odak, Escape ve odak geri yükleme sağlamıyor

Örnekler: `src/web/src/app/features/expenses/expenses.component.html:128`, `src/web/src/app/features/dashboard/dashboard.component.html:580`, `:665`, `:701`, `src/web/src/app/features/setup/setup.component.html:919` ve `:1130`. Dialog rolleri yer yer mevcut olsa da bileşenlerde ilk odağa taşıma, Tab döngüsü, Escape ile kapatma ve kapandıktan sonra tetikleyiciye odak döndürme kodu yok.

Etki: Klavye ve ekran okuyucu kullanıcıları modalın arkasına geçebilir veya kapattıktan sonra sayfadaki konumunu kaybedebilir. Uzun mobil formlarda bu davranış kullanım hatasına dönüşür.

Çıkış kriteri: Ortak erişilebilir modal/dialog altyapısı oluşturun; tüm modalları buna taşıyın ve klavye testleri ekleyin.

### P2 - Tab bileşenlerinin çoğu yalnız görsel olarak tab

Planlar, giderler ve veri ekranındaki düğmeler `role="tab"` ve `aria-selected` kullanıyor; ancak `aria-controls`, ilişkili `tabpanel`, roving `tabindex` ve ok tuşu yönetimi yok. Doğru örnek yalnız Profil ekranında bulunuyor (`src/web/src/app/features/profile/profile.component.html:41`).

Etki: Ekran okuyucu ve klavye kullanıcıları tab ilişkisini ve beklenen ok tuşu davranışını alamıyor.

Çıkış kriteri: Profil tab desenini ortak bileşene çıkarın ve diğer sekmeli ekranlarda kullanın.

### P2 - Giriş ekranında bazı mobil dokunma hedefleri 44 px'in altında

Canlı 375 px ölçümünde şifre göster düğmesi 36x36 px, “Şifremi unuttum?” kontrolü yaklaşık 116x17 px ve alt kayıt bağlantısı yaklaşık 52x18 px çıktı. Şifre düğmesinin 36 px boyutu `src/web/src/app/features/auth/login/login.component.scss:403`, bağlantının sıfır padding'i `:471` altında tanımlı.

Etki: Özellikle tek elle kullanımda yanlış dokunma ve erişilebilirlik sorunu oluşur.

Çıkış kriteri: Görsel metin boyutunu büyütmeden interaktif kutuları en az 44x44 CSS px yapın; kayıt ekranındaki aynı şifre kontrolünü de düzeltin.

### P2 - Korunan URL ilk yüklemede landing içeriğini indirip sonra girişe yönleniyor

Canlı `/plans` isteğinde önce landing chunk'ı ve landing görselleri yüklendi, ardından `/api/auth/me` 401 döndü ve login chunk'ı indirildi. Nginx SPA fallback'i `src/web/deploy/nginx.conf:61` tüm bilinmeyen yollar için kök `index.html` döndürüyor; bu dosya landing prerender içeriğini taşıyor.

Etki: Doğrudan korunan URL açılışında gereksiz veri/işlem maliyeti ve kısa süreli yanlış içerik parlaması riski var.

Çıkış kriteri: Auth durumu çözülene kadar nötr uygulama kabuğu kullanın veya korunan yollar için landing prerender içermeyen shell servis edin.

### P2 - Build temiz değil ve büyük ekranlar regresyon riskini artırıyor

Web ve mobil build başarılı, fakat dört Angular template uyarısı ile iki component style budget uyarısı üretiyor. Landing ve Data stilleri 16 kB uyarı sınırını aşıyor. Ayrıca `setup.component.html` 1.213 satır, `data.component.scss` 1.289 satır ve `setup.component.scss` 1.038 satır; bu yoğunluk breakpoint çakışmalarını ve istemeden global görsel değişiklikleri kolaylaştırıyor.

Çıkış kriteri: CI'da uyarısız build hedefleyin; büyük şablonları görev bazlı alt bileşenlere ayırın; style budget aşımını gerekçeli bir bütçe değişikliği veya gerçek küçültmeyle kapatın.

## Başarılı Kontroller

- Frontend testleri: 18/18 başarılı.
- Domain testleri: 46/46 başarılı.
- API testleri: 94/94 başarılı.
- Angular production build: başarılı, uyarılı.
- Angular mobile build: başarılı, uyarılı.
- Canlı landing: 375, 768 ve 1440 px'de doküman yatay taşması yok.
- Canlı landing: kritik konsol hatası ve başarısız ağ isteği yok.
- Mobil Lighthouse landing: Accessibility 100, Best Practices 100, SEO 100.
- Fast 4G + 4x CPU laboratuvar ölçümü: LCP 617 ms, CLS 0.00.
- `/health`: dağıtım öncesi son kontrolde sağlıklı.

## Sınırlar

- Üretimde veri değiştiren akış çalıştırılmadı.
- Güvenli staging hesabı olmadığı için oturum içi canlı görsel test yapılmadı.
- Görsel regresyon baseline'ı olmadığı için iç ekranların görsel sonucu **INCONCLUSIVE**.
- Lighthouse otomasyonu erişilebilirliğin tamamını kapsamaz; modal/focus ve klavye bulguları statik incelemeyle ayrıca doğrulandı.

## Önerilen Sıra

1. Master parola açığını kapatın ve parolayı döndürün.
2. Dosya boyutu sınırlarını proxy, API ve frontend arasında eşitleyin.
3. Ekstre biçimi kararını iki analiz ekranında birleştirin.
4. Staging test hesabı ve kritik E2E/görsel regresyon setini kurun.
5. Ortak erişilebilir modal ve tab bileşenlerini devreye alın.
6. Mobil dokunma hedeflerini ve korunan rota açılışını düzeltin.
7. Build uyarılarını temizleyip aynı denetimi yeniden çalıştırın.

Bu yedi madde kapandıktan ve oturum içi üç kırılım tarayıcı testi geçtiğinde karar `SHIP` seviyesine yükseltilebilir.
