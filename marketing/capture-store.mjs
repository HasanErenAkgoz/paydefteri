// Play Store / App Store icin dikey telefon ekran goruntuleri uretir.
// Kullanim:
//   set -a && . ~/.paydefteri-play-review.env && set +a
//   node marketing/capture-store.mjs
// Cikti: marketing/screenshots/play/*.png (1080x2400)
import { chromium } from '../src/web/node_modules/playwright/index.mjs';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BASE = process.env.PD_BASE ?? 'https://paydefteri.com';
const EMAIL = process.env.PD_EMAIL;
const PASSWORD = process.env.PD_PASSWORD;
const OUT = path.join(__dirname, 'screenshots', 'play');

if (!EMAIL || !PASSWORD) {
  console.error('PD_EMAIL ve PD_PASSWORD gerekli.');
  process.exit(1);
}

fs.mkdirSync(OUT, { recursive: true });

const shot = async (page, name) => {
  await page.waitForTimeout(1200);
  await page.evaluate(() => document.fonts.ready);
  const file = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: file });
  console.log('kaydedildi:', path.relative(process.cwd(), file));
};

const go = async (page, pathname) => {
  await page.goto(`${BASE}${pathname}`, { waitUntil: 'networkidle' });
};

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  // 360x800 CSS px * 3 = 1080x2400 fiziksel piksel: Play'in bekledigi dikey format.
  viewport: { width: 360, height: 800 },
  deviceScaleFactor: 3,
  isMobile: true,
  hasTouch: true,
  locale: 'tr-TR',
  timezoneId: 'Europe/Istanbul',
});
const page = await context.newPage();

try {
  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle' });
  await page.fill('input[type="email"]', EMAIL);
  await page.fill('input[type="password"]', PASSWORD);
  await page.click('button:has-text("Giriş Yap")');
  await page.waitForURL(/\/(plans|home)/, { timeout: 20000 });
  await page.waitForLoadState('networkidle');

  // Plan kimliklerini oturum cerezleriyle API'den al: /plans son plana
  // yonlendirdigi icin dogrudan adrese gitmek tek guvenilir yol.
  const plans = await page.evaluate(() =>
    fetch('/api/plans', { credentials: 'include' }).then((r) => r.json())
  );
  const installment = plans.find((p) => p.planType === 'Installment');
  const expense = plans.find((p) => p.planType === 'Expense');
  if (!installment || !expense) {
    throw new Error('Demo planlar bulunamadi: once ornek veri olusturun.');
  }

  await go(page, '/plans');
  const plansTab = page.locator('nav a:has-text("Planlar"), a[href="/plans"]').last();
  if (await plansTab.count()) {
    await plansTab.click();
    await page.waitForLoadState('networkidle');
  }
  await shot(page, '01-planlar');

  await go(page, `/plans/${installment.id}/dashboard`);
  await shot(page, '02-taksit-takibi');

  await go(page, `/plans/${installment.id}/balances`);
  await shot(page, '03-bakiye');

  await go(page, `/plans/${expense.id}/expenses`);
  await shot(page, '04-ortak-giderler');

  await go(page, '/spending-analysis');
  await shot(page, '05-harcama-analizi');

  await go(page, '/profile');
  await shot(page, '06-profil');
} finally {
  await browser.close();
}
