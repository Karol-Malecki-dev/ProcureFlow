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

export type { WorkspaceSearchResponse, WorkspaceSearchPage, WorkspaceSearchResult } from './workspace';

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
  PurchaseRequestDashboardProductDto,
  PurchaseRequestDashboardBranchDto,
  PurchaseRequestDashboardDto,
  PurchaseRequestDashboardResponse,
  UpsertBranchMonthlyBudgetRequest,
  DecidePurchaseRequestRequest,
  MarkPurchaseRequestOrderedRequest,
  MarkPurchaseRequestDeliveredRequest,
  PurchaseRequestAttachmentDto,
  PurchaseRequestAttachmentsResponse,
  PurchaseRequestAttachmentResponse,
  PurchaseRequestOperationResponse,
} from './purchaseRequests';
