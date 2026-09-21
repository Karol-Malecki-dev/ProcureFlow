using Domain.Entities;

namespace Application.Modules.PurchaseRequests.CreatePurchaseRequest;

public interface ICreatePurchaseRequestStore
{
    void Add(PurchaseRequest request);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
