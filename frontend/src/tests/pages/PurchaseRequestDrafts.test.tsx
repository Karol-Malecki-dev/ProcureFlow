import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import PurchaseRequestDrafts from '../../pages/PurchaseRequestDrafts';
import { catalogApi, HttpError, organizationApi, purchaseRequestApi } from '../../services/api';
import { BusinessRole, PurchaseRequestStatus } from '../../types';
import type { PurchaseRequestDto, PurchaseRequestListItemDto } from '../../types';

vi.mock('../../services/api', async () => {
  const actual = await vi.importActual<typeof import('../../services/api')>('../../services/api');

  return {
    ...actual,
    catalogApi: {
      getSelectableProducts: vi.fn(),
    },
    organizationApi: {
      getCurrentMembership: vi.fn(),
    },
    purchaseRequestApi: {
      listDrafts: vi.fn(),
      getDetails: vi.fn(),
      createDraft: vi.fn(),
      addItem: vi.fn(),
      updateItemQuantity: vi.fn(),
      removeItem: vi.fn(),
    },
  };
});

const mockedCatalogApi = catalogApi as jest.Mocked<typeof catalogApi>;
const mockedOrganizationApi = organizationApi as jest.Mocked<typeof organizationApi>;
const mockedPurchaseRequestApi = purchaseRequestApi as jest.Mocked<typeof purchaseRequestApi>;

const membership = {
  id: 'membership-1',
  organizationId: 'organization-1',
  userId: 'user-1',
  branchId: 'branch-1',
  role: BusinessRole.Employee,
  isActive: true,
};

const product = {
  id: 'product-1',
  name: 'A4 paper',
  code: 'PAPER-A4',
  unitName: 'Pack',
  unitSymbol: 'pkg',
  unitPrice: 12.5,
};

const item = {
  id: 'item-1',
  productId: product.id,
  productName: product.name,
  productCode: product.code,
  unitName: product.unitName,
  unitSymbol: product.unitSymbol,
  unitPrice: product.unitPrice,
  quantity: 2,
  comment: 'For the finance team',
  lineTotal: 25,
};

const emptyDraft: PurchaseRequestDto = {
  id: 'draft-1',
  authorUserId: membership.userId,
  organizationId: membership.organizationId,
  branchId: membership.branchId,
  status: PurchaseRequestStatus.Draft,
  note: 'Office supplies',
  items: [],
  totalValue: 0,
  createdAt: '2026-09-21T09:00:00Z',
  updatedAt: '2026-09-21T09:00:00Z',
  concurrencyStamp: 'stamp-1',
};

const populatedDraft: PurchaseRequestDto = {
  ...emptyDraft,
  items: [item],
  totalValue: 25,
};

function apiResponse<T>(data: T) {
  return {
    statusCode: 200,
    message: 'OK',
    data,
    errors: null,
    timestamp: '2026-09-21T09:00:00Z',
  };
}

function listItem(draft: PurchaseRequestDto): PurchaseRequestListItemDto {
  return {
    id: draft.id,
    status: draft.status,
    note: draft.note,
    itemCount: draft.items.length,
    totalValue: draft.totalValue,
    createdAt: draft.createdAt,
    updatedAt: draft.updatedAt,
    concurrencyStamp: draft.concurrencyStamp,
  };
}

function renderPage(path = '/purchase-requests') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/purchase-requests" element={<PurchaseRequestDrafts />} />
        <Route path="/purchase-requests/:purchaseRequestId" element={<PurchaseRequestDrafts />} />
      </Routes>
    </MemoryRouter>,
  );
}

function setupBaseApi(draftList: PurchaseRequestListItemDto[] = [], details: PurchaseRequestDto | null = null) {
  mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse(membership));
  mockedCatalogApi.getSelectableProducts.mockResolvedValue(apiResponse([product]));
  mockedPurchaseRequestApi.listDrafts.mockResolvedValue(apiResponse({
    items: draftList,
    page: 1,
    pageSize: 20,
    totalCount: draftList.length,
  }));
  mockedPurchaseRequestApi.getDetails.mockResolvedValue(apiResponse(details ?? emptyDraft));
}

