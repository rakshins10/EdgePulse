import { test, expect, type Page } from '@playwright/test';

const KEYCLOAK_USER = 'superadmin';
const KEYCLOAK_PASS = 'Test@1234';

async function login(page: Page) {
  await page.goto('/');
  try {
    await page.waitForURL(/localhost:8080/, { timeout: 10_000 });
    await page.fill('#username', KEYCLOAK_USER);
    await page.fill('#password', KEYCLOAK_PASS);
    await page.click('#kc-login');
  } catch {
    // already logged in
  }
  await page.waitForURL(/localhost:3000\/dashboard/, { timeout: 20_000 });
}

test.describe('Sprint 15 — i18n + data-driven locales', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('Language switcher lists API locales and switches UI language', async ({ page }) => {
    // Open the switcher in the topbar
    await page.locator('header button[aria-haspopup="listbox"]').click();
    // Finnish should be offered (seeded + enabled)
    const fi = page.getByRole('option', { name: /Suomi/i });
    await expect(fi).toBeVisible({ timeout: 5_000 });
    await fi.click();

    // Sidebar nav should now be Finnish ("Laitteet" = Devices)
    await expect(page.getByRole('link', { name: /Laitteet/i })).toBeVisible({ timeout: 5_000 });

    // Switch back to English so other tests/users see a known state
    await page.locator('header button[aria-haspopup="listbox"]').click();
    await page.getByRole('option', { name: /English/i }).click();
    await expect(page.getByRole('link', { name: /Devices/i })).toBeVisible({ timeout: 5_000 });
  });

  test('Configuration → Languages tab lists seeded locales', async ({ page }) => {
    await page.goto('/configuration');
    await page.getByRole('button', { name: /Languages|Kielet|Språk/i }).click();
    // The three seeded locales appear by code
    await expect(page.getByRole('cell', { name: 'en', exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('cell', { name: 'fi', exact: true })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'sv', exact: true })).toBeVisible();
  });

  test('Translations tab: set a Finnish DeviceType translation, persists', async ({ page }) => {
    await page.goto('/configuration');
    await page.getByRole('button', { name: /Translations|Käännökset|Översättningar/i }).click();

    // Selectors: lookup type (DeviceType default) + locale. Pick Finnish.
    const selects = page.locator('select');
    await expect(selects.first()).toBeVisible({ timeout: 10_000 });
    // Second select is the locale picker
    await selects.nth(1).selectOption('fi');

    // First translation input — type a value, blur to save
    const firstInput = page.locator('tbody tr td input').first();
    await expect(firstInput).toBeVisible({ timeout: 10_000 });
    const stamp = Date.now();
    const val = `Testi ${stamp}`;
    await firstInput.fill(val);
    await firstInput.blur();

    // Saved indicator appears
    await expect(page.getByText(/Saved|Tallennettu|Sparad/i).first()).toBeVisible({ timeout: 10_000 });

    // Reload the tab — value should persist
    await page.reload();
    await page.getByRole('button', { name: /Translations|Käännökset|Översättningar/i }).click();
    await page.locator('select').nth(1).selectOption('fi');
    await expect(page.locator('tbody tr td input').first()).toHaveValue(val, { timeout: 10_000 });

    // Clean up: clear the translation
    const input = page.locator('tbody tr td input').first();
    await input.fill('');
    await input.blur();
    await page.waitForTimeout(1000);
    console.log(`  Finnish translation set to "${val}" and persisted`);
  });
});
