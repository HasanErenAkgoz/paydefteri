# Mobil API Sözleşmesi

## Durum ve kapsam

Bu belge mobil uygulama için **planlanan** auth sözleşmesini ve kullanılacak mevcut iş endpoint'lerini tanımlar. Mevcut web auth cookie + XSRF akışı değişmeden kalır. Mobil istemci `/api/mobile/v1/auth/*` üzerinden Bearer access token ve dönen refresh token kullanır.

Production base URL: `https://paydefteri.com/api`

## Ortak kurallar

- JSON alanları camelCase, tarihler `YYYY-MM-DD`, zamanlar UTC ISO-8601 biçimindedir.
- Hatalar `application/problem+json` döner.
- Mobil istekler `X-Client-Platform: ios|android` ve `X-App-Version` başlıklarını gönderir.
- Korumalı iş endpoint'leri `Authorization: Bearer <access-token>` ister.
- Mobil Bearer istekleri browser XSRF token'ı taşımaz; sunucu mevcut antiforgery middleware'inde Authorization başlığını ayırır.
- Secret, token, fiş görseli ve finansal içerik loglanmaz.

## Mobil auth endpoint'leri

### `POST /api/mobile/v1/auth/login`

İstek:

```json
{
  "email": "user@example.com",
  "password": "**********",
  "device": {
    "deviceName": "Yusuf'un iPhone'u",
    "platform": "ios",
    "appVersion": "1.0.0"
  }
}
```

Yanıt `200`:

```json
{
  "accessToken": "<jwt>",
  "accessTokenExpiresAt": "2026-08-13T12:30:00Z",
  "refreshToken": "<opaque-token>",
  "refreshTokenExpiresAt": "2026-09-12T12:00:00Z",
  "sessionId": "<guid>",
  "user": {
    "userId": "<identity-id>",
    "email": "user@example.com",
    "displayName": "Yusuf"
  }
}
```

Hatalar: `400` doğrulama, `401` hatalı bilgi, `429` rate limit.

### `POST /api/mobile/v1/auth/refresh`

```json
{ "refreshToken": "<opaque-token>" }
```

Başarıda hem access hem refresh token yenilenir; gönderilen refresh token tekrar kullanılamaz. Süresi dolmuş/iptal/replay token `401` döndürür. Replay tespitinde aynı token ailesi iptal edilir.

### `POST /api/mobile/v1/auth/logout`

Bearer access token ve body'de mevcut refresh token gönderilir. İlgili cihaz session'ı idempotent olarak iptal edilir; başarı `204`.

### `GET /api/mobile/v1/auth/sessions`

Kullanıcının aktif cihaz session'larını token göstermeden döndürür:

```json
[
  {
    "id": "<guid>",
    "deviceName": "Yusuf'un iPhone'u",
    "platform": "ios",
    "appVersion": "1.0.0",
    "createdAtUtc": "2026-08-13T12:00:00Z",
    "lastUsedAtUtc": "2026-08-13T12:15:00Z",
    "isCurrent": true
  }
]
```

### `DELETE /api/mobile/v1/auth/sessions/{id}`

Yalnız session sahibi çağırabilir. Mevcut veya başka cihaz session'ını iptal eder; bulunamayan/kullanıcıya ait olmayan kayıt bilgi sızdırmayan `404` döndürür.

## Token güvenlik sözleşmesi

- Access JWT: 30 dakika, uygulama belleği.
- Refresh token: 30 gün, rastgele en az 256-bit entropy, cihaz secure storage.
- Veritabanında yalnız token hash'i tutulur.
- Parola değişimi ve hesap güvenlik olayı tüm refresh session'larını iptal eder.
- İstemci aynı anda yalnız bir refresh isteği çalıştırır; başarısız isteği en fazla bir kez tekrarlar.

## Mevcut iş endpoint'leri

Mobil istemci mevcut endpoint'leri Bearer ile yeniden kullanır:

- `GET/POST /api/plans`
- `GET/PUT/DELETE /api/plans/{planId}` ve mevcut plan lifecycle endpoint'leri
- `GET /api/plans/{planId}/expenses/board`
- `GET/POST /api/plans/{planId}/expenses`
- `PUT/DELETE /api/plans/{planId}/expenses/{expenseId}`
- `POST /api/plans/{planId}/expenses/analyze-receipt`
- Gider kategori, transfer ve recurrence endpoint'leri
- Mevcut installment/payment/receipt endpoint'leri
- Mevcut invite oluşturma/yenileme/kabul endpoint'leri
- `GET/PUT /api/auth/me|profile` ve parola değiştirme davranışı

Her iş endpoint'inde plan üyeliği/yetki kontrolü sunucuda kalır; mobil istemci yetkiyi güvenlik sınırı olarak yorumlamaz.

## Fiş analizi sözleşmesi

- `multipart/form-data`, alan adı `file`.
- Desteklenen MIME: `image/jpeg`, `image/png`, `image/webp`.
- En fazla 8 MB; dosya imzası sunucuda doğrulanır.
- Başarı mevcut `ExpenseReceiptDraftDto` biçimindedir.
- `400`: geçersiz dosya, `401/403`: auth/yetki, `429`: kota, `503`: AI sağlayıcı erişilemiyor.
- `503` halinde kullanıcı manuel girişe devam eder; istemci otomatik sonsuz retry yapmaz.

## Idempotency

Mobil bağlantı belirsizliğinde mükerrer finansal kayıtları önlemek için oluşturma endpoint'lerine aşamalı olarak `Idempotency-Key` desteği eklenir:

- İstemci her kullanıcı aksiyonunda UUID üretir.
- Aynı kullanıcı + endpoint + key, aynı request hash'iyle önceki sonucu döndürür.
- Aynı key farklı request body ile `409` döndürür.
- Anahtarların varsayılan saklama süresi 24 saattir.

MVP için öncelik: gider, ödeme, transfer ve davet kabul işlemleri.

## Versiyonlama ve uyumluluk

- Yalnız mobil auth `/mobile/v1` ile başlar; mevcut business API geriye uyumlu kullanılır.
- Kırıcı response değişikliği yeni path sürümü veya additive geçiş gerektirir.
- Bilinmeyen response alanlarını mobil istemci yok sayar.
- Minimum desteklenen app sürümü server configuration ile yönetilir; güvenlik zorunluluğu dışında hard-block kullanılmaz.
