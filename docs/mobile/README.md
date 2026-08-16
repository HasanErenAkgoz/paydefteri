# PayDefteri Mobil Doküman Seti

Bu dizin, PayDefteri'nin Angular 19 arayüzünü Capacitor ile iOS ve Android'e taşıma çalışmasının geliştirme ve yayın kaynağıdır. Belgeler birbirini tamamlar; çelişkide ürün kapsamı için PRD, teknik karar için ADR ve teknik tasarım esas alınır.

## Okuma sırası

1. [Mobil PRD](../MOBILE-PRD.md) — problem, kullanıcı, MVP ve başarı ölçütleri.
2. [Teknik Tasarım](../MOBILE-TECHNICAL-DESIGN.md) — hedef mimari ve teknik sınırlar.
3. [ADR-003](../ADR-003-capacitor-mobile.md) — Angular + Capacitor kararının gerekçesi.
4. [Geliştirme Rehberi](./DEVELOPMENT-GUIDE.md) — yerel kurulum, kod ve PR standardı.
5. [Uygulama Planı](./IMPLEMENTATION-PLAN.md) — fazlar, bağımlılıklar ve teslim sırası.
6. [Ürün Backlog'u](./PRODUCT-BACKLOG.md) — öncelikli epic ve user story'ler.
7. [UX Spesifikasyonu](./UX-SPEC.md) — mobil davranış ve tasarım handoff'u.
8. [API Sözleşmesi](./API-CONTRACT.md) — mevcut ve planlanan mobil API davranışı.
9. [Güvenlik ve Gizlilik](./SECURITY-PRIVACY.md) — threat model ve güvenlik kapıları.
10. [Risk Kaydı](./RISK-REGISTER.md) — açık karar, risk, kontrol ve sahipler.
11. [Test Stratejisi](./TEST-STRATEGY.md) — otomasyon, cihaz ve kalite yaklaşımı.
12. [Kabul Checklist'i](./ACCEPTANCE-CHECKLIST.md) — Definition of Ready/Done ve ürün kabulü.
13. [Release Runbook](./RELEASE-RUNBOOK.md) — beta, mağaza yayını, izleme ve rollback.

## Karar sahipleri

| Alan | Sahip |
|---|---|
| Kapsam, öncelik, başarı metrikleri | Product Manager |
| Gereksinim ve kabul kriteri | Business Analyst |
| Mimari ve teknik standart | CTO |
| Mobil deneyim ve erişilebilirlik | UI/UX Designer |
| Uygulama ve API çözümü | Frontend / Backend |
| Test stratejisi ve kalite kapısı | QA |
| Güvenlik ve risk seviyesi | Security/AppSec |
| CI/CD, imzalama, rollout ve rollback | DevOps |

## Çalışma kuralları

- MVP dışı maddeler açık karar olmadan geliştirmeye alınmaz.
- Her story, [Kabul Checklist'i](./ACCEPTANCE-CHECKLIST.md) içindeki DoR koşullarını sağlamalıdır.
- Auth, finansal veri, deep link veya native izin değişikliği Security ve CTO incelemesi ister.
- Her PR test kanıtı, güvenlik/gizlilik etkisi ve rollback notu içerir.
- Production deploy ayrı bir kullanıcı onayı olmadan yapılmaz.

## Belge durumu

| Belge | Durum |
|---|---|
| PRD ve teknik tasarım | Kodlamaya hazır |
| ADR | Kabul önerisi |
| Uygulama planı ve backlog | Kodlamaya hazır |
| UX ve API sözleşmesi | Faz 1–3 için hazır |
| Güvenlik, test ve kabul | Kalite kapısı olarak hazır |
| Release runbook | Beta öncesi değerlerle tamamlanacak operasyon şablonu |
