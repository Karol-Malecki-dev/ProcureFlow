using Application.Modules.PurchaseRequests;
using Domain.Entities;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

internal static class PurchaseRequestAttachmentAccess
{
    public static bool CanRead(
        PurchaseRequestMembership membership,
        PurchaseRequest request,
        Guid userId)
        => membership.IsActivePlatformAdminScope
            || membership.IsActiveProcurementScope
            || (membership.IsActiveManagerScope
                && membership.BranchId == request.BranchId)
            || (membership.IsActiveEmployeeScope
                && membership.BranchId == request.BranchId
                && request.AuthorUserId == userId);

    public static bool CanUpload(
        PurchaseRequestMembership membership,
        PurchaseRequest request,
        Guid userId)
        => membership.IsActiveEmployeeScope
            && membership.BranchId == request.BranchId
            && request.AuthorUserId == userId
            && request.Status == Domain.Enums.PurchaseRequestStatus.Draft;

    public static bool CanDelete(
        PurchaseRequestMembership membership,
        PurchaseRequest request,
        PurchaseRequestAttachment attachment,
        Guid userId)
        => CanUpload(membership, request, userId)
            && attachment.UploadedByUserId == userId;
}
