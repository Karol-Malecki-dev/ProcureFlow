import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi } from 'vitest';
import Dashboard from '../../pages/Dashboard';
import { useAuth } from '../../hooks/useAuth';
import { useFeatureAvailability } from '../../hooks/useFeatureAvailability';
import { organizationApi, purchaseRequestApi } from '../../services/api';
import { BusinessRole } from '../../types';

vi.mock('../../hooks/useAuth');
vi.mock('../../hooks/useFeatureAvailability');
vi.mock('../../services/api', async () => {
  const actual = await vi.importActual<typeof import('../../services/api')>('../../services/api');

  return {
    ...actual,
    organizationApi: {
      getCurrentMembership: vi.fn(),
    },
    purchaseRequestApi: {
      getDashboard: vi.fn(),
    },
  };
});

const mockedUseAuth = useAuth as jest.MockedFunction<typeof useAuth>;
const mockedUseFeatureAvailability = useFeatureAvailability as jest.MockedFunction<typeof useFeatureAvailability>;
const mockedOrganizationApi = organizationApi as jest.Mocked<typeof organizationApi>;
const mockedPurchaseRequestApi = purchaseRequestApi as jest.Mocked<typeof purchaseRequestApi>;

function apiResponse<T>(data: T) {
  return {
    statusCode: 200,
    message: 'OK',
    data,
    errors: null,
    timestamp: '2026-09-29T00:00:00Z',
  };
}

describe('Dashboard page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      user: {
        id: 'user-1',
        displayName: 'Alicja Employee',
        email: 'alicia@example.com',
        role: 'User',
      },
      tokens: { accessToken: 'token', expiresIn: 900 },
    } as ReturnType<typeof useAuth>);
    mockedUseFeatureAvailability.mockReturnValue({
      dashboardOverviewEnabled: true,
      globalSearchEnabled: false,
      emailFeatureSectionsEnabled: false,
      emailDeliveryEnabled: false,
      emailTwoFactorEnabled: false,
    } as ReturnType<typeof useFeatureAvailability>);
    mockedOrganizationApi.getCurrentMembership.mockResolvedValue(apiResponse({
      id: 'membership-1',
      organizationId: 'organization-1',
      userId: 'user-1',
      branchId: 'branch-1',
      role: BusinessRole.Manager,
      isActive: true,
    }));
    mockedPurchaseRequestApi.getDashboard.mockResolvedValue(apiResponse({
      scopeRole: BusinessRole.Manager,
      pendingRequestsCount: 3,
      currentMonthOrderValue: 1250.5,
      mostFrequentlyOrderedProducts: [{
        productId: 'product-1',
        productName: 'Monitor',
        productCode: 'MON-1',
        totalQuantity: 4,
        requestCount: 2,
      }],
      spendingByBranch: [{
        branchId: 'branch-1',
        branchName: 'Warsaw',
        totalValue: 1250.5,
      }],
    }));
  });

  it('loads and renders ProcureFlow dashboard metrics', async () => {
    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('heading', { name: 'Purchase request overview' })).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getAllByText(/1250,50/)).toHaveLength(2);
    expect(screen.getByText('Monitor')).toBeInTheDocument();
    expect(screen.getByText('Warsaw')).toBeInTheDocument();
    expect(mockedPurchaseRequestApi.getDashboard).toHaveBeenCalledWith('organization-1');
  });

  it('shows the backend error when the dashboard cannot be loaded', async () => {
    mockedPurchaseRequestApi.getDashboard.mockRejectedValue(new Error('Dashboard unavailable'));

    render(
      <MemoryRouter>
        <Dashboard />
      </MemoryRouter>,
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('Dashboard unavailable');
  });
});