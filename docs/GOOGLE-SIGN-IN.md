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

`capacitor://localhost` / `https://localhost` origin'leri Google'a
tanımlanamadığı için GIS web butonu native kabukta gösterilmez. API tarafı hazır:
`POST /api/mobile/v1/auth/google` aynı doğrulamayı yapıp mobil oturum üretir.
Eksik olan tek parça, ID token'ı üreten native eklenti (ör.
`@codetrix-studio/capacitor-google-auth`) ve iOS/Android client id'lerinin
`AdditionalClientIds` listesine eklenmesi.
