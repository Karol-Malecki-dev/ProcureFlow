using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Modules.PurchaseRequests;
using API.Responses;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.CreatePurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.DeletePurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.DownloadPurchaseRequestAttachment;
using Application.Modules.PurchaseRequests.Attachments.ListPurchaseRequestAttachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.PurchaseRequests.Attachments;

/// <summary>HTTP adapter for request-scoped attachment operations.</summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/purchase-requests/{purchaseRequestId:guid}/attachments")]
[Authorize]
public sealed class PurchaseRequestAttachmentsController : ControllerBase
{
    private const long MaxRequestSizeBytes = 11 * 1024 * 1024;

    private readonly ICreatePurchaseRequestAttachmentHandler _createHandler;
    private readonly IListPurchaseRequestAttachmentsHandler _listHandler;
    private readonly IDownloadPurchaseRequestAttachmentHandler _downloadHandler;
    private readonly IDeletePurchaseRequestAttachmentHandler _deleteHandler;

    public PurchaseRequestAttachmentsController(
        ICreatePurchaseRequestAttachmentHandler createHandler,
        IListPurchaseRequestAttachmentsHandler listHandler,
        IDownloadPurchaseRequestAttachmentHandler downloadHandler,
        IDeletePurchaseRequestAttachmentHandler deleteHandler)
    {
        _createHandler = createHandler;
        _listHandler = listHandler;
        _downloadHandler = downloadHandler;
        _deleteHandler = deleteHandler;
    }

    /// <summary>Uploads one attachment to the current user's draft request.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequestAttachmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid organizationId,
        Guid purchaseRequestId,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<PurchaseRequestAttachmentResponse>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        if (file is null)
        {
            return BadRequest(ApiResponse<PurchaseRequestAttachmentResponse>.Error(
                StatusCodes.Status400BadRequest,
                "Attachment file is required."));
        }

        await using var content = file.OpenReadStream();
        var result = await _createHandler.HandleAsync(
            new CreatePurchaseRequestAttachmentCommand(
                userId,
                organizationId,
                purchaseRequestId,
                file.FileName,
                file.ContentType,
                file.Length,
                content),
            cancellationToken);

        return ToActionResult(
            result,
            MapAttachment,
            StatusCodes.Status201Created);
    }

    /// <summary>Lists attachments visible to the current user for a request.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PurchaseRequestAttachmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid organizationId,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<IReadOnlyList<PurchaseRequestAttachmentResponse>>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _listHandler.HandleAsync(
            new ListPurchaseRequestAttachmentsQuery(
                userId,
                organizationId,
                purchaseRequestId),
            cancellationToken);

        return ToActionResult(
            result,
            values => values.Select(MapAttachment).ToList());
    }

    /// <summary>Downloads one attachment visible to the current user.</summary>
    [HttpGet("{attachmentId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid organizationId,
        Guid purchaseRequestId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<object>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _downloadHandler.HandleAsync(
            new DownloadPurchaseRequestAttachmentQuery(
                userId,
                organizationId,
                purchaseRequestId,
                attachmentId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Status, result.Message);
        }

        return File(
            result.Value!.Content,
            result.Value.ContentType,
            result.Value.OriginalFileName);
    }

    /// <summary>Deletes one attachment from the current user's draft request.</summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid organizationId,
        Guid purchaseRequestId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(ApiResponse<bool>.Error(
                StatusCodes.Status401Unauthorized,
                "Authenticated user identifier is invalid."));
        }

        var result = await _deleteHandler.HandleAsync(
            new DeletePurchaseRequestAttachmentCommand(
                userId,
                organizationId,
                purchaseRequestId,
                attachmentId),
            cancellationToken);

        return ToActionResult(result, value => value);
    }

    private IActionResult ToActionResult<TValue, TResponse>(
        PurchaseRequestOperationResult<TValue> result,
        Func<TValue, TResponse> map,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Status, result.Message);
        }

        return StatusCode(
            successStatusCode,
            ApiResponse<TResponse>.Success(
                map(result.Value!),
                result.Message,
                successStatusCode));
    }

    private IActionResult ToErrorResult(
        PurchaseRequestOperationStatus status,
        string message)
    {
        var statusCode = OperationResultStatusCodeMapper.Map(status);
        return StatusCode(
            statusCode,
            ApiResponse<object>.Error(statusCode, message));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    private static PurchaseRequestAttachmentResponse MapAttachment(
        PurchaseRequestAttachmentView view)
        => new(
            view.Id,
            view.PurchaseRequestId,
            view.UploadedByUserId,
            view.OriginalFileName,
            view.ContentType,
            view.SizeBytes,
            new DateTimeOffset(view.CreatedAt));
}
