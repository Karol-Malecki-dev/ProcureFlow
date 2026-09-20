using Application.Modules.Organization.GetActiveOrganization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Branch.GetActiveOrganization;

[ApiController]
[Route("api/organizations/active")]
[Authorize(Roles = "Admin")]
public sealed class GetActiveOrganizationController : ControllerBase
{
    private readonly IGetActiveOrganizationHandler _handler;

    public GetActiveOrganizationController(IGetActiveOrganizationHandler handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Returns the active organization used by the single-organization MVP.
    /// </summary>
    /// <response code="200">The active organization was found.</response>
    /// <response code="401">The request is not authenticated.</response>
    /// <response code="403">The authenticated user is not a platform administrator.</response>
    /// <response code="404">No active organization has been initialized.</response>
    [HttpGet]
    public async Task<IActionResult> GetActiveOrganization(
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new GetActiveOrganizationQuery(),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(
                ApiResponse<GetActiveOrganizationResponse>.Error(
                    404,
                    result.Message));
        }

        var organization = result.Value!;
        return Ok(
            ApiResponse<GetActiveOrganizationResponse>.Success(
                new GetActiveOrganizationResponse(
                    organization.Id,
                    organization.Name,
                    organization.Code)));
    }
}
