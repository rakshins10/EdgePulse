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
    // Already logged in
  }
  await page.waitForURL(/localhost:3000\/dashboard/, { timeout: 20_000 });
}

test.describe('Sprint 14 — CRUD UI', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('Mills: Edit modal opens, edits name, persists', async ({ page }) => {
    await page.goto('/mills');
    // Wait for at least one mill to render
    await expect(page.getByRole('button', { name: '+ Add Mill' })).toBeVisible({ timeout: 10_000 });

    // Tampere card — find by its unique location text, walk up to the card root
    const tampereLocation = page.getByText('Tampere, Finland', { exact: false }).first();
    await expect(tampereLocation).toBeVisible();
    const card = tampereLocation.locator('xpath=ancestor::*[contains(@class, "card")][1]');
    await card.locator('button[title="Edit mill"]').click();

    // Modal form
    const nameInput = page.locator('form#mill-form input').first();
    await expect(nameInput).toBeVisible({ timeout: 5_000 });
    const original = await nameInput.inputValue();
    const stamp = Date.now();
    const newName = `${original} ★${stamp}`;
    await nameInput.fill(newName);
    await page.getByRole('button', { name: 'Save Changes' }).click();

    await expect(page.locator('form#mill-form')).not.toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(`★${stamp}`, { exact: false })).toBeVisible({ timeout: 10_000 });
    console.log(`  Updated mill name to "${newName}"`);

    // Restore
    await page.getByText(`★${stamp}`, { exact: false }).locator('xpath=ancestor::*[contains(@class, "card")][1]')
      .locator('button[title="Edit mill"]').click();
    await page.locator('form#mill-form input').first().fill(original);
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await expect(page.locator('form#mill-form')).not.toBeVisible({ timeout: 10_000 });
    console.log(`  Restored original name`);
  });

  test('Mills: Delete on a mill with children is blocked', async ({ page }) => {
    await page.goto('/mills');
    await expect(page.getByRole('button', { name: '+ Add Mill' })).toBeVisible({ timeout: 10_000 });

    // Pick the Lakewood (Finland) mill — has active devices
    const card = page.getByText('Lakewood, Finland').first()
      .locator('xpath=ancestor::*[contains(@class, "card")][1]');

    // confirm() then alert() both accepted
    let alertText = '';
    page.on('dialog', async d => {
      if (d.type() === 'alert') alertText = d.message();
      await d.accept();
    });

    await card.locator('button[title="Delete mill"]').click();
    await page.waitForTimeout(2000);

    // Mill still present
    await expect(page.getByText('Lakewood, Finland').first()).toBeVisible();
    expect(alertText.toLowerCase()).toContain('active');
    console.log(`  Delete blocked. Alert: "${alertText}"`);
  });

  test('Devices: Edit modal saves name, then restore', async ({ page }) => {
    await page.goto('/devices');
    const firstRow = page.locator('tbody tr').first();
    await expect(firstRow.locator('button:has-text("Edit")')).toBeVisible({ timeout: 10_000 });
    const originalName = (await firstRow.locator('td').first().innerText()).split('\n')[0].trim();

    await firstRow.locator('button:has-text("Edit")').click();
    const nameInput = page.locator('form#edit-form input').first();
    await expect(nameInput).toBeVisible({ timeout: 5_000 });

    const stamp = Date.now();
    const newName = `${originalName} ★${stamp}`;
    await nameInput.fill(newName);
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await expect(page.locator('form#edit-form')).not.toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(`★${stamp}`, { exact: false }).first()).toBeVisible({ timeout: 10_000 });
    console.log(`  Device renamed to "${newName}"`);

    // Restore
    await page.getByText(`★${stamp}`).locator('xpath=ancestor::tr[1]').locator('button:has-text("Edit")').click();
    await page.locator('form#edit-form input').first().fill(originalName);
    await page.getByRole('button', { name: 'Save Changes' }).click();
    await expect(page.locator('form#edit-form')).not.toBeVisible({ timeout: 10_000 });
    console.log(`  Restored "${originalName}"`);
  });

  test('Configuration: Location Types CRUD UI loads', async ({ page }) => {
    await page.goto('/configuration');
    await page.getByRole('button', { name: 'Location Types' }).click();
    await expect(page.getByRole('button', { name: /\+ Add Location Type/i })).toBeVisible({ timeout: 10_000 });
    await page.getByRole('button', { name: /\+ Add Location Type/i }).click();
    await expect(page.locator('form#loc-form')).toBeVisible({ timeout: 5_000 });
    await page.getByRole('button', { name: 'Cancel' }).click();
    console.log('  Configuration → Location Types CRUD UI works');
  });
});
