/**
 * TYPES INDEX - Central export point for all types
 * 
 * Usage:
 * import { User, LoginRequest, ApiResponse } from '@/types';
 */

// Auth types
export type {
  LoginRequest,
  RegisterRequest,
  VerifyTokenRequest,
  ConfirmEmailRequest,
  ResendConfirmationRequest,
  VerifyTwoFactorRequest,
  ResendTwoFactorRequest,
  AuthenticatorSetup,
  ConfirmAuthenticatorSetupRequest,
  AuthenticatorConfirmation,
  DisableAuthenticatorRequest,
  RegenerateAuthenticatorRecoveryCodesRequest,
  JwtTokens,
  RegisterResultData,
  TwoFactorChallenge,
  LoginResponseData,
  LoginFlowResult,
  LoginAuthenticatedResult,
  LoginTwoFactorRequiredResult,
  PendingTwoFactorChallenge,
  AuthUser,
  ChangePasswordRequest,
  LoginResponse,
  RegisterResponse,
  MeResponse,
  VerifyTokenResponse,
  LogoutResponse,
  ErrorDetail,
  ApiErrorResponse,
  AuthState,
  AuthContextType,
  ForgotPasswordRequest,
  ResetPasswordRequest,
} from './auth';

// Runtime config types
export type {
  AppFeatureFlagsDto,
  AppRuntimeConfigurationDto,
} from './runtimeConfig';

// ResetType is a real runtime enum (not just a type), so it must be exported separately
// from the `export type { ... }` block above (isolatedModules forbids mixing them).
export { ResetType } from './auth';

// User types
export type {
  UserDto,
  CreateUserRequest,
  UpdateUserRequest,
  DeleteUserRequest,
  GetUserResponse,
  GetAllUsersResponse,
  GetUserCountResponse,
  CreateUserResponse,
  UpdateUserResponse,
  UpdateDisplayNameResponse,
  UpdateUserRoleResponse,
  DeleteUserResponse,
  PaginatedResponse,
  UserListState,
  UserFormState,
  UserSecurity,
  UpdateTwoFactorPreferenceRequest,
  GetUserSecurityResponse,
  UpdateTwoFactorPreferenceResponse,
} from './user/index';

// Admin types
export type {
  AdminDashboardStatsDto,
  AdminUserListItemDto,
  AdminUserDetailsDto,
  AdminUserFilterRequestDto,
  AdminUpdateUserRequestDto,
} from './admin';

export { AdminUserRole } from './admin';

// API types
export type {
  ApiResponse,
  ApiError,
  AsyncRequest,
  PaginatedRequest,
  ValidationRule,
  ValidationRules,
  FormErrors,
  AxiosErrorResponse,
} from './api';

export { HttpStatusCode } from './api';

// Project management types
export {
  ProjectTaskStatus,
  ProjectTaskPriority,
  ProjectTaskSortBy,
  SortDirection,
  ProjectMemberRole,
  ProjectInvitationStatus,
} from './project';

export type { WorkspaceSearchResponse, WorkspaceSearchPage, WorkspaceSearchResult } from './workspace';

export type {
  ProjectDto,
  ProjectTaskDto,
  ProjectTaskCommentDto,
  ProjectTaskAttachmentDto,
  ProjectMemberDto,
  ProjectMemberUserDto,
  ProjectInvitationDto,
  CreatedProjectInvitationDto,
  CreateProjectRequest,
  UpdateProjectRequest,
  CreateProjectTaskRequest,
  UpdateProjectTaskRequest,
  UpdateProjectTaskStatusRequest,
  ProjectTaskQuery,
  CreateProjectTaskCommentRequest,
  CreateProjectInvitationRequest,
  ProjectsResponse,
  ProjectResponse,
  ProjectTasksResponse,
  ProjectTaskResponse,
  ProjectTaskCommentsResponse,
  ProjectTaskCommentResponse,
  ProjectTaskAttachmentsResponse,
  ProjectTaskAttachmentResponse,
  ProjectOperationResponse,
  ProjectMembersResponse,
  ProjectMemberUsersResponse,
  ProjectMemberResponse,
  ProjectInvitationsResponse,
  CreatedProjectInvitationResponse,
  ProjectInvitationResponse,
  ProjectActivityDto,
  ProjectActivitiesResponse,
  ProjectDashboardDto,
  ProjectDashboardResponse,
} from './project';

// Notification types
export { NotificationType } from './notifications';

export type {
  NotificationDto,
  NotificationPageDto,
  GetNotificationsResponse,
  GetUnreadCountResponse,
  MarkNotificationReadResponse,
  MarkAllNotificationsReadResponse,
  NotificationEmailPreferenceDto,
  UpdateNotificationEmailPreferenceRequest,
  GetNotificationEmailPreferenceResponse,
  UpdateNotificationEmailPreferenceResponse,
} from './notifications';

// Organization and branch types
export type {
  OrganizationAddressDto,
  ActiveOrganizationDto,
  ArchiveMembershipResponse,
  BranchDto,
  BranchListItemDto,
  BranchDetailsDto,
  CreateMembershipRequest,
  CreateMembershipResponse,
  CurrentMembershipDto,
  CreatedBranchDto,
  UpdatedBranchDto,
  CreateBranchAddressRequest,
  CreateBranchRequest,
  UpdateBranchAddressRequest,
  UpdateBranchRequest,
  ArchiveBranchResponse,
  ListBranchesResponse,
  GetBranchDetailsResponse,
  CreateBranchResponse,
  UpdateBranchResponse,
  ArchiveBranchApiResponse,
  GetActiveOrganizationResponse,
  GetCurrentMembershipResponse,
  ListMembershipsResponse,
  MembershipDto,
  MembershipListFilters,
  MembershipListItemDto,
  ArchiveMembershipApiResponse,
  UpdateMembershipRequest,
  UpdateMembershipResponse,
} from './organization';

export { BusinessRole } from './organization';

// Catalog types
export type { SelectableProductDto, SelectableProductsResponse } from './catalog';

// Purchase-request draft types
export { PurchaseRequestStatus } from './purchaseRequests';
export { PurchaseRequestDecisionType } from './purchaseRequests';

export type {
  PurchaseRequestItemDto,
  PurchaseRequestDto,
  PurchaseRequestListItemDto,
  PurchaseRequestListDto,
  CreatePurchaseRequestRequest,
  AddPurchaseRequestItemRequest,
  UpdatePurchaseRequestItemQuantityRequest,
  RemovePurchaseRequestItemRequest,
  PurchaseRequestDetailsResponse,
  PurchaseRequestListResponse,
  BranchMonthlyBudgetDto,
  BranchMonthlyBudgetResponse,
  PurchaseRequestApprovalQueueItemDto,
  PurchaseRequestApprovalQueueDto,
  PurchaseRequestApprovalQueueResponse,
  PurchaseRequestFulfillmentQueueItemDto,
  PurchaseRequestFulfillmentQueueDto,
  PurchaseRequestFulfillmentQueueResponse,
  UpsertBranchMonthlyBudgetRequest,
  DecidePurchaseRequestRequest,
  MarkPurchaseRequestOrderedRequest,
  MarkPurchaseRequestDeliveredRequest,
} from './purchaseRequests';
