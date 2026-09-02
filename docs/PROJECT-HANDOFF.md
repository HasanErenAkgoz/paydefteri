# PayDefteri Project Handoff

Bu belge, projeyi devralacak geliştiricinin PayDefteri'yi hızla çalıştırabilmesi ve güvenle geliştirebilmesi için hazırlanmıştır.

Son güncelleme: 2026-09-02  
Son commit: `1abe8ee` (`Enable document analysis for statement imports`)

## 1. Ürün Özeti

PayDefteri, ortaklı ödeme/taksit planlarını ve kişisel kredi kartı harcamalarını takip eden bir web ve mobil uygulamadır.

Ana kullanım alanları:

- Ortak taksit veya gider planı oluşturma, ortak ekleme ve pay dağıtma.
- Vade, ödeme ve dekont süreçlerini takip etme.
- Ortaklar arasındaki net borç/alacak bakiyelerini mahsuplaştırma.
- Yedek, CSV/JSON dışa aktarma ve PDF raporlama.
- Kişisel kredi kartı ekstresini analiz etme, kategori/bütçe takibi ve AI Finans Koçu ile içgörü alma.

Ürün dili Türkçe, marka adı **PayDefteri**'dir. Eski adlandırmalardan kalan bazı dosya/namespace isimleri olabilir; bunlar davranış değişikliği yapılmadan topluca değiştirilmemelidir.

## 2. Teknoloji ve Mimari

| Alan | Teknoloji |
| --- | --- |
| Web | Angular 21, standalone component'ler, Signals, TypeScript |
| Mobil | Capacitor 8 (iOS/Android) |
| API | ASP.NET Core 8, MediatR, FluentValidation |
| Veri | EF Core + PostgreSQL 16 |
| Kimlik | JWT, `HttpOnly` browser cookie; mobilde access/refresh token |
| AI | Gemini Interactions API; fiş analizinde OpenAI fallback |
| Container | Docker Compose + Nginx |

Backend Clean Architecture katmanları:

```text
PayDefteri.Domain          Saf iş kuralları, entity ve enum'lar
PayDefteri.Application     Command/query, MediatR handler, DTO, interface
PayDefteri.Infrastructure  EF Core, kimlik, AI, belge ayrıştırma, e-posta, storage
PayDefteri.Api             Controller, middleware, DI ve HTTP composition root
```

Kurallar:

- Controller iş kuralı içermez; MediatR command/query çağırır.
- `Application`, altyapı bağımlılıklarını interface üzerinden alır.
- Yeni iş kuralı önce Domain/Application katmanında tasarlanmalıdır.
- Frontend feature kodu `src/web/src/app/features`, ortak UI `shared`, API istemcileri `core` altında bulunur.

## 3. Dizin Haritası

```text
src/api/PayDefteri.Api/              HTTP API, controller, middleware, Dockerfile
src/api/PayDefteri.Application/      Use-case'ler ve sözleşmeler
src/api/PayDefteri.Domain/           Domain model ve hesap kuralları
src/api/PayDefteri.Infrastructure/   PostgreSQL, AI, belge işlemleri, auth
src/web/src/app/features/            Angular ekranları
src/web/src/app/core/                Auth, interceptor, API servisleri, platform servisleri
tests/PayDefteri.Domain.Tests/       Domain testleri
tests/PayDefteri.Api.Tests/          API/integration ve altyapı testleri
docs/                                ADR, mobil dokümanlar, bu handoff
docker-compose.yml                   Yerel PostgreSQL
docker-compose.prod.yml              Hetzner production stack
```

Önemli Angular route'ları:

- `/plans`: plan listesi/yönetimi
- `/plans/:id/dashboard`: taksit takip tablosu
- `/plans/:id/expenses`: gider planı ve ekstre içe aktarma
- `/plans/:id/balances`: bakiye/mahsuplaşma
- `/plans/:id/setup`: plan kurulum
- `/plans/:id/data`: yedek/rapor
- `/spending-analysis`: kişisel harcama analizi
- `/profile`: profil, güvenlik ve mobil oturumlar

## 4. Yerel Ortamda Çalıştırma

Gereksinimler: .NET 8 SDK, Node.js 20+, Docker.

```bash
# PostgreSQL
docker compose up -d

# API: http://localhost:5096, Swagger: /swagger
dotnet run --project src/api/PayDefteri.Api

# Angular: http://localhost:4200
npm start --prefix src/web
```

Yerel geliştirmede migration'lar API başlangıcında uygulanır. Yeni migration gerektiğinde:

```bash
dotnet ef migrations add MigrationName \
  -p src/api/PayDefteri.Infrastructure \
  -s src/api/PayDefteri.Api \
  -o Persistence/Migrations
```

AI özelliklerini yerelde kullanmak için anahtarları repo dosyalarına yazmayın. `user-secrets` kullanın:

