import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import PurchaseRequestFulfillment from '../../pages/PurchaseRequestFulfillment';
import { organizationApi, purchaseRequestApi } from '../../services/api';
import { useAuth } from '../../hooks/useAuth';
import { BusinessRole, PurchaseRequestStatus } from '../../types';
import type {
  CurrentMembershipDto,
  PurchaseRequestFulfillmentQueueDto,
  PurchaseRequestDetailsResponse,
} from '../../types';

vi.mock('../../services/api', async () => {
  const actual = await vi.importActual<typeof import('../../services/api')>('../../services/api');

  return {
    ...actual,
    organizationApi: {
      getCurrentMembership: vi.fn(),
    },
    purchaseRequestApi: {
      listFulfillmentQueue: vi.fn(),
      markOrdered: vi.fn(),
      markDelivered: vi.fn(),
    },
  };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn(),
}));

const mockedOrganizationApi = organizationApi as jest.Mocked<typeof organizationApi>;
const mockedPurchaseRequestApi = purchaseRequestApi as jest.Mocked<typeof purchaseRequestApi>;
const mockedUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;

const membership: CurrentMembershipDto = {
  id: 'membership-1',
  organizationId: 'organization-1',
  userId: 'procurement-1',
  branchId: null,
  role: BusinessRole.Procurement,
  isActive: true,
};

const approvedItem = {
  id: 'request-1',
  authorUserId: 'employee-1',
  organizationId: membership.organizationId,
  branchId: 'branch-1',
  status: PurchaseRequestStatus.Approved,
  note: 'Office equipment',
  fulfillmentOrderNumber: null,
  fulfillmentNote: null,
  items: [
    {
      id: 'item-1',
      productId: 'product-1',
      productName: 'Monitor',
      productCode: 'MON-1',
      unitName: 'Piece',
      unitSymbol: 'pc',
      unitPrice: 100,
      quantity: 2,
      comment: null,
      lineTotal: 200,
    },
  ],
  totalValue: 200,
  createdAt: '2026-09-21T09:00:00Z',
  updatedAt: '2026-09-21T09:00:00Z',
  concurrencyStamp: 'stamp-1',
};

const queue: PurchaseRequestFulfillmentQueueDto = {
  items: [approvedItem],
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

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/purchase-requests/fulfillment']}>
      <Routes>
        <Route path="/purchase-requests/fulfillment" element={<PurchaseRequestFulfillment />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('PurchaseRequestFulfillment page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedUseAuth.mockReturnValue({
      user: {
        id: 'procurement-1',
        email: 'procurement@example.com',
        displayName: 'Procurement User',
        firstName: 'Procurement',
        lastName: 'User',
        avatarUrl: null,
        role: 'User',
      },
    } as ReturnType<typeof useAuth>);
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse(membership));
    mockedPurchaseRequestApi.listFulfillmentQueue.mockResolvedValue(apiResponse(queue));
  });

  it('loads the queue and moves an approved request through ordering and delivery', async () => {
    const user = userEvent;
    const orderedItem = {
      ...approvedItem,
      status: PurchaseRequestStatus.Ordered,
      fulfillmentOrderNumber: 'PO-2026-001',
      fulfillmentNote: 'Supplier confirmed',
      concurrencyStamp: 'stamp-2',
    };
    const orderedQueue: PurchaseRequestFulfillmentQueueDto = { items: [orderedItem] };
    const orderedResponse = apiResponse({
      ...approvedItem,
      status: PurchaseRequestStatus.Ordered,
      fulfillmentOrderNumber: 'PO-2026-001',
      fulfillmentNote: 'Supplier confirmed',
      concurrencyStamp: 'stamp-2',
    }) as PurchaseRequestDetailsResponse;
    const deliveredResponse = apiResponse({
      ...orderedItem,
      status: PurchaseRequestStatus.Delivered,
      concurrencyStamp: 'stamp-3',
    }) as PurchaseRequestDetailsResponse;

    mockedPurchaseRequestApi.listFulfillmentQueue
      .mockResolvedValueOnce(apiResponse(queue))
      .mockResolvedValueOnce(apiResponse(orderedQueue))
      .mockResolvedValueOnce(apiResponse({ items: [] }));
    mockedPurchaseRequestApi.markOrdered.mockResolvedValue(orderedResponse);
    mockedPurchaseRequestApi.markDelivered.mockResolvedValue(deliveredResponse);

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Fulfillment queue' })).toBeInTheDocument();
    expect(await screen.findByText('Office equipment')).toBeInTheDocument();

    await act(async () => {
      await user.type(screen.getByLabelText('Order number'), 'PO-2026-001');
      await user.type(screen.getByLabelText('Fulfillment note'), 'Supplier confirmed');
      await user.click(screen.getByRole('button', { name: /mark as ordered/i }));
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(mockedPurchaseRequestApi.markOrdered).toHaveBeenCalledWith(
        membership.organizationId,
        'request-1',
        {
          concurrencyStamp: 'stamp-1',
          orderNumber: 'PO-2026-001',
          fulfillmentNote: 'Supplier confirmed',
        },
      );
    });
    expect(await screen.findByRole('button', { name: /mark as delivered/i })).toBeInTheDocument();

    await act(async () => {
      await user.type(screen.getByLabelText('Delivery note'), 'Received by warehouse');
      await user.click(screen.getByRole('button', { name: /mark as delivered/i }));
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(mockedPurchaseRequestApi.markDelivered).toHaveBeenCalledWith(
        membership.organizationId,
        'request-1',
        {
          concurrencyStamp: 'stamp-2',
          fulfillmentNote: 'Received by warehouse',
        },
      );
    });
    expect(await screen.findByText('No accepted purchase requests are waiting for fulfillment.')).toBeInTheDocument();
  });

  it('does not expose the queue to users without a Procurement scope', async () => {
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse({
      ...membership,
      role: BusinessRole.Manager,
      branchId: 'branch-1',
    }));

    renderPage();

    expect(await screen.findByText('An active Procurement membership or platform administrator account is required.')).toBeInTheDocument();
    expect(mockedPurchaseRequestApi.listFulfillmentQueue).not.toHaveBeenCalled();
  });
});
