import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const EMAIL = process.env.PD_EMAIL;
const PASSWORD = process.env.PD_PASSWORD;
const BASE = 'https://paydefteri.com';
const OUT = path.join(__dirname, 'social', 'assets', 'live');

if (!EMAIL || !PASSWORD) {
  console.error('Set PD_EMAIL and PD_PASSWORD');
  process.exit(1);
}

fs.mkdirSync(OUT, { recursive: true });

async function shot(page, name, opts = {}) {
  const file = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: file, fullPage: !!opts.fullPage });
  console.log('saved', name, page.url());
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    locale: 'tr-TR',
    deviceScaleFactor: 2,
  });
  const page = await context.newPage();

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle', timeout: 60000 });
  await page.fill('input[name="email"]', EMAIL);
  await page.fill('input[name="password"]', PASSWORD);
  await Promise.all([
    page.waitForURL((u) => !u.pathname.includes('/login'), { timeout: 30000 }),
    page.click('button[type="submit"]'),
  ]);
  await page.waitForTimeout(2000);

  const dashUrl = page.url();
  const m = dashUrl.match(/\/plans\/([0-9a-f-]+)/i);
  const planId = m?.[1];
  console.log('planId', planId, 'url', dashUrl);

  await shot(page, '02-dashboard');

  // Scroll to table mid-section
  await page.evaluate(() => window.scrollTo(0, 520));
  await page.waitForTimeout(600);
  await shot(page, '02b-dashboard-table');

  await page.goto(`${BASE}/plans`, { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(1500);
  await shot(page, '03-plans');

  if (planId) {
    await page.goto(`${BASE}/plans/${planId}/setup`, { waitUntil: 'networkidle', timeout: 45000 });
    await page.waitForTimeout(2000);
    await shot(page, '05-setup');
    await page.evaluate(() => window.scrollTo(0, 500));
    await page.waitForTimeout(500);
    await shot(page, '05b-setup-scroll');

    await page.goto(`${BASE}/plans/${planId}/data`, { waitUntil: 'networkidle', timeout: 45000 });
    await page.waitForTimeout(2000);
    await shot(page, '06-data');
  }

  await page.goto(`${BASE}/`, { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(1000);
  await shot(page, '00-landing');

  await page.goto(`${BASE}/login`, { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(800);
  await shot(page, '01-login');

  // Mobile dashboard
  await page.setViewportSize({ width: 390, height: 844 });
  // re-login may still have token
  if (planId) {
    await page.goto(`${BASE}/plans/${planId}/dashboard`, { waitUntil: 'networkidle', timeout: 45000 });
    await page.waitForTimeout(2000);
    await shot(page, '09-dashboard-mobile');
  }

  await browser.close();
  console.log('done');
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
