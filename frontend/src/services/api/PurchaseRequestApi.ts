import type {
  AddPurchaseRequestItemRequest,
  BranchMonthlyBudgetResponse,
  CreatePurchaseRequestRequest,
  DecidePurchaseRequestRequest,
  MarkPurchaseRequestDeliveredRequest,
  MarkPurchaseRequestOrderedRequest,
  PurchaseRequestAttachmentResponse,
  PurchaseRequestAttachmentsResponse,
  PurchaseRequestOperationResponse,
  PurchaseRequestDetailsResponse,
  PurchaseRequestApprovalQueueResponse,
  PurchaseRequestFulfillmentQueueResponse,
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

  listFulfillmentQueue(organizationId: string): Promise<PurchaseRequestFulfillmentQueueResponse> {
    return this.client.get<PurchaseRequestFulfillmentQueueResponse>(
      `/organizations/${organizationId}/purchase-requests/fulfillment-queue`,
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

  markOrdered(
    organizationId: string,
    purchaseRequestId: string,
    request: MarkPurchaseRequestOrderedRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.post<PurchaseRequestDetailsResponse, MarkPurchaseRequestOrderedRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/fulfillment/order`,
      request,
    );
  }

  markDelivered(
    organizationId: string,
    purchaseRequestId: string,
    request: MarkPurchaseRequestDeliveredRequest,
  ): Promise<PurchaseRequestDetailsResponse> {
    return this.client.post<PurchaseRequestDetailsResponse, MarkPurchaseRequestDeliveredRequest>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/fulfillment/deliver`,
      request,
    );
  }

  listAttachments(
    organizationId: string,
    purchaseRequestId: string,
  ): Promise<PurchaseRequestAttachmentsResponse> {
    return this.client.get<PurchaseRequestAttachmentsResponse>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/attachments`,
    );
  }

  uploadAttachment(
    organizationId: string,
    purchaseRequestId: string,
    file: File,
  ): Promise<PurchaseRequestAttachmentResponse> {
    const form = new FormData();
    form.append('file', file);
    return this.client.post<PurchaseRequestAttachmentResponse, FormData>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/attachments`,
      form,
    );
  }

  downloadAttachment(
    organizationId: string,
    purchaseRequestId: string,
    attachmentId: string,
  ): Promise<Blob> {
    return this.client.getBlob(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/attachments/${attachmentId}/download`,
    );
  }

  deleteAttachment(
    organizationId: string,
    purchaseRequestId: string,
    attachmentId: string,
  ): Promise<PurchaseRequestOperationResponse> {
    return this.client.delete<PurchaseRequestOperationResponse>(
      `/organizations/${organizationId}/purchase-requests/${purchaseRequestId}/attachments/${attachmentId}`,
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
