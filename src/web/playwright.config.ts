import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env['E2E_BASE_URL'] ?? 'http://localhost:4200';

/**
 * Denetimdeki P1 için uçtan uca regresyon paketi: kritik akışlar üç kırılımda
 * (375 / 768 / 1440) gerçek tarayıcıda koşar.
 *
 * Ön koşul: API ayakta olmalı (varsayılan http://localhost:5096). Angular sunucusu
 * çalışmıyorsa Playwright kendisi başlatır.
 */
export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  // Görsel testler yalnızca baseline'ların üretildiği ortamda (CI ile aynı Linux
  // imajı, scripts/e2e-visual.sh) anlamlı; varsayılan koşuda dışlanır.
  grepInvert: process.env['E2E_VISUAL'] ? undefined : /@visual/,
  // Her test kendi hesabını açtığı için paralel koşabilirler.
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? [['github'], ['html', { open: 'never' }]] : [['list']],
  timeout: 60_000,
  expect: {
    timeout: 10_000,
    // Görsel baseline'lar platforma göre değişir; anlamlı olması için CI ile aynı
    // ortamda üretilmeli. Küçük anti-aliasing farklarını tolere ediyoruz.
    toHaveScreenshot: { maxDiffPixelRatio: 0.02 },
  },
  use: {
    baseURL,
    locale: 'tr-TR',
    timezoneId: 'Europe/Istanbul',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'mobile-375',
      use: { ...devices['Desktop Chrome'], viewport: { width: 375, height: 812 } },
    },
    {
      name: 'tablet-768',
      use: { ...devices['Desktop Chrome'], viewport: { width: 768, height: 1024 } },
    },
    {
      name: 'desktop-1440',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } },
    },
  ],
  webServer: {
    command: 'npm start',
    url: baseURL,
    reuseExistingServer: !process.env['CI'],
    timeout: 180_000,
  },
});
