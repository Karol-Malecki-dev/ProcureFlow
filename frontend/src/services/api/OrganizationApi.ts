import type {
  ArchiveBranchApiResponse,
  ArchiveMembershipApiResponse,
  CreateMembershipRequest,
  CreateMembershipResponse,
  CreateBranchRequest,
  CreateBranchResponse,
  GetCurrentMembershipResponse,
  GetBranchDetailsResponse,
  GetActiveOrganizationResponse,
  ListMembershipsResponse,
  MembershipListFilters,
  ListBranchesResponse,
  UpdateMembershipRequest,
  UpdateMembershipResponse,
  UpdateBranchRequest,
  UpdateBranchResponse,
} from '../../types/organization';
import { httpClient, type HttpClient } from './HttpClient';

export class OrganizationApi {
  constructor(private readonly client: HttpClient = httpClient) {}

  getActiveOrganization(): Promise<GetActiveOrganizationResponse> {
    return this.client.get<GetActiveOrganizationResponse>('/organizations/active');
  }

  getBranches(organizationId: string, includeArchived = false): Promise<ListBranchesResponse> {
    const query = includeArchived ? '?includeArchived=true' : '';
    return this.client.get<ListBranchesResponse>(
      `/organizations/${organizationId}/branches${query}`,
    );
  }

  getBranchDetails(organizationId: string, branchId: string, includeArchived = false): Promise<GetBranchDetailsResponse> {
    const query = includeArchived ? '?includeArchived=true' : '';
    return this.client.get<GetBranchDetailsResponse>(
      `/organizations/${organizationId}/branches/${branchId}${query}`,
    );
  }

  createBranch(organizationId: string, request: CreateBranchRequest): Promise<CreateBranchResponse> {
    return this.client.post<CreateBranchResponse, CreateBranchRequest>(
      `/organizations/${organizationId}/branches`,
      request,
    );
  }

  updateBranch(
    organizationId: string,
    branchId: string,
    request: UpdateBranchRequest,
  ): Promise<UpdateBranchResponse> {
    return this.client.put<UpdateBranchResponse, UpdateBranchRequest>(
      `/organizations/${organizationId}/branches/${branchId}`,
      request,
    );
  }

  archiveBranch(organizationId: string, branchId: string): Promise<ArchiveBranchApiResponse> {
    return this.client.post<ArchiveBranchApiResponse>(
      `/organizations/${organizationId}/branches/${branchId}/archive`,
    );
  }

  getMemberships(organizationId: string, filters: MembershipListFilters = {}): Promise<ListMembershipsResponse> {
    const searchParams = new URLSearchParams();
    if (filters.branchId) {
      searchParams.set('branchId', filters.branchId);
    }
    if (filters.role !== undefined) {
      searchParams.set('role', String(filters.role));
    }
    if (filters.includeInactive) {
      searchParams.set('includeInactive', 'true');
    }

    const query = searchParams.toString();
    return this.client.get<ListMembershipsResponse>(
      `/organizations/${organizationId}/memberships${query ? `?${query}` : ''}`,
    );
  }

  createMembership(organizationId: string, request: CreateMembershipRequest): Promise<CreateMembershipResponse> {
    return this.client.post<CreateMembershipResponse, CreateMembershipRequest>(
      `/organizations/${organizationId}/memberships`,
      request,
    );
  }

  updateMembership(
    organizationId: string,
    membershipId: string,
    request: UpdateMembershipRequest,
  ): Promise<UpdateMembershipResponse> {
    return this.client.put<UpdateMembershipResponse, UpdateMembershipRequest>(
      `/organizations/${organizationId}/memberships/${membershipId}`,
      request,
    );
  }

  archiveMembership(organizationId: string, membershipId: string): Promise<ArchiveMembershipApiResponse> {
    return this.client.post<ArchiveMembershipApiResponse>(
      `/organizations/${organizationId}/memberships/${membershipId}/archive`,
    );
  }

  getCurrentMembership(): Promise<GetCurrentMembershipResponse> {
    return this.client.get<GetCurrentMembershipResponse>('/memberships/current');
  }
}

export const organizationApi = new OrganizationApi();
