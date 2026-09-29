import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import { ConfirmEmailPage, LoginPage, RegisterPage, TwoFactorPage } from './pages/AuthPages';
import { extractConfirmationLink, extractTwoFactorCode, waitForMailpitMessage } from './support/mailpit';
import { purchaseRequestFixture, seedPurchaseRequestFixture } from './support/purchaseRequestFixture';

const password = 'E2e.Password.123!';

async function registerAndConfirm(
  page: Page,
  request: APIRequestContext,
  email: string,
) {
  const registerPage = new RegisterPage(page);
  await registerPage.open();
  await registerPage.register(email, password);

  const confirmationMessage = await waitForMailpitMessage(request, email, 'Confirm');
  await new ConfirmEmailPage(page).open(extractConfirmationLink(confirmationMessage));
}

async function signIn(
  page: Page,
  request: APIRequestContext,
  email: string,
) {
  const loginPage = new LoginPage(page);
  await loginPage.open();
  await loginPage.login(email, password);

  const twoFactorMessage = await waitForMailpitMessage(request, email, 'verification');
  await new TwoFactorPage(page).verify(
    extractTwoFactorCode(twoFactorMessage),
    /\/(dashboard|purchase-requests)(\/.*)?$/,
  );
}

async function signOut(page: Page) {
  await page.getByRole('button', { name: /logout/i }).click();
  await expect(page).toHaveURL(/\/login$/);
}

test('completes the critical purchase request workflow across business roles', async ({ page, request }) => {
  test.setTimeout(120_000);

  const suffix = Date.now().toString();
  const employeeEmail = `purchase-employee-${suffix}@example.test`;
  const managerEmail = `purchase-manager-${suffix}@example.test`;
  const procurementEmail = `purchase-procurement-${suffix}@example.test`;
  const note = `Critical purchase ${suffix}`;

  await registerAndConfirm(page, request, employeeEmail);
  await registerAndConfirm(page, request, managerEmail);
  await registerAndConfirm(page, request, procurementEmail);
  seedPurchaseRequestFixture({ employeeEmail, managerEmail, procurementEmail });

  await signIn(page, request, employeeEmail);
  await page.goto('/purchase-requests');
  await expect(page.getByRole('heading', { name: 'Purchase request drafts' })).toBeVisible();

  await page.getByLabel('Note').fill(note);
  await page.getByRole('button', { name: 'Create draft' }).click();
  await expect(page.getByRole('heading', { name: note })).toBeVisible();

  await page.getByLabel('Product').selectOption(purchaseRequestFixture.productId);
  await page.getByLabel('Quantity').fill('2');
  await page.getByRole('button', { name: 'Add item' }).click();
  await expect(page.locator('.purchase-request-item').filter({ hasText: 'E2E Monitor' })).toBeVisible();
  await page.getByRole('button', { name: 'Submit request' }).click();
  await expect(page.getByRole('status')).toContainText('Purchase request submitted.');
  await expect(page.locator('.role-badge').filter({ hasText: 'Submitted' })).toBeVisible();
  await signOut(page);

  await signIn(page, request, managerEmail);
  await page.goto('/purchase-requests/approvals');
  await expect(page.getByRole('heading', { name: 'Branch approval queue' })).toBeVisible();
  const approvalRequest = page.locator('.purchase-request-item').filter({ hasText: note });
  await expect(approvalRequest).toBeVisible();
  await approvalRequest.getByRole('button', { name: 'Approve' }).click();
  await expect(page.getByRole('status')).toContainText('Purchase request approved.');
  await expect(approvalRequest).toHaveCount(0);
  await signOut(page);

  await signIn(page, request, procurementEmail);
  await page.goto('/purchase-requests/fulfillment');
  await expect(page.getByRole('heading', { name: 'Purchase request fulfillment' })).toBeVisible();
  const fulfillmentRequest = page.locator('.purchase-request-item').filter({ hasText: note });
  await expect(fulfillmentRequest).toBeVisible();
  await fulfillmentRequest.getByLabel('Order number').fill(`PO-${suffix}`);
  await fulfillmentRequest.getByLabel('Fulfillment note').fill('Order confirmed by E2E.');
  await fulfillmentRequest.getByRole('button', { name: 'Mark as ordered' }).click();
  await expect(page.getByRole('status')).toContainText('Purchase request marked as ordered.');
  await fulfillmentRequest.getByRole('button', { name: 'Mark as delivered' }).click();
  await expect(page.getByRole('status')).toContainText('Purchase request marked as delivered.');
  await expect(fulfillmentRequest).toHaveCount(0);
  await signOut(page);

  await signIn(page, request, employeeEmail);
  await page.goto('/purchase-requests');
  await expect(page.getByRole('button', { name: new RegExp(note) })).toBeVisible();
  await page.getByRole('button', { name: new RegExp(note) }).click();
  await expect(page.getByRole('heading', { name: note })).toBeVisible();
  await expect(page.locator('.role-badge').filter({ hasText: 'Delivered' })).toBeVisible();

  const history = page.locator('[aria-label="Purchase request history"]');
  await expect(history).toBeVisible();
  await expect(history.locator('li')).toHaveCount(4);
  await expect(history).toContainText('Submitted');
  await expect(history).toContainText('Approved');
  await expect(history).toContainText('Ordered');
  await expect(history).toContainText('Delivered');
});