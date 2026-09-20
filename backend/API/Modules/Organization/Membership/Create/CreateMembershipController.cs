using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Create;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Membership.Create;

[ApiController]
[Route("api/organizations/{organizationId:guid}/memberships")]
[Authorize(Roles = "Admin")]
public sealed class CreateMembershipController : ControllerBase
{
    private readonly ICreateMembershipHandler _handler;

    public CreateMembershipController(ICreateMembershipHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMembership(
        Guid organizationId,
        CreateMembershipRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new CreateMembershipCommand(
                organizationId,
                request.UserId,
                request.BranchId,
                request.Role),
            cancellationToken);

        var statusCode = (int)result.StatusCode;

        if (!result.IsSuccess)
        {
            return StatusCode(
                statusCode,
                ApiResponse<MembershipResponse>.Error(statusCode, result.Message));
        }

        return StatusCode(
            statusCode,
            ApiResponse<MembershipResponse>.Success(
                MapMembership(result.Value!),
                result.Message,
                statusCode));
    }

    private static MembershipResponse MapMembership(MembershipView membership)
        => new(
            membership.Id,
            membership.OrganizationId,
            membership.UserId,
            membership.BranchId,
            membership.Role,
            membership.IsActive);
}