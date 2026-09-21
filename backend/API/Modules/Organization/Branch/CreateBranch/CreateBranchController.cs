using Application.Modules.Organization.Branch.CreateBranch;
using Domain.ValueObjects;
using API.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.Branch.CreateBranch;

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
            var statusCode = OperationResultStatusCodeMapper.Map(result.Status);
            return StatusCode(
                statusCode,
                ApiResponse<BranchResponse>.Error(statusCode, result.Message));
        }

        var response = MapBranch(result.Value!);
        const int successStatusCode = StatusCodes.Status201Created;

        return StatusCode(
            successStatusCode,
            ApiResponse<BranchResponse>.Success(
                response,
                result.Message,
                successStatusCode));
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

}
