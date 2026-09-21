import { describe, expect, it, vi } from 'vitest';
import { CatalogApi } from '../../../services/api/CatalogApi';
import { PurchaseRequestApi } from '../../../services/api/PurchaseRequestApi';
import type { HttpClient } from '../../../services/api/HttpClient';

function createClientMock() {
  return {
    get: vi.fn(),
    post: vi.fn(),
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
});

describe('CatalogApi', () => {
  it('loads selectable products for the organization', async () => {
    const client = createClientMock();
    const api = new CatalogApi(client);

    await api.getSelectableProducts('organization-1');

    expect(client.get).toHaveBeenCalledWith('/organizations/organization-1/catalog/products');
  });
});
