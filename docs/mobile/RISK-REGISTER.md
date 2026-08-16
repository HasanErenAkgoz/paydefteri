# Mobil Karar ve Risk Kaydı

## Açık kararlar

| ID | Karar | Öneri | Sahip | Son tarih |
|---|---|---|---|---|
| DEC-01 | Apple Developer ve Google Play hesap sahipliği | Tek kişiye bağlı olmayan kurumsal erişim | PM + DevOps | Faz 0 |
| DEC-02 | Secure storage native implementasyonu | Keychain/Keystore-backed, bakımlı adapter; güvenlik incelemesi sonrası kilitle | CTO + Security | Faz 1 |
| DEC-03 | Telemetry/crash sağlayıcısı | PII allowlist ve veri bölgesi değerlendirmesiyle seç | DevOps + Security | Faz 4 |
| DEC-04 | Retention ve hesap/veri silme süresi | Veri sahibi ve hukuk/uyumluluk onayıyla kesinleştir | PM + Security | Beta öncesi |
| DEC-05 | Minimum desteklenen app sürümü yönetimi | Server configuration; yalnız kritik durumda hard-block | CTO + DevOps | Faz 4 |

## Riskler

| ID | Seviye | Risk / tetikleyici | Kontrol | Sahip | Durum |
|---|---|---|---|---|---|
| R-01 | P1 | Refresh token cihaz/log/backup üzerinden sızar | Secure storage, backup exclusion, rotation, redaction | Security + Backend | Açık |
| R-02 | P1 | Mobil auth eklenirken web cookie/XSRF akışı bozulur | Ayrı endpoint, geriye uyumluluk, regresyon suite | Backend + QA | Açık |
| R-03 | P1 | Retry/çift dokunma mükerrer gider veya ödeme üretir | UI lock, idempotency key, transaction | Backend + Frontend | Açık |
| R-04 | P1 | Üye olmayan kullanıcı plan verisine erişir | Server-side authz ve negative integration tests | Backend + Security | Açık |
| R-05 | P2 | WebView klavye/safe-area/back davranışı ana akışı bozar | Platform adapter ve gerçek cihaz matrisi | Frontend + QA | Açık |
| R-06 | P2 | Universal/App Link yanlış config nedeniyle web'e düşer | Association dosyası CI/smoke doğrulaması | DevOps + QA | Açık |
| R-07 | P1 | Davet token'ı analytics/crash loguna girer | URL/token redaksiyonu ve allowlist telemetry | Security | Açık |
| R-08 | P1 | Fiş/finans verisi AI veya loglarda gereğinden fazla tutulur | `store=false`, log yasağı, privacy incelemesi | Security + Backend | Açık |
| R-09 | P2 | Store incelemesi izin/gizlilik nedeniyle reddeder | Minimum izin, doğru purpose string, silme URL'si | PM + DevOps | Açık |
| R-10 | P1 | Signing secret kaybolur/sızar | CI vault, least privilege, rotasyon/kurtarma kaydı | DevOps + Security | Açık |
| R-11 | P2 | Eski mobil sürüm backend migration sonrası çalışmaz | Expand-contract ve N-1 uyumluluk testi | CTO + Backend | Açık |
| R-12 | P2 | Offline cache hesaplar arasında görünür | User-scoped encrypted cache ve logout temizliği | Frontend + Security | Açık |

## Risk değerlendirme kuralı

- **P0:** Release durur, incident başlar; kabul edilemez.
- **P1:** Release öncesi düzeltilir; istisna yalnız Security + CTO + iş sahibi ve kısa son tarihle.
- **P2:** Sahip ve en geç 30 günlük hedefle planlanır.
- **P3:** Backlog'a alınır ve 90 gün içinde yeniden değerlendirilir.

Bir risk yalnız düzeltme kanıtı ve yeniden testle kapanır. `Durum` değerleri `Açık`, `Azaltılıyor`, `Kabul edildi`, `Kapalı` olabilir. Risk kabulü kalıcı istisna değildir; telafi kontrolü, sahibi ve bitiş tarihi kaydedilir.

