using System.IdentityModel.Tokens.Jwt;
using API.Responses;
using Application.Modules.PurchaseRequests;
using Domain.Models.Organizations.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.PurchaseRequests;

/// <summary>HTTP adapter for the organization-scoped ProcureFlow dashboard.</summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/dashboard")]
[Authorize]
public sealed class PurchaseRequestDashboardController : ControllerBase
{
    private readonly IGetPurchaseRequestDashboardHandler _handler;

    public PurchaseRequestDashboardController(IGetPurchaseRequestDashboardHandler handler)
    {
        _handler = handler;
    }

    /// <summary>Returns dashboard metrics filtered by the caller's active membership scope.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Get(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestDashboardResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _handler.HandleAsync(
            new GetPurchaseRequestDashboardQuery(userId, organizationId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<PurchaseRequestDashboardResponse>.Error(statusCode, result.Message));
        }

        return Ok(ApiResponse<PurchaseRequestDashboardResponse>.Success(
            Map(result.Value!)));
    }

    private static PurchaseRequestDashboardResponse Map(PurchaseRequestDashboardView dashboard)
        => new(
            dashboard.ScopeRole,
            dashboard.PendingRequestsCount,
            dashboard.CurrentMonthOrderValue,
            dashboard.MostFrequentlyOrderedProducts
                .Select(product => new PurchaseRequestDashboardProductResponse(
                    product.ProductId,
                    product.ProductName,
                    product.ProductCode,
                    product.TotalQuantity,
                    product.RequestCount))
                .ToList(),
            dashboard.SpendingByBranch
                .Select(branch => new PurchaseRequestDashboardBranchResponse(
                    branch.BranchId,
                    branch.BranchName,
                    branch.TotalValue))
                .ToList());
}

/// <summary>JSON contract returned by the organization dashboard endpoint.</summary>
public sealed record PurchaseRequestDashboardResponse(
    BusinessRole ScopeRole,
    int PendingRequestsCount,
    decimal CurrentMonthOrderValue,
    IReadOnlyList<PurchaseRequestDashboardProductResponse> MostFrequentlyOrderedProducts,
    IReadOnlyList<PurchaseRequestDashboardBranchResponse> SpendingByBranch);

/// <summary>Popular product metric in the dashboard response.</summary>
public sealed record PurchaseRequestDashboardProductResponse(
    Guid ProductId,
    string ProductName,
    string? ProductCode,
    decimal TotalQuantity,
    int RequestCount);

/// <summary>Branch spending metric in the dashboard response.</summary>
public sealed record PurchaseRequestDashboardBranchResponse(
    Guid BranchId,
    string BranchName,
    decimal TotalValue);