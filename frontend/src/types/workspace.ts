export interface WorkspaceSearchResult {
  type: 'purchaseRequest';
  resourceId: string;
  title: string;
  context: string;
}

export interface WorkspaceSearchPage {
  items: WorkspaceSearchResult[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface WorkspaceSearchResponse {
  statusCode: number;
  message: string;
  data: WorkspaceSearchPage;
}