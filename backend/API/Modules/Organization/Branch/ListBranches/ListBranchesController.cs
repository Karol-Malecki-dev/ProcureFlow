using Application.Modules.Organization.Branch.ListBranches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Branch.ListBranches;

[ApiController]
[Route("api/organizations/{organizationId:guid}/branches")]
[Authorize(Roles = "Admin")]
public sealed class ListBranchesController : ControllerBase
{
    private readonly IListBranchesHandler _handler;

    public ListBranchesController(IListBranchesHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches(
        Guid organizationId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new ListBranchesQuery(organizationId, includeArchived),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(
                ApiResponse<IReadOnlyList<BranchListItemResponse>>.Error(
                    404,
                    result.Message));
        }

        var response = result.Value!
            .Select(MapBranch)
            .ToList();

        return Ok(
            ApiResponse<IReadOnlyList<BranchListItemResponse>>.Success(
                response));
    }
    private static BranchListItemResponse MapBranch(BranchListItem branch)
        => new(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.IsArchived,
            new BranchListAddressResponse(
                branch.Address.Street,
                branch.Address.BuildingNumber,
                branch.Address.ApartmentNumber,
                branch.Address.City,
                branch.Address.PostalCode,
                branch.Address.Country));
}
