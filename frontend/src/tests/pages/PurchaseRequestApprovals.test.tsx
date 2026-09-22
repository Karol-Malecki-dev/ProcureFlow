import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import PurchaseRequestApprovals from '../../pages/PurchaseRequestApprovals';
import { HttpError, organizationApi, purchaseRequestApi } from '../../services/api';
import { BusinessRole, PurchaseRequestStatus } from '../../types';
import type {
  BranchMonthlyBudgetDto,
  PurchaseRequestApprovalQueueDto,
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
      listApprovalQueue: vi.fn(),
      getBudget: vi.fn(),
      decide: vi.fn(),
    },
  };
});

const mockedOrganizationApi = organizationApi as jest.Mocked<typeof organizationApi>;
const mockedPurchaseRequestApi = purchaseRequestApi as jest.Mocked<typeof purchaseRequestApi>;

const membership = {
  id: 'membership-1',
  organizationId: 'organization-1',
  userId: 'manager-1',
  branchId: 'branch-1',
  role: BusinessRole.Manager,
  isActive: true,
};

const queue: PurchaseRequestApprovalQueueDto = {
  queueRole: BusinessRole.Manager,
  items: [
    {
      id: 'request-1',
      authorUserId: 'employee-1',
      organizationId: membership.organizationId,
      branchId: membership.branchId,
      status: PurchaseRequestStatus.Submitted,
      note: 'Office equipment',
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
      canDecide: true,
      queueRole: BusinessRole.Manager,
    },
  ],
};

const budget: BranchMonthlyBudgetDto = {
  id: 'budget-1',
  organizationId: membership.organizationId,
  branchId: membership.branchId,
  year: 2026,
  month: 9,
  limitAmount: 1000,
  usedAmount: 200,
  availableAmount: 800,
  concurrencyStamp: 'budget-stamp-1',
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
    <MemoryRouter initialEntries={['/purchase-requests/approvals']}>
      <Routes>
        <Route path="/purchase-requests/approvals" element={<PurchaseRequestApprovals />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('PurchaseRequestApprovals page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse(membership));
    mockedPurchaseRequestApi.listApprovalQueue.mockResolvedValue(apiResponse(queue));
    mockedPurchaseRequestApi.getBudget.mockResolvedValue(apiResponse(budget));
  });

  it('loads the manager queue and current branch budget', async () => {
    renderPage();

    expect(await screen.findByRole('heading', { name: 'Branch approval queue' })).toBeInTheDocument();
    expect(await screen.findByText('Office equipment')).toBeInTheDocument();
    expect(await screen.findByText('800.00 PLN')).toBeInTheDocument();
    expect(screen.getAllByText('200.00 PLN')).toHaveLength(2);
    expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /reject/i })).toBeInTheDocument();
  });

  it('requires a reason before sending a rejection and refreshes after the decision', async () => {
    const user = userEvent;
    const decisionResponse = apiResponse({
      id: 'request-1',
      authorUserId: 'employee-1',
      organizationId: membership.organizationId,
      branchId: membership.branchId,
      status: PurchaseRequestStatus.Rejected,
      note: queue.items[0].note,
      items: queue.items[0].items,
      totalValue: queue.items[0].totalValue,
      createdAt: queue.items[0].createdAt,
      updatedAt: queue.items[0].updatedAt,
      concurrencyStamp: 'stamp-2',
    }) as PurchaseRequestDetailsResponse;
    mockedPurchaseRequestApi.decide.mockResolvedValue(decisionResponse);

    renderPage();
    await screen.findByRole('heading', { name: 'Branch approval queue' });
    await screen.findByRole('button', { name: /^reject$/i });

    await act(async () => {
      await user.click(screen.getByRole('button', { name: /^reject$/i }));
    });
    expect(screen.getByRole('alert')).toHaveTextContent('A rejection reason is required.');
    expect(mockedPurchaseRequestApi.decide).not.toHaveBeenCalled();

    await user.type(screen.getByLabelText('Rejection reason'), 'The request is not required.');
    await act(async () => {
      await user.click(screen.getByRole('button', { name: /^reject$/i }));
      await Promise.resolve();
    });

    await waitFor(() => {
      expect(mockedPurchaseRequestApi.decide).toHaveBeenCalledWith(
        membership.organizationId,
        'request-1',
        {
          concurrencyStamp: 'stamp-1',
          approve: false,
          rejectionReason: 'The request is not required.',
        },
      );
      expect(screen.getByRole('button', { name: /refresh/i })).not.toBeDisabled();
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Purchase request rejected.');
  });

  it('renders the Procurement queue without loading a branch budget', async () => {
    const procurementMembership = {
      ...membership,
      branchId: null,
      role: BusinessRole.Procurement,
    };
    const procurementQueue = {
      queueRole: BusinessRole.Procurement,
      items: [{
        ...queue.items[0],
        status: PurchaseRequestStatus.AwaitingProcurementApproval,
        queueRole: BusinessRole.Procurement,
      }],
    };
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse(procurementMembership));
    mockedPurchaseRequestApi.listApprovalQueue.mockResolvedValue(apiResponse(procurementQueue));

    renderPage();

    expect(await screen.findByRole('heading', { name: 'Procurement approval queue' })).toBeInTheDocument();
    expect(await screen.findByText('AwaitingProcurementApproval')).toBeInTheDocument();
    expect(screen.queryByText('Branch budget')).not.toBeInTheDocument();
    expect(mockedPurchaseRequestApi.getBudget).not.toHaveBeenCalled();
  });

  it('refreshes the queue after an optimistic-concurrency conflict', async () => {
    const user = userEvent;
    mockedPurchaseRequestApi.listApprovalQueue
      .mockResolvedValueOnce(apiResponse(queue))
      .mockResolvedValueOnce(apiResponse({ ...queue, items: [] }));
    mockedPurchaseRequestApi.decide.mockRejectedValue(new HttpError(409, 'Conflict', null));

    renderPage();
    await screen.findByRole('button', { name: /^approve$/i });

    await act(async () => {
      await user.click(screen.getByRole('button', { name: /^approve$/i }));
      await Promise.resolve();
    });

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'This request changed in another session. The latest queue has been loaded.',
    );
    await waitFor(() => {
      expect(screen.queryByText('Office equipment')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: /refresh/i })).not.toBeDisabled();
    });
  });
});
