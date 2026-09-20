using API.Contracts.Projects;
using Application.Features.Projects;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Modules.Projects;

/// <summary>
/// Provides shared HTTP mapping for project controllers during the incremental migration.
/// </summary>
public abstract class ProjectControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<TValue, TResponse>(
        ProjectOperationResult<TValue> result,
        Func<TValue, TResponse> map)
    {
        if (!result.IsSuccess)
        {
            var statusCode = (int)result.StatusCode;
            return StatusCode(statusCode, ApiResponse<TResponse>.Error(statusCode, result.Message));
        }

        var successStatusCode = (int)result.StatusCode;
        return StatusCode(
            successStatusCode,
            ApiResponse<TResponse>.Success(map(result.Value!), result.Message, successStatusCode));
    }

    protected static ProjectResponse MapProject(ProjectView project) => new(
        project.Id,
        project.Name,
        project.Description,
        project.OwnerId,
        project.CreatedAt,
        project.UpdatedAt,
        project.ConcurrencyStamp,
        project.IsArchived,
        project.CurrentUserRole);

    protected static ProjectMemberResponse MapMember(ProjectMemberView member) => new(
        member.UserId,
        member.DisplayName,
        member.Email,
        member.Role,
        member.AddedAt);

    protected static ProjectMemberUserResponse MapMemberUser(ProjectMemberUserView user) => new(
        user.Id,
        user.DisplayName,
        user.Email);

    protected static ProjectInvitationResponse MapInvitation(ProjectInvitationView invitation) => new(
        invitation.Id,
        invitation.ProjectId,
        invitation.ProjectName,
        invitation.InvitedUserId,
        invitation.InvitedUserDisplayName,
        invitation.InvitedUserEmail,
        invitation.InvitedByDisplayName,
        invitation.Role,
        invitation.Status,
        invitation.ExpiresAt,
        invitation.CreatedAt);

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out userId);
    }

}
