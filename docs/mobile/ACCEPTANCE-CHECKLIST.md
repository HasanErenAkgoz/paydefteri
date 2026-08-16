# Mobil Kabul ve Tamamlanma Checklist'i

## Definition of Ready (DoR)

Bir story geliştirmeye alınmadan:

- [ ] Kullanıcı, problem ve beklenen değer açık.
- [ ] Story ID, öncelik ve MVP ilişkisi belli.
- [ ] Mutlu, hata ve sınır kabul kriterleri test edilebilir.
- [ ] UX akışı; loading, empty, error, offline ve permission durumlarını içeriyor.
- [ ] API/veri/migration bağımlılığı tanımlı.
- [ ] Auth, PII, finansal veri, deep link ve native izin etkisi değerlendirildi.
- [ ] Test yaklaşımı ve test verisi belirlendi.
- [ ] Belirsiz ürün/mimari kararı doğru sahip tarafından kapatıldı.
- [ ] Story bağımsız veya küçük PR'lara bölünebilir.

## Story Definition of Done (DoD)

- [ ] Kabul kriterlerinin tamamı geçti.
- [ ] Kod Clean Architecture ve Angular platform portu sınırlarına uyuyor.
- [ ] Yeni davranış unit/integration testleriyle kapsandı.
- [ ] Mevcut web davranışı regresyon testinden geçti.
- [ ] Hata mesajı, log ve telemetri hassas veri içermiyor.
- [ ] Accessibility ve küçük ekran kontrolü yapıldı.
- [ ] Performance/bundle etkisi kabul edilebilir veya ölçüldü.
- [ ] Gerekli doküman, migration ve rollback notu güncellendi.
- [ ] CI yeşil ve en az bir reviewer onayı var.
- [ ] Auth/PII/native izin değişikliğinde Security/CTO incelemesi tamamlandı.

## MVP ürün kabulü

### Hesap

- [ ] Kayıt, giriş, güvenli yenileme ve çıkış iOS/Android'de çalışıyor.
- [ ] Access token kalıcı depoya yazılmıyor; refresh token secure storage'da.
- [ ] Parola değişimi ve cihaz session iptali doğru çalışıyor.
- [ ] Web cookie/XSRF oturumu bozulmadı.

### Plan ve finansal işlemler

- [ ] Taksit ve ortak gider planları listeleniyor/açılıyor.
- [ ] Plan oluşturma, düzenleme ve desteklenen arşivleme akışları çalışıyor.
- [ ] Peşin/taksitli gider, pay yöntemleri ve ödeme bilgisi doğru kaydoluyor.
- [ ] Gider düzenleme/silme, transfer ve recurrence yetki sınırları doğru.
- [ ] Yeniden deneme veya çift dokunma mükerrer finansal kayıt üretmiyor.

### Fiş ve kamera

- [ ] Kamera ve fotoğraf arşivi izinleri amaç anında isteniyor.
- [ ] İptalde form açık ve girilmiş veri korunuyor.
- [ ] JPEG/PNG/WebP ve 8 MB doğrulaması istemci/sunucuda çalışıyor.
- [ ] AI sonucu yalnız taslak; kullanıcı onayı olmadan kayıt yok.
- [ ] AI servis hatasında manuel giriş kullanılabiliyor.

### Davet

- [ ] Universal Link ve App Link geçerli daveti uygulamada açıyor.
- [ ] Oturumsuz kullanıcı giriş/kayıt sonrası davete dönüyor.
- [ ] Kurulumsuz cihazda web fallback çalışıyor.
- [ ] Davet token'ı log/analytics/crash raporunda yok.

### Mobil kalite

- [ ] Safe-area, klavye, Android geri tuşu ve app lifecycle doğru.
- [ ] Offline cache salt-okunur, kullanıcıya ayrılmış ve çıkışta temizleniyor.
- [ ] En az WCAG AA hedefleri ve 44×44 px touch target sağlanıyor.
- [ ] Crash-free ve başlangıç performans hedefleri beta ölçümünde karşılanıyor.

## Release go/no-go

- [ ] QA kritik test sonucu `GO`.
- [ ] Security P0/P1 açık bulgu olmadığını doğruladı.
- [ ] CTO mimari ve migration/rollback hazırlığını onayladı.
- [ ] DevOps imzalı artefact, secret ve rollback akışını doğruladı.
- [ ] PM MVP kapsamı, metrikler ve mağaza içeriğini kabul etti.
- [ ] Gizlilik politikası, hesap/veri silme ve izin metinleri yayınlandı.
- [ ] TestFlight/Play Internal smoke testleri geçti.
- [ ] Production rollout açık kullanıcı onayıyla planlandı.

## MVP kapsam değişikliği kaydı

Yeni istek aşağıdakileri içeriyorsa PRD ve backlog güncellenmeden geliştirmeye alınmaz: push notification, biyometri, offline yazma, tablet özel tasarımı, banka entegrasyonu, çoklu para birimi veya yönetici ekranı.

