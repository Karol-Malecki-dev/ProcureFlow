import type {
  AddPurchaseRequestItemRequest,
  CreatePurchaseRequestRequest,
  PurchaseRequestDetailsResponse,
  PurchaseRequestListResponse,
  RemovePurchaseRequestItemRequest,
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
