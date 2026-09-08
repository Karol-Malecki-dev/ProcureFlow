using Application.Modules.Organization.CreateBranch;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.CreateBranch;

[ApiController]
[Route("api/organizations/{organizationId:guid}/branches")]
[Authorize(Roles = "Admin")]
public sealed class CreateBranchController : ControllerBase
{
    private readonly ICreateBranchHandler _handler;

    public CreateBranchController(ICreateBranchHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBranch(
        Guid organizationId,
        CreateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var address = new Address
        {
            Street = request.Address.Street,
            BuildingNumber = request.Address.BuildingNumber,
            ApartmentNumber = request.Address.ApartmentNumber,
            City = request.Address.City,
            PostalCode = request.Address.PostalCode,
            Country = request.Address.Country
        };

        var result = await _handler.HandleAsync(
            new CreateBranchCommand(
                organizationId,
                request.Name,
                request.Code,
                address),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = MapStatusCode(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<BranchResponse>.Error(statusCode, result.Message));
        }

        var response = MapBranch(result.Value!);

        return StatusCode(
            result.CreatedStatusCode,
            ApiResponse<BranchResponse>.Success(
                response,
                result.Message,
                result.CreatedStatusCode));
    }

    private static BranchResponse MapBranch(BranchView branch)
        => new(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.IsArchived,
            new BranchAddressResponse(
                branch.Address.Street,
                branch.Address.BuildingNumber,
                branch.Address.ApartmentNumber,
                branch.Address.City,
                branch.Address.PostalCode,
                branch.Address.Country));

    private static int MapStatusCode(BranchOperationStatus status) => status switch
    {
        BranchOperationStatus.NotFound => 404,
        BranchOperationStatus.Conflict => 409,
        BranchOperationStatus.ValidationError => 400,
        BranchOperationStatus.Forbidden => 403,
        _ => 500
    };
}
