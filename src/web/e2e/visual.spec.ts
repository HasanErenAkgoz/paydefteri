import { expect, test } from '@playwright/test';
import { SHARED_AUTH_STATE } from './global-setup';

/**
 * Görsel regresyon baseline'ları (denetimdeki P1'in "görsel baseline" maddesi).
 *
 * Bu testler `@visual` etiketli ve varsayılan koşuda dışlanıyor; çünkü Playwright
 * ekran görüntülerini platforma göre adlandırıyor (…-linux.png / …-darwin.png) ve
 * baseline'lar CI ile aynı ortamda üretilmeli. Üretmek/doğrulamak için:
 *
 *   scripts/e2e-visual.sh --update-snapshots   # baseline üret
 *   scripts/e2e-visual.sh                      # baseline'a karşı doğrula
 *
 * Yalnızca deterministik ekranlar alınıyor: tarih/tutar/e-posta gibi değişken
 * içerik ya yok ya da maskeleniyor.
 */

const snapshotOptions = {
  fullPage: true,
  animations: 'disabled',
} as const;

test.describe('Görsel regresyon @visual', () => {
  test.describe('Herkese açık ekranlar', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('landing @visual', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot('landing.png', snapshotOptions);
    });

    test('giriş ekranı @visual', async ({ page }) => {
      await page.goto('/login');
      await page.waitForLoadState('networkidle');
      await expect(page).toHaveScreenshot('login.png', snapshotOptions);
    });
  });

  test.describe('Oturum içi ekranlar', () => {
    test.use({ storageState: SHARED_AUTH_STATE });

    test('profil @visual', async ({ page }) => {
      await page.goto('/profile');
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('tablist', { name: /Profil ayarları/i })).toBeVisible();

      // Profil verisi asenkron geliyor; alan dolmadan alınan görüntü paralel
      // koşuda kararsızlık üretiyordu. Yerleşim oturana kadar bekle.
      await expect(page.locator('input[name="displayName"]')).not.toHaveValue('', {
        timeout: 15_000,
      });

      // Hesaba özgü alanlar (ad, e-posta) her koşuda değişir; maskeliyoruz.
      await expect(page).toHaveScreenshot('profile.png', {
        ...snapshotOptions,
        mask: [page.locator('input[name="displayName"]'), page.locator('.profile-email')],
      });
    });
  });
});