describe('PurchaseRequestDrafts page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it('loads the current Employee scope and renders an empty draft state', async () => {
    setupBaseApi();

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Purchase request drafts' })).toBeInTheDocument();
    await waitFor(() => expect(mockedPurchaseRequestApi.listDrafts).toHaveBeenCalledWith(membership.organizationId, 1));
    expect(screen.getByText('No drafts found.')).toBeInTheDocument();
    expect(mockedCatalogApi.getSelectableProducts).toHaveBeenCalledWith(membership.organizationId);
  });

  it('creates a draft, adds an item, updates its quantity, and removes it', async () => {
    const user = userEvent;
    setupBaseApi([listItem(emptyDraft)], emptyDraft);
    const addedDraft = { ...populatedDraft, concurrencyStamp: 'stamp-2' };
    const updatedDraft = {
      ...addedDraft,
      items: [{ ...item, quantity: 3, lineTotal: 37.5 }],
      totalValue: 37.5,
      concurrencyStamp: 'stamp-3',
    };
    const removedDraft = {
      ...updatedDraft,
      items: [],
      totalValue: 0,
      concurrencyStamp: 'stamp-4',
    };

    mockedPurchaseRequestApi.createDraft.mockResolvedValue(apiResponse(emptyDraft));
    mockedPurchaseRequestApi.addItem.mockResolvedValue(apiResponse(addedDraft));
    mockedPurchaseRequestApi.updateItemQuantity.mockResolvedValue(apiResponse(updatedDraft));
    mockedPurchaseRequestApi.removeItem.mockResolvedValue(apiResponse(removedDraft));
    mockedPurchaseRequestApi.getDetails.mockImplementation(async () => apiResponse(emptyDraft));

    renderPage();
    await screen.findByRole('heading', { name: 'Purchase request drafts' });

    await user.type(screen.getByLabelText('Note'), 'Office supplies');
    await user.click(screen.getByRole('button', { name: /create draft/i }));

    await waitFor(() => expect(mockedPurchaseRequestApi.createDraft).toHaveBeenCalledWith(
      membership.organizationId,
      { note: 'Office supplies' },
    ));
    expect(await screen.findByRole('heading', { name: 'Office supplies' })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('button', { name: /create draft/i })).not.toBeDisabled());

    await user.selectOptions(screen.getByLabelText('Product'), product.id);
    await user.clear(screen.getByLabelText('Quantity'));
    await user.type(screen.getByLabelText('Quantity'), '2');
    await user.click(screen.getByRole('button', { name: /add item/i }));

    await waitFor(() => expect(mockedPurchaseRequestApi.addItem).toHaveBeenCalledWith(
      membership.organizationId,
      emptyDraft.id,
      {
        productId: product.id,
        quantity: 2,
        comment: null,
        concurrencyStamp: emptyDraft.concurrencyStamp,
      },
    ));

    const itemArticle = screen.getByRole('article');
    await waitFor(() => {
      expect(screen.getByRole('status')).toHaveTextContent('Item added.');
      expect(within(itemArticle).getByText('25.00 PLN', { selector: 'dd' })).toBeInTheDocument();
    });
    await waitFor(() => expect(screen.getByRole('button', { name: /^add item$/i })).not.toBeDisabled());
    expect(screen.getByText('25.00 PLN', { selector: 'strong' })).toBeInTheDocument();

    const itemQuantity = within(itemArticle).getByLabelText('Quantity');
    await user.clear(itemQuantity);
    await user.type(itemQuantity, '3');
    await user.click(screen.getByRole('button', { name: /save quantity/i }));

    await waitFor(() => expect(mockedPurchaseRequestApi.updateItemQuantity).toHaveBeenCalledWith(
      membership.organizationId,
      emptyDraft.id,
      item.id,
      { quantity: 3, concurrencyStamp: addedDraft.concurrencyStamp },
    ));
    await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent('Quantity updated.'));
    await waitFor(() => expect(screen.getByRole('button', { name: /save quantity/i })).not.toBeDisabled());

    await user.click(screen.getByRole('button', { name: /remove/i }));

    await waitFor(() => expect(mockedPurchaseRequestApi.removeItem).toHaveBeenCalledWith(
      membership.organizationId,
      emptyDraft.id,
      item.id,
      { concurrencyStamp: updatedDraft.concurrencyStamp },
    ));
    await waitFor(() => expect(screen.getByRole('status')).toHaveTextContent('Item removed.'));
    expect(await screen.findByText('This draft has no items yet.')).toBeInTheDocument();
  });

  it('reloads the server version after an optimistic-concurrency conflict', async () => {
    const user = userEvent;
    const refreshedDraft = {
      ...populatedDraft,
      items: [{ ...item, quantity: 4, lineTotal: 50 }],
      totalValue: 50,
      concurrencyStamp: 'stamp-server',
    };
    setupBaseApi([listItem(populatedDraft)], populatedDraft);
    mockedPurchaseRequestApi.updateItemQuantity.mockRejectedValue(new HttpError(409, 'Conflict', null));
    mockedPurchaseRequestApi.getDetails
      .mockResolvedValueOnce(apiResponse(populatedDraft))
      .mockResolvedValueOnce(apiResponse(refreshedDraft));

    renderPage('/purchase-requests/draft-1');
  const itemArticle = await screen.findByRole('article');
  expect(within(itemArticle).getByText('25.00 PLN', { selector: 'dd' })).toBeInTheDocument();

  const itemQuantity = within(itemArticle).getByLabelText('Quantity');
  await user.clear(itemQuantity);
  await user.type(itemQuantity, '3');
    await user.click(screen.getByRole('button', { name: /save quantity/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This draft changed in another session. The latest version has been loaded.');
    await waitFor(() => expect(within(screen.getByRole('article')).getByLabelText('Quantity')).toHaveValue(4));
    await waitFor(() => expect(screen.getByRole('button', { name: /save quantity/i })).not.toBeDisabled());
    expect(mockedPurchaseRequestApi.getDetails).toHaveBeenCalledWith(
      membership.organizationId,
      populatedDraft.id,
    );
  });

  it('requires an Employee branch membership before showing the editor', async () => {
    setupBaseApi();
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse({
      ...membership,
      role: BusinessRole.Manager,
    }));

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Purchase requests' })).toBeInTheDocument();
    expect(screen.getByText('An active Employee branch membership is required.')).toBeInTheDocument();
    expect(mockedPurchaseRequestApi.listDrafts).not.toHaveBeenCalled();
  });
});
