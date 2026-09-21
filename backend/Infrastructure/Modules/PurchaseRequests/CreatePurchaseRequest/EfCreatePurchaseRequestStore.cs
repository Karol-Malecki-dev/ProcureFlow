using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Modules.PurchaseRequests.CreatePurchaseRequest;

public sealed class EfCreatePurchaseRequestStore : ICreatePurchaseRequestStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfCreatePurchaseRequestStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(PurchaseRequest request)
        => _dbContext.PurchaseRequests.Add(request);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
