using API.Contracts.Projects;
using Application.Features.ProjectManagement.Tasks;
using Application.Features.Projects;
using Application.Modules.ProjectTasks.Attachments;
using Application.Modules.ProjectTasks.Comments;
using Microsoft.AspNetCore.Mvc;
using Shared.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Modules.ProjectTasks;

/// <summary>
/// Provides shared HTTP mapping for ProjectTasks controllers while endpoints remain slice-specific.
/// </summary>
public abstract class ProjectTaskControllerBase : ControllerBase
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

    protected static ProjectTaskResponse MapTask(ProjectTaskView task) => new(
        task.Id,
        task.ProjectId,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.DueDate,
        task.AssignedUserId,
        task.CreatedByUserId,
        task.CreatedAt,
        task.UpdatedAt,
        task.ConcurrencyStamp,
        task.Labels);

    protected static ProjectTaskCommentResponse MapComment(ProjectTaskCommentView comment) => new(
        comment.Id,
        comment.ProjectTaskId,
        comment.AuthorUserId,
        comment.AuthorDisplayName,
        comment.Content,
        comment.CreatedAt);

    protected static ProjectTaskAttachmentResponse MapAttachment(ProjectTaskAttachmentView attachment) => new(
        attachment.Id,
        attachment.ProjectTaskId,
        attachment.UploadedByUserId,
        attachment.UploaderDisplayName,
        attachment.OriginalFileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.CreatedAt);

    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out userId);
    }
}
