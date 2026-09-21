import type { SelectableProductsResponse } from '../../types/catalog';
import { httpClient, type HttpClient } from './HttpClient';

export class CatalogApi {
  constructor(private readonly client: HttpClient = httpClient) {}

  getSelectableProducts(organizationId: string): Promise<SelectableProductsResponse> {
    return this.client.get<SelectableProductsResponse>(
      `/organizations/${organizationId}/catalog/products`,
    );
  }
}

export const catalogApi = new CatalogApi();
