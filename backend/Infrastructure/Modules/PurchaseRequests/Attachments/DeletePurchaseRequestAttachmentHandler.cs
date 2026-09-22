using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.DeletePurchaseRequestAttachment;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>Deletes draft attachment metadata and schedules binary cleanup.</summary>
public sealed class DeletePurchaseRequestAttachmentHandler
    : IDeletePurchaseRequestAttachmentHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestAttachmentStore _store;

    public DeletePurchaseRequestAttachmentHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestAttachmentStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<bool>> HandleAsync(
        DeletePurchaseRequestAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty
            || command.AttachmentId == Guid.Empty)
        {
            return PurchaseRequestOperationResult<bool>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Attachment identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return PurchaseRequestOperationResult<bool>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        var request = await _store.GetRequestAsync(
            command.OrganizationId,
            command.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return PurchaseRequestOperationResult<bool>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request not found.");
        }

        var attachment = await _store.GetAttachmentAsync(
            command.PurchaseRequestId,
            command.AttachmentId,
            cancellationToken);
        if (attachment is null)
        {
            return PurchaseRequestOperationResult<bool>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request attachment not found.");
        }

        if (!PurchaseRequestAttachmentAccess.CanDelete(
                membership,
                request,
                attachment,
                command.UserId))
        {
            return PurchaseRequestOperationResult<bool>.Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "Only the request author can delete attachments from a draft.");
        }

        await _store.DeleteAsync(attachment, cancellationToken);

        return PurchaseRequestOperationResult<bool>.Success(
            true,
            "Purchase request attachment deleted.");
    }
}