```bash
dotnet user-secrets set "Gemini:ApiKey" "..." --project src/api/PayDefteri.Api
dotnet user-secrets set "OpenAI:ApiKey" "..." --project src/api/PayDefteri.Api
```

## 5. Test ve Doğrulama

Geliştirme tamamlandığında en az ilgili testleri çalıştırın:

```bash
# Tüm .NET testleri
dotnet test --no-restore

# Çözüm derlemesi
dotnet build PayDefteri.sln --no-restore

# Angular unit testleri
npm test --prefix src/web -- --watch=false --browsers=ChromeHeadless

# Angular production build
npm run build --prefix src/web

# Whitespace / patch kontrolü
git diff --check
```

Test yaklaşımı:

- Backend: xUnit + FluentAssertions. Başarılı ve reddedilen yollar birlikte test edilmelidir.
- Frontend: Jasmine/Karma. Davranış ve erişilebilirlik seçicileri test edilir.
- UI değişikliklerinde mobil viewport ve masaüstü görünümü manuel kontrol edilmelidir.
- Yeni özelliklerde TDD tercih edilir: önce kırmızı test, sonra implementasyon, ardından refactor.

## 6. Kimlik ve Oturum Modeli

Browser:

- Giriş/kayıt sonrası JWT, `paydefteri_session` adlı `HttpOnly`, `SameSite=Lax` cookie olarak yazılır.
- API, JWT'yi cookie'den `OnMessageReceived` ile okur.
- XSRF tokeni `paydefteri_xsrf` cookie'si üzerinden interceptor tarafından `X-XSRF-TOKEN` header'ına eklenir.
- Normal giriş tokeni kısa ömürlüdür (varsayılan 30 dakika).
- Kullanıcı giriş ekranındaki **Beni hatırla** seçeneğini işaretlerse token/cookie varsayılan 30 gün geçerlidir. Süre `Jwt:RememberMeDays` ile 1-90 gün arasında yapılandırılabilir.

Mobil:

- `MobileSessionService`, refresh tokeni native secure storage'da; web fallback'te local storage'da tutar.
- Refresh token oturumları `Profile > Cihazlar & Oturum` ekranından iptal edilebilir.

Güvenlik notu: Token, API yanıtında teknik olarak dönse de browser uygulaması cookie temelli çalışır. Frontend'e yeni token saklama yöntemi eklemeyin; `HttpOnly` cookie modelini koruyun.

## 7. Ekstre ve AI Akışları

İki farklı ekstre kullanım alanı vardır. Bu ayrım önemlidir.

### 7.1 Kişisel Harcama Analizi

Route: `/spending-analysis`  
API: `SpendingAnalysisController`  
Application: `Application/SpendingAnalysis`  
Parser: `Infrastructure/Documents/SpendingStatementParser.cs`

Akış:

1. Kullanıcı CSV, XLSX veya PDF yükler.
2. `ISpendingStatementParser` yerel/deterministik ayrıştırmayı dener.
3. PDF/XLSX okunamazsa, Gemini Interactions API fallback'i devreye girer.
4. Fallback yalnızca ekstre satırlarını ister; prompt belge içi talimatları yok sayar.
5. Belge `store: false` ile gönderilir. Ham kart/işyeri verileri redaksiyon kurallarıyla işlenir.
6. İşlemler normalize edilir, kategorize edilir ve kullanıcıya gösterilir.
7. Kullanıcı kategori, bütçe ve AI Finans Koçu ile çalışabilir.

Harcama Analizi ekranı:

- Ekstre geçmişinden tek tek silme desteklenir.
- Seçili ekstre için `Ekstre nabzı` özeti gösterilir.
- AI Finans Koçu ekstre özetinin hemen altında görünür; kullanıcı onayı/eylemiyle çalışır.
- AI koçu yalnızca sayısal/kategori bazlı özet kullanmalıdır; finansal tavsiye gibi sunulmamalıdır.

### 7.2 Gider Planına Kredi Kartı Ekstresi Aktarma

Route: `/plans/:id/expenses` içindeki `Ekstre İçe Aktar` modalı  
API: `POST /api/plans/{planId}/expenses/analyze-statement`  
Application: `AnalyzeCreditCardStatementCommand`  
Adapter: `Infrastructure/Services/GeminiCreditCardStatementAnalyzer.cs`

Bu eski/legacy akış, `SpendingStatementParser`'ı bir adaptörle kullanır. Böylece PDF, XLSX, CSV ve ekstre görselleri aynı ayrıştırma altyapısından geçer.

Desteklenen görsel formatları: JPG/JPEG, PNG, WEBP.

Gemini medya tipleri:

- PDF/XLSX: `document`
- Görsel ekstre: `image`

Bu davranış testlerle korunur. Parser veya Gemini payload'ı değiştirilirse `tests/PayDefteri.Api.Tests/SpendingAnalysisTests.cs` mutlaka güncellenmelidir.

