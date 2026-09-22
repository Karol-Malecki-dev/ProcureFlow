using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Modules.PurchaseRequests.AddPurchaseRequestItem;
using API.Modules.PurchaseRequests.Approval;
using API.Modules.PurchaseRequests.Budget;
using API.Modules.PurchaseRequests.ChangePurchaseRequestStatus;
using API.Modules.PurchaseRequests.CreatePurchaseRequest;
using API.Modules.PurchaseRequests.Fulfillment;
using API.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using API.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using API.Responses;
using Application.Modules.PurchaseRequests.CancelPurchaseRequest;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;
using Application.Modules.PurchaseRequests.Approval.ListPurchaseRequestApprovalQueue;
using Application.Modules.PurchaseRequests.Budget.GetBranchMonthlyBudget;
using Application.Modules.PurchaseRequests.Budget.UpsertBranchMonthlyBudget;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Application.Modules.PurchaseRequests.Fulfillment.ListPurchaseRequestFulfillmentQueue;
using Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestDelivered;
using Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestOrdered;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Application.Modules.PurchaseRequests.SubmitPurchaseRequest;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.PurchaseRequests;

/// <summary>
/// HTTP adapter for the purchase-request draft, budget and approval workflows.
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
    private readonly ISubmitPurchaseRequestHandler _submitHandler;
    private readonly ICancelPurchaseRequestHandler _cancelHandler;
    private readonly IGetPurchaseRequestDetailsHandler _detailsHandler;
    private readonly IListMyPurchaseRequestsHandler _listHandler;
    private readonly IGetBranchMonthlyBudgetHandler _getBudgetHandler;
    private readonly IUpsertBranchMonthlyBudgetHandler _upsertBudgetHandler;
    private readonly IListPurchaseRequestApprovalQueueHandler _approvalQueueHandler;
    private readonly IDecidePurchaseRequestHandler _decisionHandler;
    private readonly IListPurchaseRequestFulfillmentQueueHandler _fulfillmentQueueHandler;
    private readonly IMarkPurchaseRequestOrderedHandler _markOrderedHandler;
    private readonly IMarkPurchaseRequestDeliveredHandler _markDeliveredHandler;

    public PurchaseRequestsController(
        ICreatePurchaseRequestHandler createHandler,
        IAddPurchaseRequestItemHandler addItemHandler,
        IUpdatePurchaseRequestItemQuantityHandler updateItemQuantityHandler,
        IRemovePurchaseRequestItemHandler removeItemHandler,
        ISubmitPurchaseRequestHandler submitHandler,
        ICancelPurchaseRequestHandler cancelHandler,
        IGetPurchaseRequestDetailsHandler detailsHandler,
        IListMyPurchaseRequestsHandler listHandler,
        IGetBranchMonthlyBudgetHandler getBudgetHandler,
        IUpsertBranchMonthlyBudgetHandler upsertBudgetHandler,
        IListPurchaseRequestApprovalQueueHandler approvalQueueHandler,
        IDecidePurchaseRequestHandler decisionHandler,
        IListPurchaseRequestFulfillmentQueueHandler fulfillmentQueueHandler,
        IMarkPurchaseRequestOrderedHandler markOrderedHandler,
        IMarkPurchaseRequestDeliveredHandler markDeliveredHandler)
    {
        _createHandler = createHandler;
        _addItemHandler = addItemHandler;
        _updateItemQuantityHandler = updateItemQuantityHandler;
        _removeItemHandler = removeItemHandler;
        _submitHandler = submitHandler;
        _cancelHandler = cancelHandler;
        _detailsHandler = detailsHandler;
        _listHandler = listHandler;
        _getBudgetHandler = getBudgetHandler;
        _upsertBudgetHandler = upsertBudgetHandler;
        _approvalQueueHandler = approvalQueueHandler;
        _decisionHandler = decisionHandler;
        _fulfillmentQueueHandler = fulfillmentQueueHandler;
        _markOrderedHandler = markOrderedHandler;
        _markDeliveredHandler = markDeliveredHandler;
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
        [FromQuery] PurchaseRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestListResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _listHandler.HandleAsync(
            new ListMyPurchaseRequestsQuery(userId, organizationId, page, pageSize, status),
            cancellationToken);

        return ToActionResult(result, MapList);
    }

    /// <summary>Returns the Manager or Procurement queue for the current membership.</summary>
    [HttpGet("approval-queue")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestApprovalQueueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApprovalQueue(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestApprovalQueueResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _approvalQueueHandler.HandleAsync(
            new ListPurchaseRequestApprovalQueueQuery(userId, organizationId),
            cancellationToken);

        return ToActionResult(result, MapApprovalQueue);
    }

    /// <summary>Returns accepted requests waiting for Procurement to order or deliver them.</summary>
    [HttpGet("fulfillment-queue")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestFulfillmentQueueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FulfillmentQueue(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestFulfillmentQueueResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _fulfillmentQueueHandler.HandleAsync(
            new ListPurchaseRequestFulfillmentQueueQuery(userId, organizationId),
            cancellationToken);

        return ToActionResult(result, MapFulfillmentQueue);
    }

    /// <summary>Reads one monthly budget in the current user's authorized branch scope.</summary>
    [HttpGet("budgets/{branchId:guid}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ApiResponse<BranchMonthlyBudgetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBudget(
        Guid organizationId,
        Guid branchId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<BranchMonthlyBudgetResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _getBudgetHandler.HandleAsync(
            new GetBranchMonthlyBudgetQuery(userId, organizationId, branchId, year, month),
            cancellationToken);

        return ToActionResult(result, MapBudget);
    }

    /// <summary>Creates or updates a branch-month budget for Procurement or an admin.</summary>
    [HttpPut("budgets/{branchId:guid}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ApiResponse<BranchMonthlyBudgetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpsertBudget(
        Guid organizationId,
        Guid branchId,
        int year,
        int month,
        UpsertBranchMonthlyBudgetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<BranchMonthlyBudgetResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _upsertBudgetHandler.HandleAsync(
            new UpsertBranchMonthlyBudgetCommand(
                userId,
                organizationId,
                branchId,
                year,
                month,
                request.LimitAmount,
                request.ExpectedConcurrencyStamp),
            cancellationToken);

        return ToActionResult(result, MapBudget);
    }

    /// <summary>Approves or rejects one request from the current user's approval queue.</summary>
    [HttpPost("{purchaseRequestId:guid}/decision")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Decide(
        Guid organizationId,
        Guid purchaseRequestId,
        DecidePurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _decisionHandler.HandleAsync(
            new DecidePurchaseRequestCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ConcurrencyStamp,
                request.Approve,
                request.RejectionReason),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Marks one approved purchase request as ordered by Procurement.</summary>
    [HttpPost("{purchaseRequestId:guid}/fulfillment/order")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkOrdered(
        Guid organizationId,
        Guid purchaseRequestId,
        MarkPurchaseRequestOrderedRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _markOrderedHandler.HandleAsync(
            new MarkPurchaseRequestOrderedCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ConcurrencyStamp,
                request.OrderNumber,
                request.FulfillmentNote),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Marks one ordered purchase request as delivered by Procurement.</summary>
    [HttpPost("{purchaseRequestId:guid}/fulfillment/deliver")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkDelivered(
        Guid organizationId,
        Guid purchaseRequestId,
        MarkPurchaseRequestDeliveredRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _markDeliveredHandler.HandleAsync(
            new MarkPurchaseRequestDeliveredCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ConcurrencyStamp,
                request.FulfillmentNote),
            cancellationToken);

        return ToActionResult(result, MapDetails);
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

    /// <summary>Submits an own draft after validating its completeness and version.</summary>
    [HttpPost("{purchaseRequestId:guid}/submit")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(
        Guid organizationId,
        Guid purchaseRequestId,
        PurchaseRequestStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _submitHandler.HandleAsync(
            new SubmitPurchaseRequestCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ConcurrencyStamp),
            cancellationToken);

        return ToActionResult(result, MapDetails);
    }

    /// <summary>Cancels an own draft or submitted request before approval.</summary>
    [HttpPost("{purchaseRequestId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid organizationId,
        Guid purchaseRequestId,
        PurchaseRequestStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _cancelHandler.HandleAsync(
            new CancelPurchaseRequestCommand(
                userId,
                organizationId,
                purchaseRequestId,
                request.ConcurrencyStamp),
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
            view.FulfillmentOrderNumber,
            view.FulfillmentNote,
            view.Items.Select(MapItem).ToList(),
            view.TotalValue,
            view.CreatedAt,
            view.UpdatedAt,
            view.ConcurrencyStamp);

    private static BranchMonthlyBudgetResponse MapBudget(BranchMonthlyBudgetView view)
        => new(
            view.Id,
            view.OrganizationId,
            view.BranchId,
            view.Year,
            view.Month,
            view.LimitAmount,
            view.UsedAmount,
            view.AvailableAmount,
            view.ConcurrencyStamp);

    private static PurchaseRequestApprovalQueueResponse MapApprovalQueue(
        PurchaseRequestApprovalQueueView view)
        => new(
            view.Items
                .Select(item => new PurchaseRequestApprovalQueueItemResponse(
                    item.Id,
                    item.AuthorUserId,
                    item.OrganizationId,
                    item.BranchId,
                    item.Status,
                    item.Note,
                    item.Items.Select(MapItem).ToList(),
                    item.TotalValue,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.ConcurrencyStamp,
                    item.CanDecide,
                    item.QueueRole))
                .ToList(),
            view.QueueRole);

    private static PurchaseRequestFulfillmentQueueResponse MapFulfillmentQueue(
        PurchaseRequestFulfillmentQueueView view)
        => new(
            view.Items
                .Select(item => new PurchaseRequestFulfillmentQueueItemResponse(
                    item.Id,
                    item.AuthorUserId,
                    item.OrganizationId,
                    item.BranchId,
                    item.Status,
                    item.Note,
                    item.FulfillmentOrderNumber,
                    item.FulfillmentNote,
                    item.Items.Select(MapItem).ToList(),
                    item.TotalValue,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.ConcurrencyStamp))
                .ToList());

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
