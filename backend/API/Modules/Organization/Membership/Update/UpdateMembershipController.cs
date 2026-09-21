using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Update;
using API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Membership.Update;

[ApiController]
[Route("api/organizations/{organizationId:guid}/memberships")]
[Authorize(Roles = "Admin")]
public sealed class UpdateMembershipController : ControllerBase
{
    private readonly IUpdateMembershipHandler _handler;

    public UpdateMembershipController(IUpdateMembershipHandler handler)
    {
        _handler = handler;
    }

    [HttpPut("{membershipId:guid}")]
    public async Task<IActionResult> UpdateMembership(
        Guid organizationId,
        Guid membershipId,
        UpdateMembershipRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new UpdateMembershipCommand(
                membershipId,
                organizationId,
                request.BranchId,
                request.Role),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<MembershipResponse>.Error(statusCode, result.Message));
        }

        return Ok(
            ApiResponse<MembershipResponse>.Success(
                MapMembership(result.Value!),
                result.Message));
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