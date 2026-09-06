// Play Store feature graphic (1024x500) uretir.
// Kullanim: node marketing/capture-feature-graphic.mjs
// Cikti: marketing/screenshots/play/feature-graphic.png
import { chromium } from '../src/web/node_modules/playwright/index.mjs';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.join(__dirname, 'screenshots', 'play');
fs.mkdirSync(OUT, { recursive: true });

const iconPath = path.join(__dirname, '..', 'src', 'web', 'public', 'brand-icon.png');
const icon = `data:image/png;base64,${fs.readFileSync(iconPath).toString('base64')}`;

const html = `<!doctype html>
<html lang="tr">
<head>
<meta charset="utf-8" />
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    width: 1024px; height: 500px; overflow: hidden;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    background: radial-gradient(120% 140% at 12% 15%, #4c1d95 0%, #1e1b4b 42%, #0b1120 100%);
    color: #f8fafc; display: flex; align-items: center; gap: 44px; padding: 0 68px;
  }
  .glow {
    position: absolute; width: 520px; height: 520px; right: -140px; top: -180px;
    background: radial-gradient(circle, rgba(139,92,246,0.45) 0%, rgba(139,92,246,0) 68%);
  }
  .icon {
    width: 168px; height: 168px; border-radius: 38px; flex: none;
    box-shadow: 0 18px 48px rgba(76, 29, 149, 0.55);
  }
  .brand { font-size: 30px; font-weight: 700; letter-spacing: 0.06em; color: #c4b5fd; text-transform: uppercase; }
  h1 { font-size: 58px; line-height: 1.08; font-weight: 800; margin: 10px 0 16px; }
  h1 span { color: #a78bfa; }
  p { font-size: 25px; line-height: 1.45; color: rgba(226,232,240,0.82); max-width: 560px; }
  .pills { display: flex; gap: 12px; margin-top: 26px; }
  .pill {
    padding: 9px 18px; border-radius: 999px; font-size: 19px; font-weight: 600;
    border: 1px solid rgba(167,139,250,0.45); background: rgba(139,92,246,0.14); color: #ddd6fe;
  }
</style>
</head>
<body>
  <div class="glow"></div>
  <img class="icon" src="${icon}" alt="" />
  <div>
    <div class="brand">PayDefteri</div>
    <h1>Ortak plan,<br /><span>tek defter</span></h1>
    <p>Taksitleri ve ortak giderleri birlikte takip edin; kim ne ödedi, kalan pay ne, tek bakışta.</p>
    <div class="pills">
      <div class="pill">Taksit takibi</div>
      <div class="pill">Ortak gider</div>
      <div class="pill">Mahsuplaşma</div>
    </div>
  </div>
</body>
</html>`;

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage({ viewport: { width: 1024, height: 500 }, deviceScaleFactor: 1 });
await page.setContent(html, { waitUntil: 'load' });
await page.evaluate(() => document.fonts.ready);
const file = path.join(OUT, 'feature-graphic.png');
await page.screenshot({ path: file });
console.log('kaydedildi:', path.relative(process.cwd(), file));
await browser.close();
