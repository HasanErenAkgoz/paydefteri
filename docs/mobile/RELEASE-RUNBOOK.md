# Mobil Release ve Operasyon Runbook'u

## Amaç ve kapsam

PayDefteri iOS/Android sürümünü tekrarlanabilir, ölçülebilir ve geri alınabilir biçimde TestFlight, Google Play test kanalları ve production'a yayınlamak. Production deploy/yayın kullanıcıdan ayrıca açık onay ister.

## Ortamlar ve kanallar

| Ortam | API | Dağıtım |
|---|---|---|
| Local | localhost/dev API | Simulator/emulator |
| Test | Test veri ve test AI anahtarı | CI artefact/internal |
| Production | `https://paydefteri.com/api` | TestFlight / Play Internal → Closed → Production |

Production verisi test ortamına kopyalanmaz. Test ve production app configuration ayrıdır.

## Sürümleme

- Kullanıcı sürümü SemVer: `MAJOR.MINOR.PATCH`.
- iOS build number ve Android versionCode her artefact'ta monoton artar.
- Release branch yalnız gerekirse `release/<version>`; normal geliştirme feature branch + PR ile ilerler.
- Release notu kullanıcı görünür değişiklik, düzeltme, bilinen sınırlama ve gerekli migration'ı içerir.

## Signing ve secret yönetimi

- Apple sertifika/provisioning, App Store Connect API key ve Android upload key/Play service account CI secret vault'tadır.
- Secret, `.env`, keystore, `.p12`, provisioning profile ve API key repoya girmez.
- CI erişimi least privilege; signing işlemi korumalı branch/tag üzerinde yapılır.
- Key sahibi, rotasyon tarihi ve kurtarma prosedürü erişimi sınırlı operasyon kaydında tutulur.

## Release öncesi checklist

- [ ] Hedef commit/tag ve release kapsamı sabitlendi.
- [ ] [Kabul Checklist'i](./ACCEPTANCE-CHECKLIST.md) tamamlandı.
- [ ] .NET, Angular, Android ve iOS CI yeşil.
- [ ] Migration forward/rollback ve yedekleme adımı doğrulandı.
- [ ] Production config'te development endpoint/secret yok.
- [ ] App icon, splash, screenshot ve mağaza açıklaması güncel.
- [ ] Kamera/fotoğraf izin açıklamaları gerçek kullanımla uyumlu.
- [ ] Privacy Manifest, Data Safety, gizlilik ve hesap silme URL'si hazır.
- [ ] Universal/App Link association dosyaları production domain'de doğrulandı.
- [ ] Crash/metric dashboard ve eyleme dönük alert aktif.
- [ ] Support iletişimi ve bilinen sorunlar hazır.

## Yayın adımları

1. İmzalı iOS/Android artefact'ı CI'da üret; checksum/build provenance kaydet.
2. TestFlight Internal ve Play Internal kanalına yükle.
3. Temiz kurulum ve upgrade smoke testlerini gerçek cihazlarda çalıştır.
4. En az bir owner ve bir partner hesabıyla login, plan, gider, fiş, davet ve logout akışını doğrula.
5. Closed beta grubuna aç; 7 gün crash-free/login/gider metriklerini izle.
6. Go/no-go onayından sonra staged rollout başlat: `%10 → %25 → %50 → %100`.
7. Her aşamada en az 24 saat veya yeterli oturum örneği gözle; hata bütçesi aşılırsa durdur.

## Sağlıklı durum

- API `/health` başarılı.
- Crash-free sessions `≥%99,5`.
- Login başarı oranı `≥%98`.
- Ana akış mobil hata oranı `<%2`.
- Receipt analizinde sağlayıcı/config hatası alarm eşiğinin altında.
- Veri bütünlüğü veya yetki ihlali sinyali yok.

Kesin dashboard ve alarm bağlantıları beta altyapısı kurulunca bu belgeye eklenir.

## Rollback ve azaltım

Mobil mağaza binary'si anlık geri alınamaz. Öncelik:

1. Staged rollout'u durdur.
2. Sorun server configuration veya backend uyumluluğundaysa geriye uyumlu server fix/rollback uygula.
3. Minimum sürüm hard-block'u yalnız güvenlik/veri kaybı durumunda kullan.
4. Gerekirse ilgili server özelliğini kontrollü kapat; temel manuel gider akışını açık tut.
5. Düzeltme build'ini hızlandırılmış review ile yayınla.

Database migration genişlet-daralt yaklaşımıyla en az bir eski mobil sürümle uyumlu kalmalıdır. Destructive migration aynı release'te yapılmaz.

## Incident seviyeleri

- **SEV1:** Veri kaybı/ihlali, yetki aşımı, yaygın giriş veya finansal kayıt kesintisi.
- **SEV2:** Ana akışın önemli kullanıcı grubunda bozulması; workaround sınırlı.
- **SEV3:** Sınırlı cihaz/sürüm etkisi, workaround mevcut.
- **SEV4:** Düşük etkili kozmetik/iyileştirme.

SEV1'de rollout durdurulur; DevOps incident komutası, CTO teknik liderlik, Security veri/güvenlik değerlendirmesi yapar. SEV1/SEV2 sonrası suçlamasız postmortem ve sahip/tarihli aksiyonlar zorunludur.

## Release sonrası doğrulama

- [ ] İlk 30 dakika ve 2/8/24 saat dashboard kontrolü.
- [ ] Crash, login, refresh, gider oluşturma, fiş analiz ve deep link metrikleri normal.
- [ ] Store yorumları ve destek talepleri triage edildi.
- [ ] Bilinen riskler ve rollout yüzdesi kaydedildi.
- [ ] `%100` rollout sonrası 7 günlük sonuç PM/CTO/QA ile değerlendirildi.

## Acil iletişim şablonu

`PayDefteri mobil <sürüm> içinde <etkilenen akış> sorunu tespit edildi. Etki: <kullanıcı/iş etkisi>. Rollout <durduruldu/devam ediyor>. Geçici çözüm: <var/yok>. Sonraki güncelleme: <saat>. Finansal veri veya hesap güvenliği etkisi: <doğrulandı/inceleniyor/yok>.`

