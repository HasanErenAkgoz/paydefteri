# Mobil Test Stratejisi

## Hedef

Web davranışını bozmadan iOS/Android kritik kullanıcı sonuçlarını, auth güvenliğini, finansal veri doğruluğunu ve native cihaz davranışlarını risk bazlı doğrulamak.

## Katmanlar

| Katman | Kapsam | Araç/konum |
|---|---|---|
| Domain unit | Pay, taksit, bakiye ve recurrence kuralları | xUnit, Domain.Tests |
| API integration | Mobil auth, yetki, idempotency, receipt fallback | xUnit + test DB, Api.Tests |
| Angular unit | Port/adaptör, interceptor, form ve route davranışı | Jasmine/Karma |
| Native integration | Kamera, secure storage, deep link, back/keyboard | XCUITest/Espresso |
| E2E | Kritik kullanıcı sonuçları | Gerçek/simüle cihaz CI + manuel smoke |
| Exploratory | UX, izin, ağ, düşük bellek ve cihaz farklılığı | QA oturumu |

## Zorunlu otomatik senaryolar

### Auth

- Geçerli/geçersiz login ve rate limit.
- Access token expiry sonrası tek refresh ve orijinal isteğin bir kez tekrarı.
- Eşzamanlı isteklerde yalnız bir refresh çağrısı.
- Refresh rotation, expiry, revoke ve replay family iptali.
- Logout ve parola değişiminden sonra session kullanılamaması.
- Web cookie/XSRF login, write request ve logout regresyonu.

### Yetki ve finansal doğruluk

- Kullanıcı yalnız üyesi olduğu planı görür/değiştirir.
- Plan sahibi/ortak izin matrisi web ile aynıdır.
- Peşin ve 2–120 taksitli gider toplamı girilen tutara eşittir.
- Özel pay, yuvarlama ve ödeme dağılımı korunur.
- Ağ timeout/retry mükerrer gider, ödeme veya transfer oluşturmaz.

### Kamera ve AI

- JPEG/PNG/WebP, geçersiz MIME, bozuk magic-byte, 0 byte ve 8 MB sınırı.
- Kamera/galeri iptalinde form ve mevcut alanlar korunur.
- Başarılı analiz taslak olarak forma uygulanır.
- Düşük güvenli alanlar işaretlenir; otomatik kayıt yapılmaz.
- Gemini teknik hatası, OpenAI fallback ve iki sağlayıcı hatasında güvenli mesaj.
- Fiş/token/sağlayıcı ham yanıtının loglanmadığı doğrulanır.

### Deep link

- Kurulu uygulamada geçerli davet route'u.
- Oturumsuz giriş/kayıt sonrası davete dönüş.
- Geçersiz, süresi dolmuş ve kullanılmış token.
- Uygulama kurulu değilken web fallback.
- Token'ın analytics/crash/log içinde bulunmaması.

### Çevrimdışı ve yaşam döngüsü

- Cache gösterimi, cache zamanı ve offline bandı.
- Offline yazma aksiyonlarının kapalı olması.
- Arka plan/ön plan dönüşünde oturum ve verinin yenilenmesi.
- Process kill sonrası refresh session ile güvenli devam.
- Hesap değişiminde önceki kullanıcının cache'inin görünmemesi.

## Cihaz matrisi

| Platform | Minimum | Temsilci |
|---|---|---|
| iOS | iOS 16, küçük ekran | En güncel iOS, büyük ekran |
| Android | API 29, orta seviye cihaz | Güncel API, farklı üretici |

Her release gerçek bir iPhone ve gerçek bir Android cihazda smoke test edilir. Kamera, izin ve deep link yalnız simulator kanıtıyla kabul edilmez.

## Performans hedefleri

- Crash-free session: en az `%99,5`.
- Sıcak açılış P95: `<1 sn`; soğuk açılış P95: `<2,5 sn` (desteklenen temsilci cihazda).
- Ana API akışlarında mobil kaynaklı hata oranı: `<%2`.
- UI ana thread üzerinde gözle görülür uzun bloklama olmamalı.
- 8 MB görsel işleme cihazda bellek taşmasına yol açmamalı.

Baz ölçüm ve cihaz modeli test raporunda kaydedilir; ağ gecikmesi ayrıca raporlanır.

## Test verisi

- Production verisi kullanılmaz.
- Sentetik kullanıcı, ortak, gider ve anonimleştirilmiş/sahte fiş fixture'ları sürümlenir.
- Test anahtarları/hesapları yalnız test ortamına aittir ve CI secret'ta tutulur.
- E2E testleri kendi verisini oluşturur ve temizler.

## CI kalite kapısı

1. .NET restore/build/test (`Release`, warning as error).
2. Angular `npm ci`, unit test ve production build.
3. Android debug build ve iOS simulator build.
4. Auth/finans/deep-link kritik E2E.
5. Secret/dependency/container taraması.

Flaky kritik test release'i durdurur. Coverage yüzdesi tek başına kabul değildir; kritik davranış ve risk kapsaması kanıtlanır.

## Release test raporu

QA raporu sürüm/build, ortam, cihazlar, geçen/kalan testler, bilinen hatalar, performans ölçümü, güvenlik sonucu ve `GO / CONDITIONAL GO / NO-GO` önerisini içerir. P0/P1 hata veya veri doğruluğu riski `NO-GO`'dur.

