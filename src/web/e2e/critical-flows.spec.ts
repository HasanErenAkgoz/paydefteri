import { Page, expect, test } from '@playwright/test';
import { SHARED_AUTH_STATE } from './global-setup';

/**
 * Denetimdeki P1: kritik akışların gerçek tarayıcı regresyonu, üç kırılımda.
 *
 * İzolasyon kuralı: API auth uç noktaları IP başına dakikada 10 istekle sınırlı
 * (Program.cs), bu yüzden hesap açmak pahalı. Buna karşılık plan oluşturmak
 * sınırsız — veri yazan testler bu yüzden ortak hesabı kullanır ama **kendi
 * planını** oluşturur. Böylece üç kırılım paralel koşarken birbirinin verisini
 * bozmaz.
 */

const TEST_PASSWORD = 'E2eSifre123!';

async function registerFreshAccount(page: Page): Promise<string> {
  const email = `e2e_${Date.now()}_${Math.random().toString(36).slice(2, 8)}@example.com`;

  await page.goto('/register');
  await page.locator('input[name="displayName"]').fill('E2E Kullanıcı');
  await page.locator('input[name="email"]').fill(email);
  await page.locator('input[name="password"]').fill(TEST_PASSWORD);
  await page.locator('form button[type="submit"]').click();
  await expect(page).toHaveURL(/\/plans/, { timeout: 30_000 });

  return email;
}

/** Şablon sekmesinden yeni bir plan açar ve ana ekranının yolunu döner. */
async function createOwnPlan(page: Page, kind: 'expense' | 'installment'): Promise<string> {
  await page.goto('/plans?manage=1');
  await page.getByRole('tab', { name: /Hazır Şablonlar/ }).click();
  await page.getByText(/Hazır Şablonlar & Örnek Senaryolar/).waitFor({ timeout: 30_000 });

  const card =
    kind === 'expense'
      ? page.locator('.template-card.expense-create-card').first()
      : page.locator('.template-card:not(.expense-create-card)').first();

  await card.getByRole('button', { name: /Seç & Aç/ }).click();
  await page.waitForURL(/\/plans\/[0-9a-f-]+\/(expenses|dashboard)/, { timeout: 30_000 });

  return new URL(page.url()).pathname;
}

test.describe('İlk kullanım akışı (yeni hesap)', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('kayıt → şablon seçimi → plan ana ekranı → gider araması', async ({ page }) => {
    await registerFreshAccount(page);

    // Onboarding düzeltmesinin regresyonu: otomatik oluşturulmuş boş bir planın
    // Kurulum ekranına değil, şablon seçilebilen listeye düşülmeli.
    await expect(page).not.toHaveURL(/\/setup/);
    await expect(page.getByText('Henüz planınız yok')).toBeVisible();

    await page.getByRole('button', { name: /Hazır Şablonları Gör/i }).click();
    await expect(page.getByText(/Hazır Şablonlar & Örnek Senaryolar/)).toBeVisible();

    const createButton = page.getByRole('button', { name: /Seç & Aç/ }).first();
    await expect(createButton).toBeVisible({ timeout: 30_000 });
    await createButton.click();

    await expect(page).toHaveURL(/\/plans\/[0-9a-f-]+\/(expenses|dashboard)/, { timeout: 30_000 });

    if (/\/expenses/.test(page.url())) {
      // Arama kutusunun masaüstü ve mobil kopyaları aynı anda DOM'da olabiliyor.
      const search = page.getByPlaceholder(/ara/i).filter({ visible: true }).first();
      await expect(search).toBeVisible();
      await search.fill('market');
      await expect(search).toHaveValue('market');
    }

    // Aynı taze hesapta (henüz ekstre yok) harcama analizi boş durumu:
    // format tutarsızlığı düzeltmesinin regresyonu. Ayrı bir teste bölmüyoruz,
    // çünkü her kayıt auth rate limit'inden (10/dk) pay yiyor.
    await page.goto('/spending-analysis');
    await expect(page.getByRole('heading', { name: 'Harcama Analizi' })).toBeVisible();
    await expect(page.getByText(/fotoğraf ve taranmış görsel kabul etmez/i)).toBeVisible();
  });
});

