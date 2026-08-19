import { test, expect, type Page } from '@playwright/test';

/**
 * Sprint 30 — Ask EdgePulse (natural-language Q&A).
 * Requires the full local stack incl. Ollama with llama3.2 pulled; the model
 * answer itself is not asserted (wording varies) — only that a grounded
 * answer or an honest "unavailable" state renders.
 */
const KEYCLOAK_USER = 'customeradmin';
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

test.describe('Sprint 30 — Ask EdgePulse', () => {
  test.beforeEach(async ({ page }) => { await login(page); });

  test('sidebar entry opens the Ask page with examples', async ({ page }) => {
    await page.getByRole('link', { name: /Ask EdgePulse/ }).click();
    await expect(page).toHaveURL(/\/ask$/);
    await expect(page.getByRole('button', { name: 'Which devices have open alerts right now?' })).toBeVisible();
  });

  test('asking a question renders a grounded answer (or honest unavailable)', async ({ page }) => {
    test.setTimeout(180_000);
    await page.goto('/ask');
    await page.getByRole('button', { name: 'Which devices have open alerts right now?' }).click();
    // the question bubble appears immediately
    await expect(page.getByText('Which devices have open alerts right now?').last()).toBeVisible();
    // then either a grounded answer or an unavailable reason (model down)
    const grounded = page.getByText(/Grounded on:/);
    const unavailable = page.getByText(/not enabled|did not return|not available/);
    await expect(grounded.or(unavailable)).toBeVisible({ timeout: 150_000 });
    await page.screenshot({ path: 'e2e-results/sprint30-ask.png', fullPage: true });
  });

  test('device detail offers "Ask about this device" and focuses the page', async ({ page }) => {
    await page.goto('/devices');
    await page.locator('table tbody tr, a[href^="/devices/"]').first().click();
    await expect(page).toHaveURL(/\/devices\/[0-9a-f-]{36}$/);
    await page.getByRole('link', { name: /Ask about this device/ }).click();
    await expect(page).toHaveURL(/\/ask\?deviceId=/);
    await expect(page.getByText(/Focused on /)).toBeVisible();
  });
});
