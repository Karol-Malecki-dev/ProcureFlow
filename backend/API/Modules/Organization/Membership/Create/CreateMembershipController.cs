using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Create;
using API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        if (!result.IsSuccess)
        {
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<MembershipResponse>.Error(statusCode, result.Message));
        }

        const int successStatusCode = StatusCodes.Status201Created;
        return StatusCode(
            successStatusCode,
            ApiResponse<MembershipResponse>.Success(
                MapMembership(result.Value!),
                result.Message,
                successStatusCode));
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