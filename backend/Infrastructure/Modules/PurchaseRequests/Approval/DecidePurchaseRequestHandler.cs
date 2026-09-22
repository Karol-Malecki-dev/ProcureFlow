using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;
using Domain.Entities;
using Domain.Enums;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Approval;

/// <summary>
/// Applies Manager and Procurement decisions with request, budget, decision and
/// lifecycle history changes persisted atomically.
/// </summary>
public sealed class DecidePurchaseRequestHandler : IDecidePurchaseRequestHandler
{
    private const string ConcurrencyConflictMessage = "Purchase request or budget was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestApprovalStore _store;

    public DecidePurchaseRequestHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestApprovalStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        DecidePurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Approval identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Purchase request concurrency stamp is required.");
        }

        if (!command.Approve && string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "A rejection reason is required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        var isManager = membership.IsActiveManagerScope;
        var isProcurement = membership.IsActiveProcurementScope;
        if (!isManager && !isProcurement)
        {
            return Failure(
                membership.Role is BusinessRole.Manager or BusinessRole.Procurement
                    ? PurchaseRequestOperationStatus.Conflict
                    : PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot decide purchase requests in this scope.");
        }

        var request = isManager
            ? await _store.GetManagerRequestAsync(membership, command.PurchaseRequestId, cancellationToken)
            : await _store.GetProcurementRequestAsync(membership, command.PurchaseRequestId, cancellationToken);
        if (request is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Purchase request was not found.");
        }

        if (request.AuthorUserId == command.UserId)
        {
            return Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "The request author cannot approve or reject their own purchase request.");
        }

        if (!HasExpectedVersion(request.ConcurrencyStamp, command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }

        var previousStatus = request.Status;
        var now = DateTime.UtcNow;
        string successMessage;

        try
        {
            if (!command.Approve)
            {
                var requiredStatus = isManager
                    ? PurchaseRequestStatus.Submitted
                    : PurchaseRequestStatus.AwaitingProcurementApproval;
                if (request.Status != requiredStatus)
                {
                    return Failure(
                        PurchaseRequestOperationStatus.Conflict,
                        "This purchase request is no longer waiting for this approval decision.");
                }

                request.Reject(command.RejectionReason!);
                _store.AddDecision(PurchaseRequestApprovalDecision.Create(
                    request.Id,
                    command.UserId,
                    isManager ? BusinessRole.Manager : BusinessRole.Procurement,
                    PurchaseRequestDecisionType.Rejected,
                    request.TotalValue,
                    reason: command.RejectionReason,
                    decidedAtUtc: now));
                successMessage = "Purchase request rejected.";
            }
            else if (isManager)
            {
                if (request.Status != PurchaseRequestStatus.Submitted)
                {
                    return Failure(
                        PurchaseRequestOperationStatus.Conflict,
                        "Only submitted purchase requests can be decided by a Manager.");
                }

                var budgetPeriod = GetCurrentBudgetPeriod();
                var budget = await _store.GetBudgetAsync(
                    request.BranchId,
                    budgetPeriod.Year,
                    budgetPeriod.Month,
                    cancellationToken);
                if (budget is null)
                {
                    return Failure(
                        PurchaseRequestOperationStatus.NotFound,
                        "A monthly budget must be configured before a Manager can approve this request.");
                }

                var availableBudget = budget.AvailableAmount;
                if (request.TotalValue <= availableBudget)
                {
                    request.Approve();
                    if (request.TotalValue > 0m)
                    {
                        budget.Reserve(request.TotalValue);
                    }

                    _store.AddDecision(PurchaseRequestApprovalDecision.Create(
                        request.Id,
                        command.UserId,
                        BusinessRole.Manager,
                        PurchaseRequestDecisionType.Approved,
                        request.TotalValue,
                        availableBudget,
                        budgetYear: budgetPeriod.Year,
                        budgetMonth: budgetPeriod.Month,
                        decidedAtUtc: now));
                    successMessage = "Purchase request approved within the monthly budget.";
                }
                else
                {
                    request.EscalateToProcurement();
                    _store.AddDecision(PurchaseRequestApprovalDecision.Create(
                        request.Id,
                        command.UserId,
                        BusinessRole.Manager,
                        PurchaseRequestDecisionType.Escalated,
                        request.TotalValue,
                        availableBudget,
                        Math.Max(0m, request.TotalValue - availableBudget),
                        budgetYear: budgetPeriod.Year,
                        budgetMonth: budgetPeriod.Month,
                        decidedAtUtc: now));
                    successMessage = "Purchase request escalated to Procurement because it exceeds the available budget.";
                }
            }
            else
            {
                if (request.Status != PurchaseRequestStatus.AwaitingProcurementApproval)
                {
                    return Failure(
                        PurchaseRequestOperationStatus.Conflict,
                        "Only escalated purchase requests can be decided by Procurement.");
                }

                var budgetPeriod = GetCurrentBudgetPeriod();
                var budget = await _store.GetBudgetAsync(
                    request.BranchId,
                    budgetPeriod.Year,
                    budgetPeriod.Month,
                    cancellationToken);
                if (budget is null)
                {
                    return Failure(
                        PurchaseRequestOperationStatus.NotFound,
                        "The monthly budget for this request was not found.");
                }

                var availableBudget = budget.AvailableAmount;
                var overBudgetAmount = Math.Max(0m, request.TotalValue - availableBudget);
                request.Approve();
                if (request.TotalValue > 0m)
                {
                    budget.ReserveOverLimit(request.TotalValue);
                }

                _store.AddDecision(PurchaseRequestApprovalDecision.Create(
                    request.Id,
                    command.UserId,
                    BusinessRole.Procurement,
                    PurchaseRequestDecisionType.Approved,
                    request.TotalValue,
                    availableBudget,
                    overBudgetAmount,
                    budgetYear: budgetPeriod.Year,
                    budgetMonth: budgetPeriod.Month,
                    decidedAtUtc: now));
                successMessage = "Purchase request approved by Procurement.";
            }

            _store.AddStatusHistory(PurchaseRequestStatusHistory.Create(
                request.Id,
                previousStatus,
                request.Status,
                command.UserId,
                now));
            await _store.SaveChangesInTransactionAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.ValidationError, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }
        catch (DbUpdateException exception) when (
            PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                exception,
                "UX_PurchaseRequestApprovalDecisions_Request_Role"))
        {
            _store.ClearChangeTracker();
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                "A decision for this purchase request and approval role already exists.");
        }
        catch (DbUpdateException)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, "Purchase request decision could not be saved.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
            PurchaseRequestViewMapper.ToDetailsView(request),
            successMessage);
    }

    private static (int Year, int Month) GetCurrentBudgetPeriod()
    {
        var now = DateTime.UtcNow;
        return (now.Year, now.Month);
    }

    private static bool HasExpectedVersion(string current, string expected)
        => string.Equals(current, expected.Trim(), StringComparison.Ordinal);

    private static PurchaseRequestOperationResult<PurchaseRequestDetailsView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(status, message);
}
