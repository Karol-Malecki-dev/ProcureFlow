using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Modules.PurchaseRequests.AddPurchaseRequestItem;
using API.Modules.PurchaseRequests.CreatePurchaseRequest;
using API.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using API.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using API.Responses;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.PurchaseRequests;

/// <summary>
/// HTTP adapter for the PF3 purchase-request draft workflow.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/purchase-requests")]
[Authorize]
public sealed class PurchaseRequestsController : ControllerBase
{
    private readonly ICreatePurchaseRequestHandler _createHandler;
    private readonly IAddPurchaseRequestItemHandler _addItemHandler;
    private readonly IUpdatePurchaseRequestItemQuantityHandler _updateItemQuantityHandler;
    private readonly IRemovePurchaseRequestItemHandler _removeItemHandler;
    private readonly IGetPurchaseRequestDetailsHandler _detailsHandler;
    private readonly IListMyPurchaseRequestsHandler _listHandler;

    public PurchaseRequestsController(
        ICreatePurchaseRequestHandler createHandler,
        IAddPurchaseRequestItemHandler addItemHandler,
        IUpdatePurchaseRequestItemQuantityHandler updateItemQuantityHandler,
        IRemovePurchaseRequestItemHandler removeItemHandler,
        IGetPurchaseRequestDetailsHandler detailsHandler,
        IListMyPurchaseRequestsHandler listHandler)
    {
        _createHandler = createHandler;
        _addItemHandler = addItemHandler;
        _updateItemQuantityHandler = updateItemQuantityHandler;
        _removeItemHandler = removeItemHandler;
        _detailsHandler = detailsHandler;
        _listHandler = listHandler;
    }

    /// <summary>Creates an empty draft in the current user's active Employee branch.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid organizationId,
        CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _createHandler.HandleAsync(
            new CreatePurchaseRequestCommand(userId, organizationId, request.Note),
            cancellationToken);

        return ToActionResult(result, MapDetails, StatusCodes.Status201Created);
    }

    /// <summary>Lists the current Employee's own drafts in stable updated order.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        Guid organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestListResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _listHandler.HandleAsync(
            new ListMyPurchaseRequestsQuery(userId, organizationId, page, pageSize),
            cancellationToken);

        return ToActionResult(result, MapList);
    }

    /// <summary>Returns one own draft without exposing requests from another scope.</summary>
    [HttpGet("{purchaseRequestId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        Guid organizationId,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _detailsHandler.HandleAsync(
            new GetPurchaseRequestDetailsQuery(userId, organizationId, purchaseRequestId),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Adds a server-owned catalog snapshot to an own draft.</summary>
    [HttpPost("{purchaseRequestId:guid}/items")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(
        Guid organizationId,
        Guid purchaseRequestId,
        AddPurchaseRequestItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _addItemHandler.HandleAsync(
            new AddPurchaseRequestItemCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ProductId,
                request.Quantity,
                request.Comment,
                request.ConcurrencyStamp),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Updates one item quantity using the expected request version.</summary>
    [HttpPatch("{purchaseRequestId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItemQuantity(
        Guid organizationId,
        Guid purchaseRequestId,
        Guid itemId,
        UpdatePurchaseRequestItemQuantityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _updateItemQuantityHandler.HandleAsync(
            new UpdatePurchaseRequestItemQuantityCommand(
                userId,
                organizationId,
                purchaseRequestId,
                itemId,
                request.Quantity,
                request.ConcurrencyStamp),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Removes one item using the expected request version.</summary>
    [HttpDelete("{purchaseRequestId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveItem(
        Guid organizationId,
        Guid purchaseRequestId,
        Guid itemId,
        RemovePurchaseRequestItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _removeItemHandler.HandleAsync(
            new RemovePurchaseRequestItemCommand(
                userId,
                organizationId,
                purchaseRequestId,
                itemId,
                request.ConcurrencyStamp),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    private IActionResult ToActionResult<TValue, TResponse>(
        PurchaseRequestOperationResult<TValue> result,
        Func<TValue, TResponse> map,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<TResponse>.Error(statusCode, result.Message));
        }

        return StatusCode(
            successStatusCode,
            ApiResponse<TResponse>.Success(
                map(result.Value!),
                result.Message,
                successStatusCode));
    }

    private static PurchaseRequestResponse MapDetails(PurchaseRequestDetailsView view)
        => new(
            view.Id,
            view.AuthorUserId,
            view.OrganizationId,
            view.BranchId,
            view.Status,
            view.Note,
            view.Items.Select(MapItem).ToList(),
            view.TotalValue,
            view.CreatedAt,
            view.UpdatedAt,
            view.ConcurrencyStamp);

    private static PurchaseRequestItemResponse MapItem(PurchaseRequestItemView item)
        => new(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.ProductCode,
            item.UnitName,
            item.UnitSymbol,
            item.UnitPrice,
            item.Quantity,
            item.Comment,
            item.LineTotal);

    private static PurchaseRequestListResponse MapList(PurchaseRequestListView view)
        => new(
            view.Items
                .Select(item => new PurchaseRequestListItemResponse(
                    item.Id,
                    item.Status,
                    item.Note,
                    item.ItemCount,
                    item.TotalValue,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.ConcurrencyStamp))
                .ToList(),
            view.Page,
            view.PageSize,
            view.TotalCount);

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }
}
