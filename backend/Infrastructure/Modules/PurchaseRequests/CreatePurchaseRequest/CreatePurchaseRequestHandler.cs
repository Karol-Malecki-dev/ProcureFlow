using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Domain.Entities;

namespace Infrastructure.Modules.PurchaseRequests.CreatePurchaseRequest;

/// <summary>
/// Creates an empty draft from the current Employee membership scope.
/// </summary>
public sealed class CreatePurchaseRequestHandler : ICreatePurchaseRequestHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly ICreatePurchaseRequestStore _store;

    public CreatePurchaseRequestHandler(
        IPurchaseRequestMembershipReader membershipReader,
        ICreatePurchaseRequestStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        CreatePurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty || command.OrganizationId == Guid.Empty)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "User and organization identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);

        if (membership is null)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        if (membership.Role != Domain.Models.Organizations.Enums.BusinessRole.Employee)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "Only an Employee can create a purchase request draft.");
        }

        if (!membership.IsActiveEmployeeScope)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.Conflict,
                "The current organization or branch is not active.");
        }

        try
        {
            var request = PurchaseRequest.Create(
                command.UserId,
                membership.OrganizationId,
                membership.BranchId!.Value,
                command.Note);
            _store.Add(request);
            await _store.SaveChangesAsync(cancellationToken);

            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
                PurchaseRequestViewMapper.ToDetailsView(request),
                "Purchase request draft created.");
        }
        catch (ArgumentException exception)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                exception.Message);
        }
    }
}
