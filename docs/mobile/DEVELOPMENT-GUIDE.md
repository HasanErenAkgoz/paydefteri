# Mobil Geliştirme Rehberi

## Ön koşullar

- .NET SDK 8, Node.js 22 ve npm.
- macOS üzerinde güncel tam Xcode; Android Studio, hedef Android SDK ve JDK 21.
- Docker Desktop ile yerel PostgreSQL.
- Apple/Google signing bilgileri local debug için zorunlu değildir.
- Gemini/OpenAI anahtarları mobil uygulamaya değil yalnız API user-secrets'a eklenir.

## Mevcut projeyi çalıştırma

```bash
docker compose up -d
dotnet run --project src/api/PayDefteri.Api
npm start --prefix src/web
```

Web: `http://localhost:4200`, API: `http://localhost:5096`, Swagger: `/swagger`.

## Mobil komutlar

```bash
npm run build:mobile --prefix src/web
npx --prefix src/web cap sync ios
npx --prefix src/web cap sync android
npx --prefix src/web cap open ios
npx --prefix src/web cap open android
```

Android CLI doğrulaması: `cd src/web/android && ./gradlew assembleDebug`.
Capacitor 8 bu adımda JDK 21 ister. iOS simulator/archive doğrulaması için `xcode-select`
tam Xcode kurulumunu göstermelidir; Command Line Tools tek başına yeterli değildir.

`build:mobile` production Angular çıktısını `capacitor.config.ts` içindeki `webDir` dizinine üretir. Plugin veya web asset değişikliğinden sonra `cap sync` çalıştırılır. Native klasörde elle yapılan anlamlı değişiklik repoya alınır; DerivedData, Gradle build ve imzalama artefact'ları alınmaz.

## Kod yerleşimi

- Feature ekranları: `src/web/src/app/features`.
- Paylaşılan UI: `src/web/src/app/shared`.
- API modelleri/servisleri: `src/web/src/app/core`.
- Platform sözleşmeleri/adaptörleri: `src/web/src/app/core/platform`.
- Mobil auth backend: Application command/query, Infrastructure persistence/token servisi, API controller.
- Native projeler: `src/web/ios` ve `src/web/android`.

Feature component doğrudan Capacitor plugin, `window`, secure storage veya platform kontrolü çağırmaz. Bunun yerine bir port enjekte eder. İş ve yetkilendirme kuralı backend'de kalır.

## Ortamlar

- `environment.ts`: yerel web.
- `environment.prod.ts`: production web, same-origin `/api`.
- `environment.mobile.ts`: production/test mobil API URL'si.

API URL, app sürümü ve telemetry ortam bazlı olabilir. API key, JWT signing key, SMTP veya signing secret hiçbir Angular environment dosyasına konmaz.

## Kod standardı

- TypeScript 2 boşluk ve tek tırnak; component PascalCase, dosya kebab-case.
- C# nullable açık; PascalCase public üyeler, camelCase local değişkenler.
- Küçük, tek sorumluluklu sınıf/fonksiyon; erken/genel soyutlama yok.
- Observable subscription yaşam döngüsü güvenli olmalı; sınırsız retry yasaktır.
- Hata mesajları kullanıcıya eylem sunmalı, sağlayıcı/secret detayı göstermemeli.

## Branch, commit ve PR

- Branch: `feature/mobile-<kısa-kebab-açıklama>`; thread branch gerekiyorsa repo standardına uygun `codex/` öneki kullanılabilir.
- Commit: `feat(mobile): add capacitor platform adapters` gibi Conventional Commit.
- PR: amaç, kapsam/kapsam dışı, test kanıtı, ekran görüntüsü, security/privacy etkisi, migration, rollout ve rollback.
- Auth, PII, deep link veya signing değişikliği ikinci uzman incelemesi ister.

## Yerel doğrulama

```bash
dotnet build PayDefteri.sln
dotnet test
npm test --prefix src/web -- --watch=false
npm run build --prefix src/web -- --configuration=production
```

Mobil değişiklikte bunlara Android debug build, iOS simulator build ve en az bir gerçek cihaz smoke testi eklenir. Test anahtarı ve sentetik fiş kullanılır; production verisi kullanılmaz.

## Debug ilkeleri

- Token, parola, fiş base64 veya finansal veri console/log'a yazılmaz.
- Native network inspection yalnız test hesabı ve test verisiyle yapılır.
- Platform farkı feature içine koşullu ifadelerle yayılmadan adaptörde çözülür.
- Bir native plugin sorununda önce port sözleşmesi ve browser fallback korunur.
