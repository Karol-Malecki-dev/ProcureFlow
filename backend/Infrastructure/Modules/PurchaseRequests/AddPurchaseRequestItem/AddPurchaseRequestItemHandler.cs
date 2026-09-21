using Application.Modules.Catalog.ProductRead;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.AddPurchaseRequestItem;

/// <summary>
/// Adds a current catalog product as an immutable request-item snapshot.
/// </summary>
public sealed class AddPurchaseRequestItemHandler : IAddPurchaseRequestItemHandler
{
    private const string ConcurrencyConflictMessage = "Purchase request was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestDraftStore _draftStore;
    private readonly ISelectableProductReader _productReader;

    public AddPurchaseRequestItemHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestDraftStore draftStore,
        ISelectableProductReader productReader)
    {
        _membershipReader = membershipReader;
        _draftStore = draftStore;
        _productReader = productReader;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        AddPurchaseRequestItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request concurrency stamp is required.");
        }

        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty
            || command.ProductId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request and product identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        if (!membership.IsActiveEmployeeScope)
        {
            return Failure(
                membership.Role == Domain.Models.Organizations.Enums.BusinessRole.Employee
                    ? PurchaseRequestOperationStatus.Conflict
                    : PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot edit purchase request drafts in this scope.");
        }

        var request = await _draftStore.GetOwnedDraftAsync(
            membership,
            command.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Purchase request draft was not found.");
        }

        if (!HasExpectedVersion(request.ConcurrencyStamp, command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }

        var product = await _productReader.GetSelectableProductAsync(
            membership.OrganizationId,
            command.ProductId,
            cancellationToken);
        if (product is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Selectable catalog product was not found.");
        }

        try
        {
            request.AddItem(
                product.ProductId,
                product.Name,
                product.Code,
                product.UnitName,
                product.UnitSymbol,
                product.UnitPrice,
                command.Quantity,
                command.Comment);
            await _draftStore.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _draftStore.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }
        catch (DbUpdateException)
        {
            _draftStore.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, "Purchase request item conflicts with another write.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
            PurchaseRequestViewMapper.ToDetailsView(request),
            "Purchase request item added.");
    }

    private static bool HasExpectedVersion(string current, string expected)
        => string.Equals(current, expected.Trim(), StringComparison.Ordinal);

    private static PurchaseRequestOperationResult<PurchaseRequestDetailsView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(status, message);
}
