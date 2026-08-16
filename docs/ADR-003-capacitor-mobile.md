# ADR-003: Angular + Capacitor ile mobil uygulama

- **Durum:** Kabul önerisi
- **Tarih / sahipler:** 13 Ağustos 2026 — CTO, Frontend, Security, DevOps
- **İlgili belgeler:** [Mobil PRD](./MOBILE-PRD.md), [Teknik Tasarım](./MOBILE-TECHNICAL-DESIGN.md), [Uygulama Planı](./mobile/IMPLEMENTATION-PLAN.md)

## Bağlam

PayDefteri Angular 19 web arayüzü ve ASP.NET Core 8 API ile çalışır. Mobil uygulamanın görsel tasarımı, temel ekranları, form davranışı ve iş verisi web ile aynı olacaktır. Kamera, secure storage, deep link, safe-area, klavye ve native dağıtım desteği gereklidir.

## Karar sürücüleri

- Mevcut Angular bileşen ve tasarım yatırımını korumak.
- Tek ekip ve tek UI kod tabanıyla hızlı MVP teslimi.
- Web ile mobil davranış farklılığını ve bakım maliyetini azaltmak.
- Kamera/deep link/secure storage gibi native yeteneklere kontrollü erişmek.
- iOS/Android mağaza dağıtımını desteklemek.

## Seçenekler

1. **Angular + Capacitor** — En yüksek kod/tasarım paylaşımı ve hızlı teslim; WebView davranışı ve native köprü bakımı gerekir.
2. **Flutter** — Güçlü native-benzeri UI ve tek mobil kod; mevcut Angular UI tamamen yeniden yazılır, iki frontend oluşur.
3. **React Native** — Geniş ekosistem; mevcut Angular bilgi/bileşenleri yeniden kullanılamaz, ikinci frontend gerekir.
4. **Yalnız PWA** — En düşük efor; mağaza/native secure storage, platform deep link ve kamera deneyimi üzerinde daha az kontrol.

## Karar ve gerekçe

Angular arayüzü Capacitor ile paketlenecektir. Ionic UI zorunlu olmayacak; mevcut PayDefteri tasarım sistemi korunacaktır. Native yetenekler `core/platform` portları ve web/Capacitor adaptörleri üzerinden kullanılacaktır.

Bu karar MVP'nin “tasarım aynı” ve hızlı teslim hedefini en düşük yeniden yazım riskiyle karşılar. Finansal iş kuralları istemcide çoğaltılmayacak, mevcut API esas alınacaktır.

## Sonuçlar ve trade-off'lar

- Angular feature bileşenlerinin çoğu ortak kalır.
- `ios/` ve `android/` native projeleri, signing ve mağaza süreçleri ek bakım ister.
- WebView, klavye, back button, safe-area ve cihaz permission farkları gerçek cihaz testini zorunlu kılar.
- Native API çağrıları doğrudan feature bileşenlerine yayılmaz; port/adaptör sınırı korunur.
- Performans sorunu ölçülürse yalnız sorunlu ekran/özellik için native alternatif değerlendirilir; baştan tam yeniden yazım yapılmaz.

## Auth kararı

Web HttpOnly cookie + XSRF akışı korunur. Native uygulama kısa ömürlü Bearer access token ve Keychain/Keystore'da saklanan, dönen opaque refresh token kullanır. Access token kalıcı storage'a yazılmaz. Yalnız mobil auth endpoint'leri `/api/mobile/v1/auth` altında versiyonlanır.

## Migration ve rollback

1. Capacitor aynı Angular kaynağına additive eklenir; web build yolu korunur.
2. Mobil auth entity/endpoint'leri mevcut web auth'u değiştirmeden eklenir.
3. API değişiklikleri eski web ve en az bir önceki mobil sürümle geriye uyumlu kalır.
4. Mobil sorununda mağaza rollout'u durdurulur; web uygulaması bağımsız çalışmaya devam eder.
5. Capacitor yaklaşımı başarısız olursa platform portları yeni native istemciye geçiş sınırı sağlar.

## Güvenlik, gizlilik ve operasyon etkisi

- Native secure storage ve token rotation/replay tespiti gerektirir.
- Uygulama paketine API anahtarı/secret konmaz.
- Fiş görselleri ve finansal veri log/analytics'e girmez.
- iOS/Android signing secret'ları CI vault'ta tutulur.
- TestFlight/Play staged rollout, crash/başarı metrikleri ve release rollback runbook'u zorunludur.

## Yeniden değerlendirme tetikleyicileri

- Ölçülmüş kritik WebView performans/erişilebilirlik sorunu.
- Ürünün yoğun arka plan çalışma, Bluetooth/NFC veya gelişmiş native UI gerektirmesi.
- Native'e özel ekranların paylaşılan ekranlardan daha büyük hale gelmesi.
- Mağaza politikası ya da Capacitor desteğinin hedef platformları karşılamaması.

