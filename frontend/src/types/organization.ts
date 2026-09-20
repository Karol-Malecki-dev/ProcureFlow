import type { ApiResponse } from './api';

export interface OrganizationAddressDto {
  street: string;
  buildingNumber: string;
  apartmentNumber: string | null;
  city: string;
  postalCode: string;
  country: string;
}

export interface ActiveOrganizationDto {
  id: string;
  name: string;
  code: string;
}

export enum BusinessRole {
  Employee = 1,
  Manager = 2,
  Procurement = 3,
}

export interface BranchDto {
  id: string;
  name: string;
  code: string;
  isArchived: boolean;
  address: OrganizationAddressDto;
}

export type BranchListItemDto = BranchDto;
export type BranchDetailsDto = BranchDto;
export type CreatedBranchDto = BranchDto;
export type UpdatedBranchDto = BranchDto;

export interface CreateBranchAddressRequest {
  street: string;
  buildingNumber: string;
  apartmentNumber: string | null;
  city: string;
  postalCode: string;
  country: string;
}

export interface CreateBranchRequest {
  name: string;
  code: string;
  address: CreateBranchAddressRequest;
}

export type UpdateBranchAddressRequest = CreateBranchAddressRequest;
export type UpdateBranchRequest = CreateBranchRequest;

export interface ArchiveBranchResponse {
  isArchived: boolean;
}

export type ListBranchesResponse = ApiResponse<BranchListItemDto[]>;
export type GetBranchDetailsResponse = ApiResponse<BranchDetailsDto>;
export type CreateBranchResponse = ApiResponse<CreatedBranchDto>;
export type UpdateBranchResponse = ApiResponse<UpdatedBranchDto>;
export type ArchiveBranchApiResponse = ApiResponse<ArchiveBranchResponse>;
export type GetActiveOrganizationResponse = ApiResponse<ActiveOrganizationDto>;

export interface MembershipListItemDto {
  id: string;
  userId: string;
  userDisplayName: string;
  userEmail: string;
  branchId: string | null;
  branchName: string | null;
  role: BusinessRole;
  isActive: boolean;
}

export interface MembershipDto {
  id: string;
  organizationId: string;
  userId: string;
  branchId: string | null;
  role: BusinessRole;
  isActive: boolean;
}

export interface CurrentMembershipDto extends MembershipDto {}

export interface CreateMembershipRequest {
  userId: string;
  branchId: string | null;
  role: BusinessRole;
}

export type UpdateMembershipRequest = Omit<CreateMembershipRequest, 'userId'>;

export interface MembershipListFilters {
  branchId?: string;
  role?: BusinessRole;
  includeInactive?: boolean;
}

export interface ArchiveMembershipResponse {
  isArchived: boolean;
}

export type ListMembershipsResponse = ApiResponse<MembershipListItemDto[]>;
export type CreateMembershipResponse = ApiResponse<MembershipDto>;
export type UpdateMembershipResponse = ApiResponse<MembershipDto>;
export type ArchiveMembershipApiResponse = ApiResponse<ArchiveMembershipResponse>;
export type GetCurrentMembershipResponse = ApiResponse<CurrentMembershipDto>;
