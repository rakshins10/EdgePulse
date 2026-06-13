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
  } catch { /* already logged in */ }
  await page.waitForURL(/localhost:3000\/dashboard/, { timeout: 20_000 });
}

test.describe('Sprint 15B — CSV round-trip + new-language pre-fill', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('Add German with pre-fill, export CSV, then it appears in switcher', async ({ page }) => {
    // Server requires code ^[a-z]{2,3}(-..)?$ — use a throwaway 2-letter code.
    const shortCode = 'zz';

    await page.goto('/configuration');
    await page.getByRole('button', { name: /Languages|Kielet|Språk/i }).click();

    // Open Add Language
    await page.getByRole('button', { name: /\+ Add Language|\+ Lisää kieli|\+ Lägg till språk/i }).click();
    await expect(page.locator('form#lang-form')).toBeVisible({ timeout: 5_000 });

    // Fill — code zz (test), Display "QA Test", Native "QA". Pre-fill checkbox is on by default.
    await page.locator('form#lang-form input').first().fill(shortCode);
    const inputs = page.locator('form#lang-form input');
    // inputs: [code, flag, displayName, nativeName, sortOrder, enabled(checkbox), prefill(checkbox)]
    await page.locator('form#lang-form input[placeholder="German"]').fill('QA Test');
    await page.locator('form#lang-form input[placeholder="Deutsch"]').fill('QA');

    await page.getByRole('button', { name: /^Create$|^Luo$|^Skapa$/ }).click();

    // Modal closes after create (+ prefill)
    await expect(page.locator('form#lang-form')).not.toBeVisible({ timeout: 20_000 });

    // Row for zz appears
    await expect(page.getByRole('cell', { name: shortCode, exact: true })).toBeVisible({ timeout: 10_000 });
    console.log(`  Created locale "${shortCode}" with pre-fill`);

    // Export CSV for zz via the IO panel — verify a download happens
    // Select zz in the IO panel locale dropdown (the last select on the page)
    const ioSelect = page.locator('select').last();
    await ioSelect.selectOption(shortCode);

    const downloadPromise = page.waitForEvent('download', { timeout: 15_000 });
    await page.getByRole('button', { name: /Export CSV|Vie CSV|Exportera CSV/i }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toContain(shortCode);
    console.log(`  Exported CSV: ${download.suggestedFilename()}`);

    // The new locale should now appear in the top-bar switcher menu
    await page.locator('header button[aria-haspopup="listbox"]').click();
    await expect(
      page.locator('[role="listbox"]').getByRole('option', { name: /QA/i })
    ).toBeVisible({ timeout: 5_000 });

    // Cleanup: delete the test locale
    await page.keyboard.press('Escape');
    await page.getByRole('button', { name: /Languages|Kielet|Språk/i }).click();
    const row = page.getByRole('cell', { name: shortCode, exact: true }).locator('xpath=ancestor::tr[1]');
    page.once('dialog', d => d.accept());
    await row.getByRole('button', { name: /Delete|Poista|Ta bort/i }).click();
    await expect(page.getByRole('cell', { name: shortCode, exact: true })).toHaveCount(0, { timeout: 10_000 });
    console.log('  Cleaned up test locale');
  });
});
