using Application.Modules.Organization.Membership.GetList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Membership.List;

[ApiController]
[Route("api/organizations/{organizationId:guid}/memberships")]
[Authorize(Roles = "Admin")]
public sealed class ListMembershipsController : ControllerBase
{
    private readonly IListMembershipHandler _handler;

    public ListMembershipsController(IListMembershipHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> ListMemberships(
        Guid organizationId,
        [FromQuery] Guid? branchId = null,
        [FromQuery] Domain.Models.Organizations.Enums.BusinessRole? role = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new ListMembershipsQuery(
                organizationId,
                branchId,
                role,
                includeInactive),
            cancellationToken);

        var statusCode = (int)result.StatusCode;

        if (!result.IsSuccess)
        {
            return StatusCode(
                statusCode,
                ApiResponse<IReadOnlyList<MembershipListResponse>>.Error(
                    statusCode,
                    result.Message));
        }

        var response = result.Value!
            .Select(membership => new MembershipListResponse(
                membership.Id,
                membership.UserId,
                membership.UserDisplayName,
                membership.UserEmail,
                membership.BranchId,
                membership.BranchName,
                membership.Role,
                membership.IsActive))
            .ToList();

        return Ok(
            ApiResponse<IReadOnlyList<MembershipListResponse>>.Success(
                response,
                result.Message));
    }
}