using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.Approval.ListPurchaseRequestApprovalQueue;

public interface IListPurchaseRequestApprovalQueueHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView>> HandleAsync(
        ListPurchaseRequestApprovalQueueQuery query,
        CancellationToken cancellationToken = default);
}