test.describe('Oturum içi ekranlar (paylaşılan hesap)', () => {
  test.use({ storageState: SHARED_AUTH_STATE });

  test('profil ekranı açılır ve sekmeler arasında geçiş yapılır', async ({ page }) => {
    await page.goto('/profile');
    await expect(page.getByRole('tablist', { name: /Profil ayarları/i })).toBeVisible();

    // Mobilde kısa etiket ("Güvenlik"), masaüstünde tam etiket ("Şifre & Güvenlik").
    const securityTab = page.getByRole('tab', { name: /Güvenlik/i });
    await securityTab.click();
    await expect(securityTab).toHaveAttribute('aria-selected', 'true');
  });

  test('gider ekleme: form doldurulur ve yeni gider listede görünür', async ({ page }, testInfo) => {
    await createOwnPlan(page, 'expense');

    const expenseName = `E2E ${testInfo.project.name} ${Date.now()}`;
    await page.getByRole('button', { name: /Gider Ekle/ }).first().click();

    const modal = page.getByRole('dialog').filter({ hasText: 'Yeni Gider Ekle' });
    await expect(modal).toBeVisible();
    // Tutar alanı type="number"; nokta ayraç bekliyor.
    await modal.getByPlaceholder('0,00').fill('123.45');
    await modal.getByPlaceholder(/Gider adı/).fill(expenseName);
    await modal.getByRole('button', { name: /Gideri Kaydet/ }).click();

    await expect(modal).toBeHidden({ timeout: 15_000 });
    // Masaüstü tablosu ve mobil kart listesi aynı anda DOM'da; kırılıma göre biri
    // gizli. Bu yüzden "ilk eşleşme" değil, "görünür eşleşme" aranıyor.
    await expect(
      page.getByText(expenseName).filter({ visible: true }).first()
    ).toBeVisible({ timeout: 15_000 });
  });

  test('ekstre önizleme: CSV yüklenir ve analiz sonucu render olur', async ({ page }, testInfo) => {
    await page.goto('/spending-analysis');

    // Her kırılım kendi işyeri adını yükler; paralel koşuda birbirine karışmaz.
    const merchant = `MARKET ${testInfo.project.name.toUpperCase()}`;
    const csv = [
      'Tarih;Açıklama;Tutar;Para Birimi',
      `01.08.2026;${merchant};1.250,50;TRY`,
      '03.08.2026;AKARYAKIT;850,00;TRY',
    ].join('\n');

    await page.locator('input[type="file"]').first().setInputFiles({
      name: `e2e-${testInfo.project.name}.csv`,
      mimeType: 'text/csv',
      buffer: Buffer.from(csv, 'utf8'),
    });

    await expect(page.getByText('Ekstre nabzı')).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText(merchant).first()).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Toplam harcama')).toBeVisible();
  });

  test('ödeme işaretleme: taksit ödemesi kaydedilir', async ({ page }) => {
    await createOwnPlan(page, 'installment');

    // Kırılıma göre masaüstü tablosu veya mobil kart listesi görünür olur.
    const payableCell = page
      .locator('button.check-btn:not([disabled])')
      .filter({ visible: true })
      .first();
    await expect(payableCell).toBeVisible({ timeout: 30_000 });
    await payableCell.click();

    const dialog = page.getByRole('dialog').filter({ hasText: /Ödeme/ }).first();
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Kaydet' }).click();

    await expect(dialog).toBeHidden({ timeout: 15_000 });
    await expect(
      page.locator('button.check-btn.checked').filter({ visible: true }).first()
    ).toBeVisible({ timeout: 15_000 });
  });

  test('sayfalar yatay taşma yapmadan render olur', async ({ page }) => {
    for (const path of ['/plans?manage=1', '/profile', '/spending-analysis']) {
      await page.goto(path);
      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth
      );
      expect(overflow, `${path} yatay taşma yapmamalı`).toBeLessThanOrEqual(1);
    }
  });
});
