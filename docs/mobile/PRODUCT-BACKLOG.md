# Mobil Ürün Backlog'u

## Öncelik modeli

- **P0:** MVP yayınını doğrudan mümkün kılan veya güvenlik için zorunlu iş.
- **P1:** MVP kalite ve kullanılabilirliği için gerekli iş.
- **P2:** MVP sonrası değer; ilk release'i bloke etmez.

Bir story geliştirmeye alınmadan [Definition of Ready](./ACCEPTANCE-CHECKLIST.md#definition-of-ready-dor) sağlanır.

## Epic M0 — Teknik temel

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-001 | Geliştirici olarak Angular uygulamasını iOS simulator ve Android emulator üzerinde çalıştırabilmek istiyorum. | P0 | — |
| MOB-002 | Kullanıcı olarak çentik, klavye ve geri tuşu nedeniyle aksiyonların kaybolmamasını istiyorum. | P0 | MOB-001 |
| MOB-003 | Ekip olarak web ve native davranışını platform portlarıyla ayırmak istiyoruz. | P0 | MOB-001 |
| MOB-004 | Kullanıcı olarak native açılışta landing yerine doğru oturum ekranına yönlendirilmek istiyorum. | P1 | MOB-001 |

## Epic M1 — Hesap ve güvenli oturum

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-101 | Kullanıcı olarak mevcut PayDefteri hesabımla mobilde giriş yapmak istiyorum. | P0 | MOB-003 |
| MOB-102 | Kullanıcı olarak 30 dakikada bir yeniden giriş yapmadan güvenli biçimde oturumumu sürdürmek istiyorum. | P0 | MOB-101 |
| MOB-103 | Kullanıcı olarak çıkış yaptığım cihazın oturumunu iptal etmek istiyorum. | P0 | MOB-102 |
| MOB-104 | Kullanıcı olarak diğer cihaz oturumlarımı görüp iptal etmek istiyorum. | P1 | MOB-102 |
| MOB-105 | Kullanıcı olarak parola değiştiğinde eski cihaz oturumlarımın kapanmasını istiyorum. | P0 | MOB-102 |

## Epic M2 — Plan ve gider eşdeğerliği

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-201 | Kullanıcı olarak planlarımı listelemek ve doğru plan ekranını açmak istiyorum. | P0 | MOB-101 |
| MOB-202 | Plan sahibi olarak taksit veya ortak gider planı oluşturmak/düzenlemek istiyorum. | P0 | MOB-201 |
| MOB-203 | Kullanıcı olarak manuel peşin/taksitli gider eklemek ve paylaşımı seçmek istiyorum. | P0 | MOB-201 |
| MOB-204 | Yetkili kullanıcı olarak gideri düzenlemek, silmek ve ödeme durumunu yönetmek istiyorum. | P0 | MOB-203 |
| MOB-205 | Kullanıcı olarak borç/alacak, transfer ve tekrar eden giderleri görüntülemek istiyorum. | P1 | MOB-201 |
| MOB-206 | Kullanıcı olarak rapor/yedek dosyasını native paylaşım menüsüyle paylaşmak istiyorum. | P1 | MOB-003 |

## Epic M3 — Kamera ve AI fiş analizi

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-301 | Kullanıcı olarak fişi kamera ile çekmek veya fotoğraflardan seçmek istiyorum. | P0 | MOB-003 |
| MOB-302 | Kullanıcı olarak seçimi iptal ettiğimde gider formumun açık ve verilerimin korunmuş kalmasını istiyorum. | P0 | MOB-301 |
| MOB-303 | Kullanıcı olarak AI sonucunu formda taslak görüp kontrol ederek kaydetmek istiyorum. | P0 | MOB-301 |
| MOB-304 | Kullanıcı olarak analiz servisi çalışmadığında manuel girişe devam etmek istiyorum. | P0 | MOB-303 |
| MOB-305 | Kullanıcı olarak taksit ödemesine fiş görseli eklemek istiyorum. | P1 | MOB-301 |

## Epic M4 — Davet ve deep link

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-401 | Davet alan kullanıcı olarak web bağlantısından doğrudan mobil davet ekranını açmak istiyorum. | P0 | MOB-001 |
| MOB-402 | Oturumum yoksa giriş/kayıt sonrası aynı davete geri dönmek istiyorum. | P0 | MOB-401, MOB-101 |
| MOB-403 | Plan sahibi olarak davet bağlantısını native paylaşım menüsüyle göndermek istiyorum. | P1 | MOB-401 |
| MOB-404 | Uygulama kurulu değilse daveti web üzerinden tamamlamak istiyorum. | P0 | MOB-401 |

## Epic M5 — Dayanıklılık ve kalite

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-501 | Kullanıcı olarak bağlantı yokken son plan ve gider özetimi okuyabilmek istiyorum. | P1 | MOB-201 |
| MOB-502 | Kullanıcı olarak çevrimdışıyken veri değiştiremeyeceğimi açıkça görmek istiyorum. | P0 | MOB-501 |
| MOB-503 | Ekip olarak crash ve ana akış hata oranlarını finansal veri toplamadan izlemek istiyoruz. | P0 | MOB-001 |
| MOB-504 | Ekip olarak desteklenmeyen sürümleri güvenli güncellemeye yönlendirmek istiyoruz. | P1 | MOB-001 |

## Epic M6 — Yayın

| ID | Story | Öncelik | Bağımlılık |
|---|---|---|---|
| MOB-601 | Ekip olarak imzalı iOS/Android beta build'lerini tekrarlanabilir şekilde üretmek istiyoruz. | P0 | M0–M5 |
| MOB-602 | Kullanıcı olarak kamera/fotoğraf izinlerinin neden istendiğini açıkça görmek istiyorum. | P0 | MOB-301 |
| MOB-603 | Kullanıcı olarak gizlilik ve hesap/veri silme bilgilerine erişmek istiyorum. | P0 | MOB-101 |

## MVP sonrası backlog

- **MOB-701 (P2):** Push ödeme/vade hatırlatmaları.
- **MOB-702 (P2):** Biyometrik uygulama kilidi.
- **MOB-703 (P2):** Tablet için özel bilgi mimarisi.
- **MOB-704 (P2):** Güvenli çevrimdışı yazma ve senkronizasyon.
- **MOB-705 (P2):** Çoklu para birimi.

