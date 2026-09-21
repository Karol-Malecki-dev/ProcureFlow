using System.IdentityModel.Tokens.Jwt;
using Application.Modules.Catalog.UnitOfMeasure;
using Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;
using API.Responses;

namespace API.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

[ApiController]
[Route("api/organizations/{organizationId:guid}/catalog/units-of-measure")]
[Authorize]
public sealed class CreateUnitOfMeasureController : ControllerBase
{
    private readonly ICreateUnitOfMeasureHandler _handler;

    public CreateUnitOfMeasureController(ICreateUnitOfMeasureHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUnitOfMeasure(
        Guid organizationId,
        CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(
                ApiResponse<UnitOfMeasureResponse>.Error(
                    401,
                    "Authenticated user identifier is invalid."));
        }

        var result = await _handler.HandleAsync(
            new CreateUnitOfMeasureCommand(
                organizationId,
                userId,
                request.Name,
                request.Symbol),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<UnitOfMeasureResponse>.Error(statusCode, result.Message));
        }

        const int successStatusCode = StatusCodes.Status201Created;
        return StatusCode(
            successStatusCode,
            ApiResponse<UnitOfMeasureResponse>.Success(
                MapUnitOfMeasure(result.Value!),
                result.Message,
                successStatusCode));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }

    private static UnitOfMeasureResponse MapUnitOfMeasure(UnitOfMeasureView unitOfMeasure)
        => new(
            unitOfMeasure.Id,
            unitOfMeasure.OrganizationId,
            unitOfMeasure.Name,
            unitOfMeasure.Symbol,
            unitOfMeasure.IsActive);
}