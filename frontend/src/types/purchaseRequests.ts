import type { ApiResponse } from './api';
import type { BusinessRole } from './organization';

export enum PurchaseRequestStatus {
  Draft = 1,
  Submitted = 2,
  Approved = 3,
  Rejected = 4,
  Ordered = 5,
  Delivered = 6,
  Cancelled = 7,
  AwaitingProcurementApproval = 8,
}

export enum PurchaseRequestDecisionType {
  Approved = 1,
  Rejected = 2,
  Escalated = 3,
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
  fulfillmentOrderNumber: string | null;
  fulfillmentNote: string | null;
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

export interface BranchMonthlyBudgetDto {
  id: string;
  organizationId: string;
  branchId: string;
  year: number;
  month: number;
  limitAmount: number;
  usedAmount: number;
  availableAmount: number;
  concurrencyStamp: string;
}

export interface PurchaseRequestApprovalQueueItemDto {
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
  canDecide: boolean;
  queueRole: BusinessRole;
}

export interface PurchaseRequestApprovalQueueDto {
  items: PurchaseRequestApprovalQueueItemDto[];
  queueRole: BusinessRole;
}

export interface PurchaseRequestFulfillmentQueueItemDto {
  id: string;
  authorUserId: string;
  organizationId: string;
  branchId: string;
  status: PurchaseRequestStatus;
  note: string | null;
  fulfillmentOrderNumber: string | null;
  fulfillmentNote: string | null;
  items: PurchaseRequestItemDto[];
  totalValue: number;
  createdAt: string;
  updatedAt: string;
  concurrencyStamp: string;
}

export interface PurchaseRequestFulfillmentQueueDto {
  items: PurchaseRequestFulfillmentQueueItemDto[];
}

export interface UpsertBranchMonthlyBudgetRequest {
  limitAmount: number;
  expectedConcurrencyStamp: string | null;
}

export interface DecidePurchaseRequestRequest {
  concurrencyStamp: string;
  approve: boolean;
  rejectionReason: string | null;
}

export interface MarkPurchaseRequestOrderedRequest {
  concurrencyStamp: string;
  orderNumber: string | null;
  fulfillmentNote: string | null;
}

export interface MarkPurchaseRequestDeliveredRequest {
  concurrencyStamp: string;
  fulfillmentNote: string | null;
}

export type PurchaseRequestDetailsResponse = ApiResponse<PurchaseRequestDto>;
export type PurchaseRequestListResponse = ApiResponse<PurchaseRequestListDto>;
export type BranchMonthlyBudgetResponse = ApiResponse<BranchMonthlyBudgetDto>;
export type PurchaseRequestApprovalQueueResponse = ApiResponse<PurchaseRequestApprovalQueueDto>;
export type PurchaseRequestFulfillmentQueueResponse = ApiResponse<PurchaseRequestFulfillmentQueueDto>;
