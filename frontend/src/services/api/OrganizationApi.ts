import type {
  ArchiveBranchApiResponse,
  CreateBranchRequest,
  CreateBranchResponse,
  GetBranchDetailsResponse,
  GetActiveOrganizationResponse,
  ListBranchesResponse,
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
}

export const organizationApi = new OrganizationApi();
