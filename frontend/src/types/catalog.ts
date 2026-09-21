import type { ApiResponse } from './api';

export interface SelectableProductDto {
  id: string;
  name: string;
  code: string | null;
  unitName: string;
  unitSymbol: string;
  unitPrice: number;
}

export type SelectableProductsResponse = ApiResponse<SelectableProductDto[]>;
