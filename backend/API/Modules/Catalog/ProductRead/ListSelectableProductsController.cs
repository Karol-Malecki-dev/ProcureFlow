using System.IdentityModel.Tokens.Jwt;
using API.Responses;
using Application.Modules.PurchaseRequests;
using Application.Modules.Catalog.ProductRead;
using Domain.Models.Organizations.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Catalog.ProductRead;

/// <summary>
/// Exposes products that the current Employee can select for a purchase-request draft.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/catalog/products")]
[Authorize]
public sealed class ListSelectableProductsController : ControllerBase
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly ISelectableProductReader _productReader;

    public ListSelectableProductsController(
        IPurchaseRequestMembershipReader membershipReader,
        ISelectableProductReader productReader)
    {
        _membershipReader = membershipReader;
        _productReader = productReader;
    }

    /// <summary>
    /// Lists active and available products in the current Employee's organization scope.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SelectableProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> List(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<IReadOnlyList<SelectableProductResponse>>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            userId,
            organizationId,
            cancellationToken);

        if (membership is null)
        {
            return NotFound(ApiResponse<IReadOnlyList<SelectableProductResponse>>.Error(
                StatusCodes.Status404NotFound,
                "Active organization membership was not found."));
        }

        if (!membership.IsActiveEmployeeScope)
        {
            var statusCode = membership.Role == BusinessRole.Employee
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status403Forbidden;

            return StatusCode(
                statusCode,
                ApiResponse<IReadOnlyList<SelectableProductResponse>>.Error(
                    statusCode,
                    "The current user cannot select catalog products in this scope."));
        }

        var products = await _productReader.GetSelectableProductsAsync(
            organizationId,
            cancellationToken);

        var response = products
            .Select(Map)
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<SelectableProductResponse>>.Success(
            response,
            "Selectable catalog products loaded."));
    }

    private static SelectableProductResponse Map(SelectableProductView product)
        => new(
            product.ProductId,
            product.Name,
            product.Code,
            product.UnitName,
            product.UnitSymbol,
            product.UnitPrice);

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }
}