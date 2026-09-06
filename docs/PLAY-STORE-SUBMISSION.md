# Play Store Gönderim Bilgileri

Bu belge, PayDefteri'nin Google Play'e gönderilmesi için gereken teknik bilgileri ve Data Safety
formu cevaplarını koddan türetilmiş haliyle toplar. Süreç adımları (kanallar, yayın, rollback)
[`mobile/RELEASE-RUNBOOK.md`](mobile/RELEASE-RUNBOOK.md) içindedir. Değerler değişirse burayı da
güncelleyin.

## 1. Uygulama kimliği

| Alan | Değer | Kaynak |
| --- | --- | --- |
| Paket adı | `com.paydefteri.app` | `src/web/android/app/build.gradle` |
| Sürüm | `versionCode 1`, `versionName 1.0.0` | `src/web/android/app/build.gradle` |
| Hedef / minimum API | `targetSdk 36`, `minSdk 29` | `src/web/android/variables.gradle` |
| İzinler | `INTERNET`, `CAMERA` | `AndroidManifest.xml` |
| Derin bağlantı | `https://paydefteri.com/invite/*` (`autoVerify`) | `AndroidManifest.xml` |

Kamera izni yalnızca fiş/fatura fotoğrafı çekip gider formunu otomatik doldurmak için kullanılır.
Konum, kişiler, rehber, SMS ve arka plan izni istenmez.

## 2. İmzalama

Upload key yerel makinede üretildi; keystore ve parolası repoya girmez.

- Keystore: `~/paydefteri-upload.jks` (PKCS12, RSA 4096, 2054'e kadar geçerli)
- Kimlik bilgileri: `~/.paydefteri-upload-keystore.env` (mod 600)
- Sertifika sahibi: `CN=PayDefteri, OU=Mobile, O=PayDefteri, L=Istanbul, C=TR`
- Sertifika SHA-256: `EB:24:94:2B:7A:18:F9:E6:33:E7:DE:09:6B:C1:34:10:D0:BA:A6:DD:B1:69:95:BA:6B:B3:F3:C1:8D:A4:10:FC`

**Bu iki dosyayı yedekleyin.** Kaybedilirse uygulama bir daha güncellenemez; kurtarma yalnızca Play
App Signing üzerinden upload key sıfırlama talebiyle mümkündür.

İmzalı paket üretimi:

```bash
set -a && . ~/.paydefteri-upload-keystore.env && set +a
cd src/web && npm run build:mobile && npx cap sync android
cd android && JAVA_HOME=<jdk-21-yolu> ./gradlew bundleRelease
# çıktı: app/build/outputs/bundle/release/app-release.aab
jarsigner -verify app/build/outputs/bundle/release/app-release.aab   # "jar verified."
```

Ortam değişkenleri tanımlı değilse build imzasız `.aab` üretir ve uyarı yazar; Play imzasız paketi
kabul etmez.

Derin bağlantı doğrulaması için `https://paydefteri.com/.well-known/assetlinks.json` yayınlanmalı ve
içine Play Console'un ürettiği **app signing key** SHA-256'sı yazılmalıdır (upload key değil). Doğru
parmak izi Console → Setup → App integrity ekranındadır.

## 3. Data Safety formu cevapları

Aktarımda şifreleme: **evet** — istemci yalnızca HTTPS konuşur (`cleartext: false`, manifest'te
`usesCleartextTraffic` yok, iOS'ta ATS istisnası yok) ve sunucu TLS arkasındadır.

Kullanıcı verisi silme talebi: **evet** — uygulama içinden (Profil → Şifre & Güvenlik → Hesabı Sil)
ve web üzerinden `https://paydefteri.com/privacy` adresindeki yönergeyle.

| Veri türü | Toplanır | Paylaşılır | Zorunlu | Amaç |
| --- | --- | --- | --- | --- |
| E-posta adresi | Evet | Hayır | Zorunlu | Hesap yönetimi, giriş, plan daveti |
| Ad (görünen ad) | Evet | Hayır | Zorunlu | Hesap yönetimi, planlarda kimlik |
| Finansal bilgi — plan/taksit/gider tutarları | Evet | Hayır | Zorunlu | Uygulamanın temel işlevi |
| Finansal bilgi — IBAN | Evet | Hayır | İsteğe bağlı | Mahsuplaşmada ödeme yönlendirme |
| Fotoğraf — dekont/fiş görselleri | Evet | Hayır | İsteğe bağlı | Dekont kaydı ve fiş analizi |
| Uygulama etkinliği — ekstre analizi kayıtları | Evet | Hayır | İsteğe bağlı | Harcama analizi |
| Cihaz bilgisi — cihaz adı, platform, uygulama sürümü | Evet | Hayır | Zorunlu | Oturum yönetimi ve güvenlik |

Konum, kişiler, sağlık, mesaj, arama geçmişi, reklam kimliği ve çerez tabanlı izleme
**toplanmıyor**. Uygulama içi reklam ve üçüncü taraf analitik SDK'sı yok.

Servis sağlayıcılar (Play terminolojisinde "paylaşım" değil, işleme amacıyla aktarım):

- **Google Gemini** — fiş ve ekstre ayrıştırma. Belge/görsel gönderilir; istek `store: false` ile
  yapılır ve hassas alanlar `SensitiveFinancialDataRedactor` ile maskelenir
  (`Infrastructure/Services/GeminiExpenseReceiptAnalyzer.cs`, `GeminiCreditCardStatementAnalyzer.cs`).
- **OpenAI** — yalnızca fiş analizinde Gemini başarısız olursa yedek yol
  (`Infrastructure/Services/OpenAiExpenseReceiptAnalyzer.cs`).
- **Resend** — işlemsel e-posta (doğrulama, davet, hatırlatma).
- **Cloudflare** — alan adı önünde CDN ve TLS.

## 4. Mağaza listesi için gerekenler

- Gizlilik politikası URL'si: `https://paydefteri.com/privacy`
- Kategori: Finans; hedef kitle 18+, çocuklara yönelik değil
- İçerik derecelendirme anketi: finans uygulaması, kullanıcı içeriği paylaşımı yok, reklam yok
- Görseller: ikon 512×512, özellik grafiği 1024×500, en az 2 telefon ekran görüntüsü
- Kısa ve uzun açıklama (Türkçe); pazarlama metinleri `marketing/` altında

## 5. Gönderim öncesi son kontroller

- [ ] `assetlinks.json` yayında ve Play'in app signing SHA-256'sını içeriyor
- [ ] `versionCode` bir önceki yüklemeden büyük
- [ ] İmzalı `.aab` `jarsigner -verify` ile doğrulandı
- [ ] Üretim API'si HTTPS üzerinden yanıt veriyor (`https://paydefteri.com/health`)
- [ ] Play inceleme ekibine test hesabı verildi (uygulama giriş gerektiriyor)
- [ ] Hesap silme akışı gerçek bir hesapla denendi
