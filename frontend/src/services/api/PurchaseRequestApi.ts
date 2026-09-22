import type {
  AddPurchaseRequestItemRequest,
  BranchMonthlyBudgetResponse,
  CreatePurchaseRequestRequest,
  DecidePurchaseRequestRequest,
  PurchaseRequestDetailsResponse,
  PurchaseRequestApprovalQueueResponse,
  PurchaseRequestListResponse,
  RemovePurchaseRequestItemRequest,
  UpsertBranchMonthlyBudgetRequest,
  UpdatePurchaseRequestItemQuantityRequest,
} from '../../types/purchaseRequests';
import { httpClient, type HttpClient } from './HttpClient';

export class PurchaseRequestApi {
  constructor(private readonly client: HttpClient = httpClient) {}

  listDrafts(organizationId: string, page = 1, pageSize = 20): Promise<PurchaseRequestListResponse> {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return this.client.get<PurchaseRequestListResponse>(
      `/organizations/${organizationId}/purchase-requests?${query.toString()}`,
    );
  }

  listApprovalQueue(organizationId: string): Promise<PurchaseRequestApprovalQueueResponse> {
    return this.client.get<PurchaseRequestApprovalQueueResponse>(
      `/organizations/${organizationId}/purchase-requests/approval-queue`,
    );
  }

  getBudget(
    organizationId: string,
    branchId: string,
    year: number,
    month: number,
  ): Promise<BranchMonthlyBudgetResponse> {
    return this.client.get<BranchMonthlyBudgetResponse>(
      `/organizations/${organizationId}/purchase-requests/budgets/${branchId}/${year}/${month}`,
    );
  }

  upsertBudget(
    organizationId: string,
    branchId: string,
    year: number,
    month: number,
    request: UpsertBranchMonthlyBudgetRequest,
  ): Promise<BranchMonthlyBudgetResponse> {
    return this.client.put<BranchMonthlyBudgetResponse, UpsertBranchMonthlyBudgetRequest>(
      `/organizations/${organizationId}/purchase-requests/budgets/${branchId}/${year}/${month}`,
      request,
    );
  }

  decide(
    organizationId: string,
    purchaseRequestId: string,
    request: DecidePurchaseRequestRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.post<PurchaseRequestDetailsResponse, DecidePurchaseRequestRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/decision`,
      request,
    );
  }

  getDetails(organizationId: string, purchaseRequestId: string): Promise<PurchaseRequestDetailsResponse> {
    return this.client.get<PurchaseRequestDetailsResponse>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}`,
    );
  }

  createDraft(
    organizationId: string,
    request: CreatePurchaseRequestRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.post<PurchaseRequestDetailsResponse, CreatePurchaseRequestRequest>(
      `/organizations/${organizationId}/purchase-requests`,
      request,
    );
  }

  addItem(
    organizationId: string,
    purchaseRequestId: string,
    request: AddPurchaseRequestItemRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.post<PurchaseRequestDetailsResponse, AddPurchaseRequestItemRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/items`,
      request,
    );
  }

  updateItemQuantity(
    organizationId: string,
    purchaseRequestId: string,
    itemId: string,
    request: UpdatePurchaseRequestItemQuantityRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.patch<PurchaseRequestDetailsResponse, UpdatePurchaseRequestItemQuantityRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/items/${itemId}`,
      request,
    );
  }

  removeItem(
    organizationId: string,
    purchaseRequestId: string,
    itemId: string,
    request: RemovePurchaseRequestItemRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.delete<PurchaseRequestDetailsResponse, RemovePurchaseRequestItemRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/items/${itemId}`,
      request,
    );
  }
}

export const purchaseRequestApi = new PurchaseRequestApi();
