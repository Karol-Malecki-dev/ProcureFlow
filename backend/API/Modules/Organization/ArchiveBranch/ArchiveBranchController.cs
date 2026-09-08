using Application.Modules.Organization.ArchiveBranch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;

namespace API.Modules.Organization.ArchiveBranch;

[ApiController]
[Route("api/organizations/{organizationId:guid}/branches")]
[Authorize(Roles = "Admin")]
public sealed class ArchiveBranchController : ControllerBase
{
    private readonly IArchiveBranchHandler _handler;

    public ArchiveBranchController(IArchiveBranchHandler handler)
    {
        _handler = handler;
    }

    [HttpPost("{branchId:guid}/archive")]
    public async Task<IActionResult> ArchiveBranch(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var result = await _handler.HandleAsync(
            new ArchiveBranchCommand(organizationId, branchId),
            cancellationToken);

        if (!result.IsSuccess)
        {
            var statusCode = result.Status switch
            {
                ArchiveBranchStatus.NotFound => 404,
                ArchiveBranchStatus.Conflict => 409,
                _ => 500
            };

            return StatusCode(
                statusCode,
                ApiResponse<ArchiveBranchResponse>.Error(statusCode, result.Message));
        }

        return Ok(
            ApiResponse<ArchiveBranchResponse>.Success(
                new ArchiveBranchResponse(result.Value == true),
                result.Message));
    }
}
