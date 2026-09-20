using Application.Modules.Organization.Membership.Archive;
using Domain.Models.Organizations.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Membership.Archive;


[ApiController]
[Route("api/organizations/{organizationId:guid}/memberships")]
[Authorize(Roles = "Admin")]
public sealed class ArchiveMembershipController : ControllerBase
{
    private readonly IArchiveMembershipHandler _handler;

    public ArchiveMembershipController(IArchiveMembershipHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{membershipId:guid}/archive")]
    public async Task<IActionResult> ArchiveMembership(
        Guid organizationId, 
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new ArchiveMembershipCommand(organizationId, membershipId),
            cancellationToken);
    
        if(!result.IsSuccess)
        {
            var statusCode = result.Status switch
            {
                MembershipOperationStatus.ValidationError => 400,
                MembershipOperationStatus.NotFound => 404,
                MembershipOperationStatus.Conflict => 409,
                _ => 500
            };
            return StatusCode(
                statusCode,
                ApiResponse<ArchiveMembershipResponse>.Error(statusCode,result.Message));
        }

        return Ok(
            ApiResponse<ArchiveMembershipResponse>.Success(
                new ArchiveMembershipResponse(result.Value == true),
                result.Message));

    }


}
