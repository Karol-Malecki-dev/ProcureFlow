using Application.Modules.Organization.GetBranchDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.GetBranchDetails;

[ApiController]
[Route("api/organizations/{organizationId:guid}/branches")]
[Authorize(Roles = "Admin")]
public sealed class GetBranchDetailsController : ControllerBase
{
    private readonly IGetBranchDetailsHandler _handler;

    public GetBranchDetailsController(IGetBranchDetailsHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{branchId:guid}")]
    public async Task<IActionResult> GetBranchDetails(
        Guid organizationId,
        Guid branchId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new GetBranchDetailsQuery(organizationId, branchId, includeArchived),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(
                ApiResponse<GetBranchDetailsResponse>.Error(
                    404,
                    result.Message));
        }

        return Ok(
            ApiResponse<GetBranchDetailsResponse>.Success(
                MapBranch(result.Value!)));
    }

    private static GetBranchDetailsResponse MapBranch(BranchDetails branch)
        => new(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.IsArchived,
            new GetBranchDetailsAddressResponse(
                branch.Address.Street,
                branch.Address.BuildingNumber,
                branch.Address.ApartmentNumber,
                branch.Address.City,
                branch.Address.PostalCode,
                branch.Address.Country));
}
