using Application.Modules.PurchaseRequests;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.PurchaseRequests.Fulfillment;

internal static class PurchaseRequestFulfillmentAccess
{
    public static bool CanManage(PurchaseRequestMembership membership)
        => membership.IsActiveProcurementScope
            || membership.IsActivePlatformAdminScope;

    public static PurchaseRequestOperationStatus DeniedStatus(
        PurchaseRequestMembership membership)
        => membership.Role == BusinessRole.Procurement
            ? PurchaseRequestOperationStatus.Conflict
            : PurchaseRequestOperationStatus.Forbidden;
}
