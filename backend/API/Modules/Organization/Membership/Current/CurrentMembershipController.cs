using System.IdentityModel.Tokens.Jwt;
using API.Responses;
using Application.Modules.Organization.Membership.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Membership.Current;

[ApiController]
[Route("api/memberships/current")]
[Authorize]
public sealed class CurrentMembershipController : ControllerBase
{
    private readonly IGetMembershipDetailsHandler _handler;

    public CurrentMembershipController(IGetMembershipDetailsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentMembership(
        CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(
                ApiResponse<CurrentMembershipResponse>.Error(401, "Authenticated user identifier is invalid."));
        }

        var result = await _handler.HandleAsync(
            new GetMembershipQueary(userId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<CurrentMembershipResponse>.Error(statusCode, result.Message));
        }

        var membership = result.Value!;

        return Ok(
            ApiResponse<CurrentMembershipResponse>.Success(
                new CurrentMembershipResponse(
                    membership.Id,
                    membership.OrganizationId,
                    membership.UserId,
                    membership.BranchId,
                    membership.Role,
                    membership.IsActive)));
    }
}