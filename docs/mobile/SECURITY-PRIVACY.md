# Mobil Güvenlik ve Gizlilik

## Güvenlik hedefi

Mobil uygulama finansal plan, gider, kişi ve fiş bilgisi işler. Temel hedefler hesap ele geçirme, yetki aşımı, token/PII sızıntısı, deep link kötüye kullanımı, mükerrer finansal işlem ve tedarik zinciri riskini azaltmaktır.

## Veri sınıflandırması

| Veri | Sınıf | Saklama |
|---|---|---|
| E-posta, görünen ad | PII | Sunucu; hesap yaşam döngüsü |
| Plan, gider, ödeme, bakiye | Hassas finansal veri | Sunucu; ürün retention politikası |
| Fiş/fatura görseli | Hassas finansal/PII | AI analizinde geçici; kalıcı saklama yalnız açık ürün özelliğinde |
| Access token | Gizli credential | Yalnız uygulama belleği, 30 dk |
| Refresh token | Gizli credential | Keychain/Keystore; sunucuda hash, 30 gün |
| Davet token'ı | Gizli capability | Kısa süreli işlem durumu; log/analytics yok |
| Teknik telemetri | Dahili | Minimize ve süreli |

Kesin retention süreleri production öncesi veri sahibi ve hukuk/uyumluluk sorumlusuyla onaylanmalıdır; bu belge hukuki tavsiye değildir.

## Tehdit modeli

| Tehdit | Kontrol | Doğrulama |
|---|---|---|
| Çalınan refresh token | Secure storage, rotation, hash, replay family revoke | Integration + cihaz testi |
| Başka plana erişim | Her endpoint'te sunucu yetkilendirmesi | Negative API testleri |
| Deep link token sızıntısı | Log/analytics redaksiyonu, HTTPS, tek kullanımlı davet | Log ve E2E testi |
| MITM | Yalnız HTTPS, platform trust store, ATS/network security policy | Proxy/cihaz testi |
| Kötü amaçlı dosya | MIME + magic-byte + boyut doğrulaması; image decode sınırı | Validator/fuzz testleri |
| AI sağlayıcısına fazla veri | Yalnız kullanıcı seçimi, minimizasyon, `store=false`, server-side key | Kod/config inceleme |
| Mükerrer ödeme/gider | UI lock + idempotency key + transaction | Ağ kesinti testi |
| Secret'ın uygulamaya gömülmesi | Secret yalnız server/CI vault | Artefact secret taraması |
| Zararlı dependency/build | Lockfile, dependency/container tarama, korunan signing | CI kanıtı |
| Hassas log/crash | Merkezi redaksiyon ve allowlist telemetry | Otomatik log testi |

## Kimlik ve oturum kontrolleri

- Web cookie/XSRF ve mobil Bearer akışları ayrıdır.
- Access token kalıcı storage, URL, analytics veya crash context'e yazılmaz.
- Refresh token native secure storage dışında tutulmaz; biometric flag güvenlik sınırı sayılmaz.
- Refresh rotation atomik transaction içinde yapılır.
- Logout idempotent; parola değişimi tüm cihaz session'larını iptal eder.
- Auth ve receipt endpoint'leri IP/kullanıcı temelli rate limit uygular.
- Sunucu saati ve token expiry toleransı izlenir; clock skew düşük tutulur.

## Uygulama ve platform kontrolleri

- iOS Keychain ve Android Keystore-backed şifreli storage kullanılır.
- Android backup'tan credential/cache hariç tutulur; iOS keychain accessibility uygun seviyede seçilir.
- Screenshot engelleme varsayılan değildir; hassas ekran ihtiyacı ayrıca ürün kararıdır.
- Root/jailbreak tespiti bilgi sinyali olabilir, tek başına erişim engeli değildir.
- Certificate pinning MVP'de zorunlu değildir; operasyonel rotasyon maliyeti nedeniyle threat model değişirse ADR ile değerlendirilir.
- Kamera/fotoğraf izni yalnız kullanıcı aksiyonunda ve amaç metniyle istenir.

## AI ve fiş gizliliği

- Gemini/OpenAI API anahtarları istemciye verilmez.
- Görsel yalnız analiz isteği sırasında API'ye gönderilir; sağlayıcıya `store=false` iletilir.
- Ham görsel, base64, sağlayıcı response'u veya prompt loglanmaz.
- AI sonucu taslaktır ve kullanıcı onayı olmadan finansal kayıt oluşturmaz.
- Kullanıcı gizlilik metninde üçüncü taraf AI işleyişi ve veri amacı açıkça anlatılır.

## CI/CD güvenlik kapısı

- Secret scan, dependency audit, .NET/NPM vulnerability scan ve container image scan.
- Release build'de debug endpoint, development config veya source-map public erişimi yok.
- Signing key/service account least privilege ve korumalı CI secret olarak tutulur.
- Auth/deep link/PII değişikliğinde Security reviewer zorunludur.
- P0/P1 bulgu açıkken release yapılmaz; istisna kayıtlı sahip/son tarih ve Security+CTO onayıyla [Risk Kaydı](./RISK-REGISTER.md) içinde tutulmalıdır.

## Stop-ship koşulları

- Token, API key, parola, davet token'ı veya fiş içeriğinin log/artefact içinde bulunması.
- Kullanıcının üye olmadığı planı okuyabilmesi/değiştirebilmesi.
- Refresh token replay'inin geçerli oturum üretmesi.
- İmzalama anahtarının kaybolması veya build provenance'ın doğrulanamaması.
- Kritik finansal kayıtların mükerrer oluşması ya da veri kaybı.
- Gizlilik politikası ve hesap/veri silme yolu olmadan store submission.

## Incident ve rotasyon

Credential sızıntısı şüphesinde ilgili key/session iptal edilir, kanıt korunur ve SEV1/SEV2 süreci başlatılır. AI anahtarı uygulama kesintisinden bağımsız döndürülebilmelidir. Kullanıcıya etkisi olan veri olayı Security, DevOps ve CTO'ya hemen eskale edilir.
