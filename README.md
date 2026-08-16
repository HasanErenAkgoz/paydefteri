# PayDefteri

Çok ortaklı borç & taksit takip. Ortaklar arasında paylaşımlı ödeme planlarını, mahsuplaşmayı ve tahsisat takibini yönetir.

Referans prototip: [`fuzul-taksit-takip.html`](fuzul-taksit-takip.html) (davranış spec’i; Fuzul bir örnek şablondur).

**Stack:** Angular 19 · ASP.NET Core 8 · EF Core · PostgreSQL · JWT

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker (PostgreSQL)

## Quick start

```bash
# 1. Database
docker compose up -d
# Postgres: user/password/db = taksitle (container: taksitle-db)

# 2. API (http://localhost:5096 — Swagger at /swagger)
dotnet run --project src/api/FuzulTaksitTakip.Api

# 3. Web (http://localhost:4200)
npm start --prefix src/web
```

> Eski `fuzul_*` volume’u varsa temiz başlatmak için: `docker compose down -v && docker compose up -d`

Development migrations apply automatically on API startup.

### Fiş/fatura görselinden gider taslağı

“Yeni gider” formundaki **Fişten doldur** özelliği birincil olarak Gemini API kullanır; Gemini
teknik hata verirse OpenAI yedek olarak denenir. Anahtarları repoya veya `appsettings.json`
dosyasına yazmayın; geliştirme ortamında user-secrets kullanın:

```bash
dotnet user-secrets set "Gemini:ApiKey" "gemini-api-key" \
  --project src/api/FuzulTaksitTakip.Api
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." \
  --project src/api/FuzulTaksitTakip.Api
```

Varsayılan Gemini modeli `gemini-3.1-flash-lite` olup `Gemini:ReceiptModel` ile değiştirilebilir.
Müşteri belgeleri için verilerin model geliştirmede kullanılmadığı ücretli API katmanını kullanın.
JPEG, PNG ve WebP dosyaları en fazla 8 MB olarak kabul edilir. Görsel kalıcı depolamaya yazılmaz;
analiz sonucu yalnızca kullanıcı tarafından kontrol edilip kaydedilecek bir gider taslağıdır.

## Solution layout

```
FuzulTaksitTakip.sln          # technical solution id (legacy path)
src/api/...                   # Clean Architecture API
src/web/                      # Angular — PayDefteri UI
tests/
.cursor/                      # Agent harness
docker-compose.yml
```

> Product brand: **PayDefteri**. Code namespaces may still say `FuzulTaksitTakip` until a dedicated rename pass.

## Commands

| Action | Command |
|--------|---------|
| Build API | `dotnet build FuzulTaksitTakip.sln` |
| Domain tests | `dotnet test` |
| Web serve | `npm start --prefix src/web` |
| Web build | `cd src/web && npx ng build` |
| New migration | `dotnet ef migrations add Name -p src/api/FuzulTaksitTakip.Infrastructure -s src/api/FuzulTaksitTakip.Api -o Persistence/Migrations` |

## First use

1. Register at `/register`
2. Create a plan
3. Setup → **Fuzul şablonunu yükle** (örnek: 1.070.000 ₺ / 20 taksit)
4. Dashboard’da ödemeleri işaretle; Data’dan JSON yedek al

## Domain rules (share)

- `default` → `totalAmount * defaultPct / 100`
- `equal` → `totalAmount / partnersCount`
- `custom` → `customShares[partnerId]` (sum must equal total)

Settlement: `paidBy ≠ owner` → N-party net balances.

## Agent workflow

See [`.cursor/settings.json`](.cursor/settings.json): `plan-phase` → `implement-phase` → `verify-phase` → `quality-gate`.
