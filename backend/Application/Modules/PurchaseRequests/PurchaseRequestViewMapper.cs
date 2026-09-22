using Domain.Entities;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Maps the aggregate to application projections without leaking domain entities to API.
/// </summary>
public static class PurchaseRequestViewMapper
{
    public static BranchMonthlyBudgetView ToBudgetView(
        BranchMonthlyBudget budget,
        Guid organizationId)
        => new(
            budget.Id,
            organizationId,
            budget.BranchId,
            budget.Year,
            budget.Month,
            budget.LimitAmount,
            budget.UsedAmount,
            budget.AvailableAmount,
            budget.ConcurrencyStamp);

    public static PurchaseRequestDetailsView ToDetailsView(PurchaseRequest request)
        => new(
            request.Id,
            request.AuthorUserId,
            request.OrganizationId,
            request.BranchId,
            request.Status,
            request.Note,
            request.Items
                .OrderBy(item => item.Id)
                .Select(ToItemView)
                .ToList(),
            request.TotalValue,
            request.CreatedAt,
            request.UpdatedAt,
            request.ConcurrencyStamp);

    public static PurchaseRequestItemView ToItemView(PurchaseRequestItem item)
        => new(
            item.Id,
            item.ProductId,
            item.ProductNameSnapshot,
            item.ProductCodeSnapshot,
            item.UnitNameSnapshot,
            item.UnitSymbolSnapshot,
            item.UnitPriceSnapshot,
            item.Quantity,
            item.Comment,
            item.LineTotal);
}
