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