### 7.3 Production Gemini Ayarları

`docker-compose.prod.yml` API konteynerine şu değişkenleri geçirir:

```text
GEMINI_API_KEY                 Zorunlu; gerçek değer .env içinde
GEMINI_RECEIPT_MODEL           Fiş/fatura analiz modeli
GEMINI_STATEMENT_MODEL         PDF/XLSX/görsel ekstre analiz modeli
OPENAI_API_KEY                 Gemini fiş analizi hata verirse fallback için opsiyonel
```

Anahtar değerlerini loglamayın, dokümana yazmayın veya commit etmeyin. Canlı ortamda yalnızca değişkenin yapılandırılmış olup olmadığını kontrol edin.

## 8. Canlı Ortam ve Deploy

Production stack Hetzner sunucusunda `/opt/paydefteri` altında Docker Compose ile çalışır.

Servisler:

- `postgres`: PostgreSQL 16
- `api`: ASP.NET Core API, container içi port `8080`
- `web`: Nginx ile Angular build, host port `8890`

Deployment akışı:

```bash
cd /opt/paydefteri
git fetch origin main
git pull --ff-only origin main
docker compose -f docker-compose.prod.yml --env-file .env up -d --build
```

Doğrulama:

```bash
docker compose -f docker-compose.prod.yml --env-file .env ps
curl -fsS http://localhost:8890/health
```

Notlar:

- `.env` sunucuya özeldir; repoya alınmaz.
- Canlıda önce API/web image build olur, sonra konteynerler yeniden oluşturulur.
- Health endpoint başarılı cevapta `{"status":"ok"}` döner.
- Sunucudaki eski/ilgisiz untracked dosyaları silmeyin veya reset atmayın.

## 9. Son Tamamlanan İşler

Yakın commitler ve etkileri:

| Commit | Değişiklik |
| --- | --- |
| `1abe8ee` | Legacy ekstre importunu ortak parser'a bağladı; PDF/XLSX/CSV/görsel belge analizi desteği |
| `f759810` | `Beni hatırla` seçeneğiyle 30 günlük kalıcı browser oturumu |
| `36adc0b` | AI Finans Koçu'nu ekstre özetinin altına taşıdı ve görünürlüğünü artırdı |
| `b5fcdca` | Profil ekranında mobil taşma/kayma düzeltmeleri |
| `8247d1b` | Harcama Analizi ekstre silme ve `Ekstre nabzı` özeti |
| `a1f9cfa` | Banka ekstrelerinde esnek başlık eşleme ve Gemini belge fallback'i |
| `4ebbd3f` | Bakiye akışı ve genel frontend UX iyileştirmeleri |

## 10. Bilinen Notlar ve Teknik Borç

- Angular production build şu an hata vermeden tamamlanır; ancak `expenses.component.html` ve `plan-list.component.html` içinde gereksiz `?.`/`??` diagnostic uyarıları vardır.
- `landing.component.scss` ve `data.component.scss` component style budget uyarısı üretir. Bu uyarılar yeni ekstre çalışmasından bağımsız, mevcut CSS boyutlarıyla ilgilidir.
- `GeminiCreditCardStatementAnalyzer` sınıf adı tarihsel olarak kalmıştır; artık bir Gemini çağrısı yapmak yerine ortak parser'a adaptördür. Büyük bir refactor yapılacaksa adı `CreditCardStatementImportAdapter` gibi daha doğru bir isimle değiştirilebilir.
- Ekstre ayrıştırma işinde banka formatları sürekli değişebilir. Yeni bir banka örneği geldiğinde önce fixture/test ekleyip parser kurallarını sonra genişletin.
- AI çıktılarını doğrudan muhasebe gerçeği gibi kabul etmeyin; kullanıcı onayı ve işlem önizlemesi korunmalıdır.

## 11. Geliştirme Prensipleri

- Mevcut design system ve mobil davranışları koruyun; her ekranda masaüstü/mobil karşılığını kontrol edin.
- Kullanıcı verisi, API anahtarı, sunucu `.env` dosyası veya gerçek dokümanları commit etmeyin.
- Önceden var olan dirty worktree değişikliklerini geri almayın.
- Commit mesajları kısa ve emir kipinde olmalı: `Fix mobile profile layout` gibi.
- Yeni endpointlerde yetkilendirme, sahiplik kontrolü ve XSRF modelini göz ardı etmeyin.

Ek ayrıntılar için mevcut belgeler:

- `README.md`: hızlı başlangıç
- `AGENTS.md`: repository kuralları
- `docs/ADR-001-expense-plans.md`: gider planı kararı
- `docs/ADR-002-multi-currency.md`: çoklu para birimi
- `docs/ADR-003-capacitor-mobile.md`: mobil mimari
- `docs/mobile/`: mobil ürün, API ve release dokümanları
- `docs/frontend-ux-audit-presentation.md`: önceki frontend UX analizi
