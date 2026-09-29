import type { WorkspaceSearchResponse } from '../../types';
import { httpClient, type HttpClient } from './HttpClient';

export class WorkspaceApi {
  constructor(private readonly client: HttpClient = httpClient) {}

  searchWorkspace(query: string, signal?: AbortSignal): Promise<WorkspaceSearchResponse> {
    const params = new URLSearchParams({ query: query.trim(), type: 'purchaseRequest', page: '1', pageSize: '10' });
    return this.client.get<WorkspaceSearchResponse>(`/workspace/search?${params.toString()}`, { signal });
  }
}

export const workspaceApi = new WorkspaceApi();