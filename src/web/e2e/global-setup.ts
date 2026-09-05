import { chromium, type FullConfig, type Page } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const apiUrl = process.env['E2E_API_URL'] ?? 'http://localhost:5096';

export const SHARED_AUTH_STATE = path.join(__dirname, '.auth', 'shared.json');

/**
 * API'nin auth uç noktaları IP başına dakikada 10 istekle sınırlı (Program.cs).
 * Her test kendi hesabını açsaydı üç kırılımda bu limit aşılırdı; bu yüzden
 * "oturum açmış kullanıcı" yeten testler burada üretilen tek hesabı ve onun
 * planlarını paylaşır.
 */
export default async function globalSetup(config: FullConfig): Promise<void> {
  await assertApiIsUp();

  const baseURL = config.projects[0]?.use?.baseURL ?? 'http://localhost:4200';
  await mkdir(path.dirname(SHARED_AUTH_STATE), { recursive: true });

  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ baseURL });

    // Yalnızca oturum: veri yazan testler kendi planını kendisi oluşturuyor,
    // böylece üç kırılım paralel koşarken birbirinin verisini bozmuyor.
    await register(page);
    await page.context().storageState({ path: SHARED_AUTH_STATE });
  } finally {
    await browser.close();
  }
}

async function register(page: Page): Promise<void> {
  await page.goto('/register');
  await page.locator('input[name="displayName"]').fill('E2E Paylaşılan');
  await page
    .locator('input[name="email"]')
    .fill(`e2e_shared_${Date.now()}_${Math.random().toString(36).slice(2, 8)}@example.com`);
  await page.locator('input[name="password"]').fill('E2eSifre123!');
  await page.locator('form button[type="submit"]').click();
  await page.waitForURL(/\/plans/, { timeout: 30_000 });
}


async function assertApiIsUp(): Promise<void> {
  let response: Response;
  try {
    response = await fetch(`${apiUrl}/health`, { signal: AbortSignal.timeout(10_000) });
  } catch (error) {
    throw new Error(
      `E2E testleri için API gerekli ama ${apiUrl}/health adresine ulaşılamadı.\n` +
        'Çalıştırın: docker compose up -d && dotnet run --project src/api/PayDefteri.Api\n' +
        `Ayrıntı: ${(error as Error).message}`
    );
  }

  if (!response.ok) {
    throw new Error(`API sağlıklı değil: ${apiUrl}/health -> HTTP ${response.status}`);
  }
}
