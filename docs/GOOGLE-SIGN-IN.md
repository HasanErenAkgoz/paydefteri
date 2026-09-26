# Google ile giriş / kayıt

Google butonu tek bir uçla hem girişi hem kaydı karşılar: adres ilk kez
geldiğinde API hesabı oluşturur, sonraki girişlerde aynı hesabı bulur.

## Akış

1. Tarayıcı, Google Identity Services (GIS) butonunu çizer ve kullanıcı Google
   hesabını seçtiğinde bir **ID token** alır.
2. İstemci bu token'ı `POST /api/auth/google` gövdesinde gönderir.
3. API token'ı sunucu tarafında doğrular (imza, issuer, son kullanma, audience),
   ardından hesabı çözer ve normal oturum çerezini bırakır.

Token'ın kendisi tek kanıt olduğu için istemcinin ilettiği e-posta veya isim
hiçbir zaman güvenilmez; her şey doğrulanmış token'dan okunur.

## Hesap eşleşmesi

| Durum | Sonuç |
|---|---|
| Aynı Google hesabıyla daha önce girilmiş | Aynı hesap açılır |
| Aynı e-postayla şifreli hesap var | Hesaplar **bağlanır**, şifre çalışmaya devam eder |
| Hiçbiri yok | Şifresiz, e-postası doğrulanmış yeni hesap açılır |
| Google'da e-posta doğrulanmamış | **Reddedilir** (401) — aksi halde başkasının adresi sahiplenilebilirdi |

Google ile açılan hesabın şifresi yoktur: profil ekranı şifre değiştirme
bölümünü gizler ve hesap silme şifre istemez (`UserProfileDto.hasPassword`).

## Google Cloud kurulumu

1. [Google Cloud Console](https://console.cloud.google.com/apis/credentials) →
   **Create credentials → OAuth client ID → Web application**.
2. **Authorized JavaScript origins**: `https://paydefteri.com` (yerel geliştirme
   için ayrıca `http://localhost:4200`).
3. Redirect URI gerekmez — GIS popup modunda çalışır.
4. OAuth consent screen'i yayınlayın; aksi halde yalnızca test kullanıcıları
   giriş yapabilir.

## Yapılandırma

Client id gizli bir değer değildir (sayfa kaynağında görünür), ama ortama göre
değiştiği için commit edilmez.

| Katman | Anahtar |
|---|---|
| API | `Authentication:Google:ClientId` — prod'da `GOOGLE_CLIENT_ID` ortam değişkeni |
| API (native istemciler) | `Authentication:Google:AdditionalClientIds` — iOS/Android client id'leri |
| Web | `environment.googleClientId` — prod imajına `GOOGLE_CLIENT_ID` build arg'ı ile girer |

Yerel geliştirme:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>.apps.googleusercontent.com" --project src/api/PayDefteri.Api
```

ve `src/web/src/environments/environment.ts` içindeki `googleClientId` alanına
aynı değeri yazın.

Boş bırakıldığında buton hiç çizilmez ve uç `401` döner; yarım çalışan bir buton
görünmez.

## Mobil (Capacitor)

GIS web butonu native kabukta çizilemez: `capacitor://localhost` / `https://localhost`
origin'leri Google'a authorized origin olarak tanımlanamıyor. Bu yüzden mobilde
token **native eklentiden** gelir, buton da bizimdir (Google'ın koyu tema
butonu spesifikasyonuna göre çizilir).

Eklenti: **`@capgo/capacitor-social-login`**. Capacitor 8 ile uyumlu olan ve
Firebase gerektirmeyen tek aktif seçenek — `@codetrix-studio/capacitor-google-auth`
Capacitor 6'da kalmıştır, bu projede çalışmaz.

Akış web ile aynı uca varır: eklentinin döndürdüğü ID token'ın audience'ı **web
client id**'dir, yani API tarafında ekstra bir doğrulama kuralı gerekmez. Tek
fark, token'ın `POST /api/mobile/v1/auth/google` ucuna gidip cookie yerine mobil
oturum (access + refresh) üretmesi.

### Gereken OAuth client'ları

| Tip | Ne için | Ayar |
|---|---|---|
| Web | Token'ın audience'ı; Android ve web bunu kullanır | `environment.googleClientId` |
| iOS | Uygulamayı Google'a tanıtır | `environment.googleIosClientId` + Info.plist URL scheme |
| Android | Uygulamayı paket adı + SHA-1 ile tanıtır | Konsolda tanımlı olması yeter, koda girmez |

Android client'ı **imzalayan sertifikanın SHA-1'i başına** tanımlanır. Pratikte
birden fazla gerekir; her biri için ayrı bir Android client açılır (paket adı
aynı kalır):

| Yapı | SHA-1 kaynağı |
|---|---|
| Yerel debug | `keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android` |
| Play'den kurulan (kapalı test / üretim) | Play Console → Uygulama imzalama → **uygulama imzalama anahtarı** SHA-1 |
| Yerelde imzalanıp yan yüklenen release | Upload keystore'un SHA-1'i |

Play App Signing aktifken mağazadan kurulan uygulamayı **upload anahtarı değil**,
Play'in uygulama imzalama anahtarı imzalar — Android client'a o SHA-1 girilmezse
mağaza sürümünde Google girişi sessizce başarısız olur.

### Kayıtlı client'lar ve parmak izleri

Bunların hiçbiri gizli değil; client id'ler sayfa kaynağında görünür, parmak
izleri ise sertifika özetidir.

| Client | Tip | Değer |
|---|---|---|
| PayDefteri Web | Web | `623515520878-igetq8uafv653fofpsv1vodu4is6ievp` |
| PayDefteri iOS | iOS | `623515520878-or9gj6l238th8dv0fiv8see321c08f80` |
| PayDefteri Android (debug) | Android | SHA-1 `2E:45:AA:CC:44:8E:4B:2F:3F:03:CD:FF:CA:97:BD:F8:2F:5E:1D:FE` |
| PayDefteri Android (Play app signing) | Android | SHA-1 `E1:9E:31:9F:42:D6:6F:DD:85:62:95:FB:72:AE:04:0C:E7:DB:17:6F` |

Debug parmak izi `~/.android/debug.keystore`'dan, Play parmak izi Play Console →
Uygulama imzalama → "Klasik anahtar" sütunundan gelir. İmza anahtarı bir gün
döndürülürse yeni SHA-1 için yeni bir Android client açılmalıdır.

### iOS

`Info.plist` içinde ters çevrilmiş iOS client id bir URL scheme olarak kayıtlı
olmalı (`com.googleusercontent.apps.<iOS client id>`). Bu olmadan Google
oturumu uygulamaya geri dönemez.

### Sürüm notu

Mobil bundle'ı senkronlamak (`npm run mobile:sync`) kullanıcıların telefonundaki
uygulamayı değiştirmez — yeni bir AAB/IPA üretip mağazaya göndermek gerekir.
