import type { ApiResponse } from './api';

export enum PurchaseRequestStatus {
  Draft = 1,
  Submitted = 2,
  Approved = 3,
  Rejected = 4,
  Ordered = 5,
  Delivered = 6,
  Cancelled = 7,
}

export interface PurchaseRequestItemDto {
  id: string;
  productId: string;
  productName: string;
  productCode: string | null;
  unitName: string;
  unitSymbol: string;
  unitPrice: number;
  quantity: number;
  comment: string | null;
  lineTotal: number;
}

export interface PurchaseRequestDto {
  id: string;
  authorUserId: string;
  organizationId: string;
  branchId: string;
  status: PurchaseRequestStatus;
  note: string | null;
  items: PurchaseRequestItemDto[];
  totalValue: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface PurchaseRequestListItemDto {
  id: string;
  status: PurchaseRequestStatus;
  note: string | null;
  itemCount: number;
  totalValue: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface PurchaseRequestListDto {
  items: PurchaseRequestListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreatePurchaseRequestRequest {
  note: string | null;
}

export interface AddPurchaseRequestItemRequest {
  productId: string;
  quantity: number;
  comment: string | null;
  concurrencyStamp: string;
}

export interface UpdatePurchaseRequestItemQuantityRequest {
  quantity: number;
  concurrencyStamp: string;
}

export interface RemovePurchaseRequestItemRequest {
  concurrencyStamp: string;
}

export type PurchaseRequestDetailsResponse = ApiResponse<PurchaseRequestDto>;
export type PurchaseRequestListResponse = ApiResponse<PurchaseRequestListDto>;
