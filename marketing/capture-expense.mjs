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

async function shot(page, name) {
  const file = path.join(OUT, `${name}.png`);
  await page.screenshot({ path: file });
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
  console.log('landed', page.url());

  // Go to plans list
  await page.goto(`${BASE}/plans`, { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForTimeout(2000);
  await shot(page, 'expense-00-plans');

  // Find expense plan card / link
  const expenseBadge = page.locator('.plan-type-badge.expense, :text("Gider"), :text("Ortak Gider")').first();
  const expenseCard = page.locator('.plan-card').filter({ has: page.locator('.plan-type-badge.expense') }).first();

  let expenseUrl = null;
  if (await expenseCard.count()) {
    const href = await expenseCard.locator('a').first().getAttribute('href');
    console.log('expense card href', href);
    if (href) {
      await page.goto(href.startsWith('http') ? href : `${BASE}${href}`, {
        waitUntil: 'networkidle',
        timeout: 45000,
      });
    } else {
      await expenseCard.click();
    }
  } else {
    // Try any link containing /expenses
    const expLink = page.locator('a[href*="/expenses"]').first();
    if (await expLink.count()) {
      await expLink.click();
    } else {
      // Create sample couple expense plan from UI if available
      const sampleBtn = page.locator('button:has-text("örnek"), button:has-text("Örnek"), button:has-text("Gider Planı")').first();
      console.log('no expense plan found, buttons:', await page.locator('button, a').evaluateAll((els) =>
        els.map((e) => (e.innerText || '').trim()).filter(Boolean).slice(0, 40)
      ));
      if (await sampleBtn.count()) {
        await sampleBtn.click();
        await page.waitForTimeout(2000);
      }
    }
  }

  await page.waitForTimeout(2500);
  console.log('after select', page.url());

  // If still not on expenses, try to navigate from current plan
  let url = page.url();
  const planMatch = url.match(/\/plans\/([0-9a-f-]+)/i);
  if (planMatch && !url.includes('/expenses')) {
    // Check if this is expense or installment - try expenses route
    const tryExp = `${BASE}/plans/${planMatch[1]}/expenses`;
    await page.goto(tryExp, { waitUntil: 'networkidle', timeout: 30000 }).catch(() => {});
    await page.waitForTimeout(1500);
    url = page.url();
  }

  // List all plan ids from API via localStorage token
  const token = await page.evaluate(() => {
    for (const k of Object.keys(localStorage)) {
      const v = localStorage.getItem(k);
      if (v && (v.startsWith('eyJ') || v.includes('accessToken'))) return v;
    }
    return null;
  });
  console.log('token found', !!token);

  // Fetch plans via page request with cookies
  const plansJson = await page.evaluate(async () => {
    const res = await fetch('/api/plans', { credentials: 'include' });
    if (!res.ok) {
      // try with bearer from storage
      let auth = null;
      for (const k of Object.keys(localStorage)) {
        try {
          const raw = localStorage.getItem(k);
          if (!raw) continue;
          if (raw.startsWith('eyJ')) {
            auth = raw;
            break;
          }
          const parsed = JSON.parse(raw);
          if (parsed?.accessToken || parsed?.token) {
            auth = parsed.accessToken || parsed.token;
            break;
          }
        } catch {}
      }
      const headers = auth ? { Authorization: `Bearer ${auth}` } : {};
      const res2 = await fetch('/api/plans', { headers, credentials: 'include' });
      return { status: res2.status, body: await res2.text() };
    }
    return { status: res.status, body: await res.text() };
  });
  console.log('plans api', plansJson.status, plansJson.body.slice(0, 500));

  let plans = [];
  try {
    plans = JSON.parse(plansJson.body);
  } catch {}

  const expensePlan = Array.isArray(plans)
    ? plans.find((p) => p.planType === 'Expense' || p.planType === 1)
    : null;

  if (expensePlan) {
    console.log('expense plan', expensePlan.id, expensePlan.title);
    await page.goto(`${BASE}/plans/${expensePlan.id}/expenses`, {
      waitUntil: 'networkidle',
      timeout: 45000,
    });
    await page.waitForTimeout(2500);
    await shot(page, 'expense-01-table');

    await page.evaluate(() => window.scrollTo(0, 420));
    await page.waitForTimeout(600);
    await shot(page, 'expense-01b-table-scroll');

    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(400);
    // Open add form if button exists
    const addBtn = page.locator('button:has-text("Gider ekle")').first();
    if (await addBtn.count()) {
      await addBtn.click();
      await page.waitForTimeout(1000);
      await shot(page, 'expense-02-add-form');
    }

    await page.goto(`${BASE}/plans/${expensePlan.id}/data`, {
      waitUntil: 'networkidle',
      timeout: 45000,
    });
    await page.waitForTimeout(2000);
    await shot(page, 'expense-03-report');

    await page.goto(`${BASE}/plans/${expensePlan.id}/setup`, {
      waitUntil: 'networkidle',
      timeout: 45000,
    });
    await page.waitForTimeout(2000);
    await shot(page, 'expense-04-setup');
  } else {
    console.log('No expense plan — creating via API');
    // Create couple sample via UI on plans page
    await page.goto(`${BASE}/plans`, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1500);
    const createSample = page.locator('button:has-text("Örnekle aç"), button:has-text("örnekle"), button:has-text("Örnek")').first();
    const allText = await page.locator('button').evaluateAll((els) =>
      els.map((e) => (e.innerText || '').trim()).filter(Boolean)
    );
    console.log('plan page buttons', allText);
    if (await createSample.count()) {
      await createSample.click();
      await page.waitForTimeout(3000);
      await shot(page, 'expense-01-table');
    } else {
      await shot(page, 'expense-99-debug');
    }
  }

  // Mobile
  await page.setViewportSize({ width: 390, height: 844 });
  if (expensePlan) {
    await page.goto(`${BASE}/plans/${expensePlan.id}/expenses`, {
      waitUntil: 'networkidle',
      timeout: 45000,
    });
    await page.waitForTimeout(2000);
    await shot(page, 'expense-05-mobile');
  }

  await browser.close();
  console.log('done');
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
