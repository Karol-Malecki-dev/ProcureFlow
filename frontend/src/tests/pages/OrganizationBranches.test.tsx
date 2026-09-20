import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { vi } from 'vitest';
import OrganizationBranches from '../../pages/OrganizationBranches';
import { organizationApi } from '../../services/api';
import type { BranchDto } from '../../types/organization';

vi.mock('../../services/api', async () => {
  const actual = await vi.importActual<typeof import('../../services/api')>('../../services/api');

  return {
    ...actual,
    organizationApi: {
      getActiveOrganization: vi.fn(),
      getBranches: vi.fn(),
      getBranchDetails: vi.fn(),
      createBranch: vi.fn(),
      updateBranch: vi.fn(),
      archiveBranch: vi.fn(),
    },
  };
});

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

const archivedBranch: BranchDto = {
  ...activeBranch,
  id: 'branch-2',
  name: 'Archived branch',
  code: 'OLD',
  isArchived: true,
};

function apiResponse<T>(data: T) {
  return {
    statusCode: 200,
    message: 'OK',
    data,
    errors: null,
    timestamp: '2026-09-08T00:00:00Z',
  };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/admin/organizations/org-1/branches']}>
      <Routes>
        <Route path="/admin/organizations/:organizationId/branches" element={<OrganizationBranches />} />
      </Routes>
    </MemoryRouter>,
  );
}

function renderActiveOrganizationPage() {
  return render(
    <MemoryRouter initialEntries={['/admin/organization/branches']}>
      <Routes>
        <Route path="/admin/organization/branches" element={<OrganizationBranches />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe('OrganizationBranches page', () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it('renders loading and empty states', async () => {
    mockedOrganizationApi.getBranches.mockResolvedValue(apiResponse([]));

    renderPage();

    expect(screen.getByText('Loading branches...')).toBeInTheDocument();
    expect(await screen.findByRole('heading', { name: 'No branches found' })).toBeInTheDocument();
  });

  it('resolves the active organization on the convenience route', async () => {
    mockedOrganizationApi.getActiveOrganization.mockResolvedValue(apiResponse({
      id: 'org-1',
      name: 'ProcureFlow MVP',
      code: 'PF-MVP',
    }));
    mockedOrganizationApi.getBranches.mockResolvedValue(apiResponse([activeBranch]));

    renderActiveOrganizationPage();

    expect(await screen.findByRole('heading', { name: 'Warsaw branch' })).toBeInTheDocument();
    expect(mockedOrganizationApi.getActiveOrganization).toHaveBeenCalledTimes(1);
    expect(mockedOrganizationApi.getBranches).toHaveBeenCalledWith('org-1', false);
    expect(screen.getByText('ProcureFlow MVP (PF-MVP)')).toBeInTheDocument();
  });

  it('renders an API error state', async () => {
    mockedOrganizationApi.getBranches.mockRejectedValue(new Error('Branch service unavailable'));

    renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent('Branch service unavailable');
  });

  it('creates a branch and refreshes the list', async () => {
    const user = userEvent;
    mockedOrganizationApi.getBranches.mockResolvedValue(apiResponse([]));
    mockedOrganizationApi.createBranch.mockResolvedValue(apiResponse(activeBranch));

    renderPage();
    await screen.findByRole('heading', { name: 'No branches found' });

    await user.type(screen.getByLabelText('Branch name'), 'Warsaw branch');
    await user.type(screen.getByLabelText('Branch code'), 'WAW');
    await user.type(screen.getByLabelText('Street'), 'Main Street');
    await user.type(screen.getByLabelText('Building number'), '1');
    await user.type(screen.getByLabelText('City'), 'Warsaw');
    await user.type(screen.getByLabelText('Postal code'), '00-001');
    await user.type(screen.getByLabelText('Country'), 'Poland');
    await user.click(screen.getByRole('button', { name: /create branch/i }));

    await waitFor(() => expect(mockedOrganizationApi.createBranch).toHaveBeenCalledWith(
      'org-1',
      expect.objectContaining({
        name: 'Warsaw branch',
        code: 'WAW',
        address: expect.objectContaining({ city: 'Warsaw' }),
      }),
    ));
    expect(await screen.findByText('Branch created.')).toBeInTheDocument();
    expect(mockedOrganizationApi.getBranches).toHaveBeenCalledTimes(2);
  });

  it('edits and archives an active branch', async () => {
    const user = userEvent;
    mockedOrganizationApi.getBranches.mockResolvedValue(apiResponse([activeBranch]));
    mockedOrganizationApi.updateBranch.mockResolvedValue(apiResponse({ ...activeBranch, name: 'Updated branch' }));
    mockedOrganizationApi.archiveBranch.mockResolvedValue(apiResponse({ isArchived: true }));

    renderPage();
    expect(await screen.findByRole('heading', { name: 'Warsaw branch' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /^edit$/i }));
    const nameInput = screen.getByLabelText('Branch name');
    await user.clear(nameInput);
    await user.type(nameInput, 'Updated branch');
    await user.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(mockedOrganizationApi.updateBranch).toHaveBeenCalledWith(
      'org-1',
      'branch-1',
      expect.objectContaining({ name: 'Updated branch' }),
    ));

    await user.click(screen.getByRole('button', { name: /archive/i }));

    await waitFor(() => expect(mockedOrganizationApi.archiveBranch).toHaveBeenCalledWith('org-1', 'branch-1'));
    expect(await screen.findByText('Branch archived.')).toBeInTheDocument();
  });

  it('reloads archived branches when the filter is enabled', async () => {
    const user = userEvent;
    mockedOrganizationApi.getBranches.mockImplementation(async (_organizationId, includeArchived) =>
      apiResponse(includeArchived ? [archivedBranch] : [activeBranch]));

    renderPage();
    expect(await screen.findByRole('heading', { name: 'Warsaw branch' })).toBeInTheDocument();

    await user.click(screen.getByLabelText('Include archived branches'));

    expect(await screen.findByRole('heading', { name: 'Archived branch' })).toBeInTheDocument();
    expect(mockedOrganizationApi.getBranches).toHaveBeenLastCalledWith('org-1', true);
  });
});
