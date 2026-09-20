import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import OrganizationMemberships from '../../pages/OrganizationMemberships';
import { adminApi, organizationApi } from '../../services/api';
import { BusinessRole } from '../../types/organization';
import type { AdminUserListItemDto } from '../../types/admin';
import type { BranchDto, MembershipListItemDto } from '../../types/organization';

vi.mock('../../services/api', async () => {
  const actual = await vi.importActual<typeof import('../../services/api')>('../../services/api');

  return {
    ...actual,
    adminApi: {
      getUsers: vi.fn(),
    },
    organizationApi: {
      getActiveOrganization: vi.fn(),
      getBranches: vi.fn(),
      getMemberships: vi.fn(),
      createMembership: vi.fn(),
      updateMembership: vi.fn(),
      archiveMembership: vi.fn(),
    },
  };
});

const mockedAdminApi = adminApi as jest.Mocked<typeof adminApi>;
const mockedOrganizationApi = organizationApi as jest.Mocked<typeof organizationApi>;

const activeBranch: BranchDto = {
  id: 'branch-1',
  name: 'Warsaw branch',
  code: 'WAW',
  isArchived: false,
  address: {
    street: 'Main Street',
    buildingNumber: '1',
    apartmentNumber: null,
    city: 'Warsaw',
    postalCode: '00-001',
    country: 'Poland',
  },
};

const activeUser: AdminUserListItemDto = {
  id: 'user-1',
  email: 'employee@example.com',
  displayName: 'Alex Employee',
  role: 'User',
  isActive: true,
  isEmailConfirmed: true,
  createdAt: '2026-09-20T00:00:00Z',
};

const activeMembership: MembershipListItemDto = {
  id: 'membership-1',
  userId: activeUser.id,
  userDisplayName: activeUser.displayName,
  userEmail: activeUser.email,
  branchId: activeBranch.id,
  branchName: activeBranch.name,
  role: BusinessRole.Manager,
  isActive: true,
};

function apiResponse<T>(data: T) {
  return {
    statusCode: 200,
    message: 'OK',
    data,
    errors: null,
    timestamp: '2026-09-20T00:00:00Z',
  };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/admin/organizations/org-1/memberships']}>
      <Routes>
        <Route path="/admin/organizations/:organizationId/memberships" element={<OrganizationMemberships />} />
      </Routes>
    </MemoryRouter>,
  );
}

function setupApi(membershipItems: MembershipListItemDto[] = []) {
  mockedOrganizationApi.getBranches.mockResolvedValue(apiResponse([activeBranch]));
  mockedAdminApi.getUsers.mockResolvedValue(apiResponse([activeUser]));
  mockedOrganizationApi.getMemberships.mockResolvedValue(apiResponse(membershipItems));
}

describe('OrganizationMemberships page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it('renders the empty state after loading references and memberships', async () => {
    setupApi();

    renderPage();

    expect(await screen.findByRole('heading', { name: 'No memberships found' })).toBeInTheDocument();
    expect(screen.getByText('Organization org-1')).toBeInTheDocument();
  });

  it('assigns a Manager to a selected branch', async () => {
    const user = userEvent;
    setupApi();
    mockedOrganizationApi.createMembership.mockResolvedValue(apiResponse({
      id: 'membership-1',
      organizationId: 'org-1',
      userId: activeUser.id,
      branchId: activeBranch.id,
      role: BusinessRole.Manager,
      isActive: true,
    }));

    renderPage();
    await screen.findByRole('heading', { name: 'No memberships found' });

    await user.selectOptions(screen.getByLabelText('User'), activeUser.id);
    await user.selectOptions(screen.getByLabelText('Business role'), String(BusinessRole.Manager));
    await user.selectOptions(screen.getByRole('combobox', { name: /^Branch$/ }), activeBranch.id);
    await user.click(screen.getByRole('button', { name: /assign membership/i }));

    await waitFor(() => expect(mockedOrganizationApi.createMembership).toHaveBeenCalledWith(
      'org-1',
      {
        userId: activeUser.id,
        branchId: activeBranch.id,
        role: BusinessRole.Manager,
      },
    ));
    expect(await screen.findByText('Membership assigned.')).toBeInTheDocument();
  });

  it('clears the branch requirement for Procurement', async () => {
    const user = userEvent;
    setupApi();
    mockedOrganizationApi.createMembership.mockResolvedValue(apiResponse({
      id: 'membership-1',
      organizationId: 'org-1',
      userId: activeUser.id,
      branchId: null,
      role: BusinessRole.Procurement,
      isActive: true,
    }));

    renderPage();
    await screen.findByRole('heading', { name: 'No memberships found' });

    await user.selectOptions(screen.getByLabelText('User'), activeUser.id);
    await user.selectOptions(screen.getByLabelText('Business role'), String(BusinessRole.Procurement));

    expect(screen.queryByRole('combobox', { name: /^Branch$/ })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /assign membership/i }));

    await waitFor(() => expect(mockedOrganizationApi.createMembership).toHaveBeenCalledWith(
      'org-1',
      {
        userId: activeUser.id,
        branchId: null,
        role: BusinessRole.Procurement,
      },
    ));
  });

  it('updates and deactivates an active membership', async () => {
    const user = userEvent;
    setupApi([activeMembership]);
    mockedOrganizationApi.updateMembership.mockResolvedValue(apiResponse({
      id: activeMembership.id,
      organizationId: 'org-1',
      userId: activeUser.id,
      branchId: null,
      role: BusinessRole.Procurement,
      isActive: true,
    }));
    mockedOrganizationApi.archiveMembership.mockResolvedValue(apiResponse({ isArchived: true }));

    renderPage();
    expect(await screen.findByRole('heading', { name: activeUser.displayName })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /^edit$/i }));
    await user.selectOptions(screen.getByLabelText('Business role'), String(BusinessRole.Procurement));
    await user.click(screen.getByRole('button', { name: /save assignment/i }));

    await waitFor(() => expect(mockedOrganizationApi.updateMembership).toHaveBeenCalledWith(
      'org-1',
      activeMembership.id,
      { role: BusinessRole.Procurement, branchId: null },
    ));

    await user.click(screen.getByRole('button', { name: /deactivate/i }));

    await waitFor(() => expect(mockedOrganizationApi.archiveMembership).toHaveBeenCalledWith(
      'org-1',
      activeMembership.id,
    ));
    expect(await screen.findByText('Membership deactivated.')).toBeInTheDocument();
  });

  it('keeps a conflict message visible when the backend rejects a duplicate assignment', async () => {
    const user = userEvent;
    setupApi();
    mockedOrganizationApi.createMembership.mockRejectedValue(new Error('User already has an active membership.'));

    renderPage();
    await screen.findByRole('heading', { name: 'No memberships found' });

    await user.selectOptions(screen.getByLabelText('User'), activeUser.id);
    await user.selectOptions(screen.getByRole('combobox', { name: /^Branch$/ }), activeBranch.id);
    await user.click(screen.getByRole('button', { name: /assign membership/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('User already has an active membership.');
    expect(screen.getByLabelText('User')).toHaveValue(activeUser.id);
  });
});