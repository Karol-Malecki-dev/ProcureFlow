using Application.Modules.Organization.UpdateBranch;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.UpdateBranch;

[ApiController]
[Route("api/organizations/{organizationId:guid}/branches")]
[Authorize(Roles = "Admin")]
public sealed class UpdateBranchController : ControllerBase
{
    private readonly IUpdateBranchHandler _handler;

    public UpdateBranchController(IUpdateBranchHandler handler)
    {
        _handler = handler;
    }

    [HttpPut("{branchId:guid}")]
    public async Task<IActionResult> UpdateBranch(
        Guid organizationId,
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new UpdateBranchCommand(
                organizationId,
                branchId,
                request.Name,
                request.Code,
                new Address
                {
                    Street = request.Address.Street,
                    BuildingNumber = request.Address.BuildingNumber,
                    ApartmentNumber = request.Address.ApartmentNumber,
                    City = request.Address.City,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.Country
                }),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = MapStatusCode(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<UpdateBranchResponse>.Error(statusCode, result.Message));
        }

        return Ok(
            ApiResponse<UpdateBranchResponse>.Success(
                MapBranch(result.Value!),
                result.Message));
    }

    private static UpdateBranchResponse MapBranch(UpdatedBranch branch)
        => new(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.IsArchived,
            new UpdateBranchAddressResponse(
                branch.Address.Street,
                branch.Address.BuildingNumber,
                branch.Address.ApartmentNumber,
                branch.Address.City,
                branch.Address.PostalCode,
                branch.Address.Country));

    private static int MapStatusCode(UpdateBranchStatus status) => status switch
    {
        UpdateBranchStatus.NotFound => 404,
        UpdateBranchStatus.Conflict => 409,
        UpdateBranchStatus.ValidationError => 400,
        _ => 500
    };
}
