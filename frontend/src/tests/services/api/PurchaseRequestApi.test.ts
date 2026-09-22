import { describe, expect, it, vi } from 'vitest';
import { CatalogApi } from '../../../services/api/CatalogApi';
import { PurchaseRequestApi } from '../../../services/api/PurchaseRequestApi';
import type { HttpClient } from '../../../services/api/HttpClient';

function createClientMock() {
  return {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  } as unknown as jest.Mocked<HttpClient>;
}

describe('PurchaseRequestApi', () => {
  it('builds list and details requests in the organization scope', async () => {
    const client = createClientMock();
    const api = new PurchaseRequestApi(client);

    await api.listDrafts('organization-1', 2, 10);
    await api.getDetails('organization-1', 'draft-1');

    expect(client.get).toHaveBeenNthCalledWith(
      1,
      '/organizations/organization-1/purchase-requests?page=2&pageSize=10',
    );
    expect(client.get).toHaveBeenNthCalledWith(
      2,
      '/organizations/organization-1/purchase-requests/draft-1',
    );
  });

  it('sends client-controlled draft fields and the expected concurrency stamp', async () => {
    const client = createClientMock();
    const api = new PurchaseRequestApi(client);

    await api.createDraft('organization-1', { note: 'Office supplies' });
    await api.addItem('organization-1', 'draft-1', {
      productId: 'product-1',
      quantity: 2,
      comment: 'Finance team',
      concurrencyStamp: 'stamp-1',
    });
    await api.updateItemQuantity('organization-1', 'draft-1', 'item-1', {
      quantity: 3,
      concurrencyStamp: 'stamp-2',
    });
    await api.removeItem('organization-1', 'draft-1', 'item-1', {
      concurrencyStamp: 'stamp-3',
    });

    expect(client.post).toHaveBeenNthCalledWith(
      1,
      '/organizations/organization-1/purchase-requests',
      { note: 'Office supplies' },
    );
    expect(client.post).toHaveBeenNthCalledWith(
      2,
      '/organizations/organization-1/purchase-requests/draft-1/items',
      {
        productId: 'product-1',
        quantity: 2,
        comment: 'Finance team',
        concurrencyStamp: 'stamp-1',
      },
    );
    expect(client.patch).toHaveBeenCalledWith(
      '/organizations/organization-1/purchase-requests/draft-1/items/item-1',
      { quantity: 3, concurrencyStamp: 'stamp-2' },
    );
    expect(client.delete).toHaveBeenCalledWith(
      '/organizations/organization-1/purchase-requests/draft-1/items/item-1',
      { concurrencyStamp: 'stamp-3' },
    );
  });

  it('builds approval, budget, decision, and fulfillment requests', async () => {
    const client = createClientMock();
    const api = new PurchaseRequestApi(client);

    await api.listApprovalQueue('organization-1');
    await api.listFulfillmentQueue('organization-1');
    await api.getBudget('organization-1', 'branch-1', 2026, 9);
    await api.upsertBudget('organization-1', 'branch-1', 2026, 9, {
      limitAmount: 5000,
      expectedConcurrencyStamp: null,
    });
    await api.decide('organization-1', 'request-1', {
      concurrencyStamp: 'stamp-1',
      approve: false,
      rejectionReason: 'Not required',
    });
    await api.markOrdered('organization-1', 'request-1', {
      concurrencyStamp: 'stamp-2',
      orderNumber: 'PO-2026-001',
      fulfillmentNote: 'Supplier confirmed',
    });
    await api.markDelivered('organization-1', 'request-1', {
      concurrencyStamp: 'stamp-3',
      fulfillmentNote: 'Received',
    });

    expect(client.get).toHaveBeenNthCalledWith(
      1,
      '/organizations/organization-1/purchase-requests/approval-queue',
    );
    expect(client.get).toHaveBeenNthCalledWith(
      2,
      '/organizations/organization-1/purchase-requests/fulfillment-queue',
    );
    expect(client.get).toHaveBeenNthCalledWith(
      3,
      '/organizations/organization-1/purchase-requests/budgets/branch-1/2026/9',
    );
    expect(client.put).toHaveBeenCalledWith(
      '/organizations/organization-1/purchase-requests/budgets/branch-1/2026/9',
      { limitAmount: 5000, expectedConcurrencyStamp: null },
    );
    expect(client.post).toHaveBeenNthCalledWith(
      1,
      '/organizations/organization-1/purchase-requests/request-1/decision',
      {
        concurrencyStamp: 'stamp-1',
        approve: false,
        rejectionReason: 'Not required',
      },
    );
    expect(client.post).toHaveBeenNthCalledWith(
      2,
      '/organizations/organization-1/purchase-requests/request-1/fulfillment/order',
      {
        concurrencyStamp: 'stamp-2',
        orderNumber: 'PO-2026-001',
        fulfillmentNote: 'Supplier confirmed',
      },
    );
    expect(client.post).toHaveBeenNthCalledWith(
      3,
      '/organizations/organization-1/purchase-requests/request-1/fulfillment/deliver',
      {
        concurrencyStamp: 'stamp-3',
        fulfillmentNote: 'Received',
      },
    );
  });
});

describe('CatalogApi', () => {
  it('loads selectable products for the organization', async () => {
    const client = createClientMock();
    const api = new CatalogApi(client);

    await api.getSelectableProducts('organization-1');

    expect(client.get).toHaveBeenCalledWith('/organizations/organization-1/catalog/products');
  });
});
