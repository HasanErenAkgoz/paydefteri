# PayDefteri — SEO Operasyon Rehberi

Canlı: https://paydefteri.com  
Teknik uygulama: landing + meta/OG + robots/sitemap + build-time prerender (`/`, `/login`, `/register`)

## Hedef anahtar kelimeler

1. ortak taksit takip  
2. ortak ödeme planı  
3. taksit mahsuplaşma  
4. borç takip uygulaması  
5. PayDefteri (marka)

## Kodda ne var?

| Öğe | Konum |
|---|---|
| Meta / OG / Twitter / canonical | `src/web/src/index.html` + `SeoService` |
| `robots.txt` | `src/web/public/robots.txt` |
| `sitemap.xml` | `src/web/public/sitemap.xml` |
| OG görseli | `src/web/public/og-image.png` |
| Landing | `/` → `features/landing` |
| Prerender | `app.routes.server.ts` (`RenderMode.Prerender`) |
| Auth sayfalar | `noindex` (route `data.seo`) |

## Google Search Console checklist

1. [Search Console](https://search.google.com/search-console) → property ekle: `https://paydefteri.com`
2. Domain doğrulama: Cloudflare DNS TXT **veya** HTML meta / dosya
3. Sitemaps → `https://paydefteri.com/sitemap.xml` gönder
4. URL Inspection → `https://paydefteri.com/` → **İndekslemeyi iste**
5. Aynı işlemi `/login` ve `/register` için isteğe bağlı tekrarla
6. 2–4 hafta izle: Performans → sorgular, tıklama, gösterim

## Yayın sonrası hızlı doğrulama

```bash
curl -sI https://paydefteri.com/ | head
curl -s https://paydefteri.com/robots.txt
curl -s https://paydefteri.com/sitemap.xml
curl -s https://paydefteri.com/ | rg -o "Ortak taksit|og:title|application/ld\\+json" | head
```

- [ ] Ana sayfada H1 ve meta description görünüyor  
- [ ] `robots.txt` Allow + Sitemap satırı doğru  
- [ ] `og-image.png` 200 dönüyor  
- [ ] LinkedIn paylaşım önizlemesi (post debugger) kontrol  

## Off-page (trafik / backlink)

- LinkedIn launch postu ([`docs/TANITIM.md`](TANITIM.md))  
- GitHub README’de ürün linki  
- İleride: “ortak taksit nasıl takip edilir” blog yazıları (2. dalga)

## Bilinçli sınırlar

- Runtime Node SSR yok; nginx static + prerender HTML  
- `/plans`, `/profile`, `/invite`, `/home` indeks dışı  
- Geniş “fintech Türkiye” head term’leri kısa vadede hedef değil
