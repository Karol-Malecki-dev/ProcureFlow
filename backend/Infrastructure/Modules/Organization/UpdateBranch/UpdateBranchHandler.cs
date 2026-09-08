using Application.Modules.Organization.UpdateBranch;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.UpdateBranch;

public sealed class UpdateBranchHandler : IUpdateBranchHandler
{
    private readonly IUpdateBranchStore _store;

    public UpdateBranchHandler(IUpdateBranchStore store)
    {
        _store = store;
    }

    public async Task<UpdateBranchResult> HandleAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        var branch = await _store.GetOwnedBranchAsync(
            command.OrganizationId,
            command.BranchId,
            cancellationToken);

        if (branch is null)
        {
            return UpdateBranchResult.Failure(
                UpdateBranchStatus.NotFound,
                "Branch not found.");
        }

        if (branch.IsArchived)
        {
            return UpdateBranchResult.Failure(
                UpdateBranchStatus.Conflict,
                "Archived branch cannot be updated.");
        }

        if (await _store.CodeExistsAsync(
                command.OrganizationId,
                command.BranchId,
                command.Code,
                cancellationToken))
        {
            return UpdateBranchResult.Failure(
                UpdateBranchStatus.Conflict,
                $"Branch code '{command.Code}' already exists for organization '{command.OrganizationId}'.");
        }

        if (await _store.NameExistsAsync(
                command.OrganizationId,
                command.BranchId,
                command.Name,
                cancellationToken))
        {
            return UpdateBranchResult.Failure(
                UpdateBranchStatus.Conflict,
                $"Branch name '{command.Name}' already exists for organization '{command.OrganizationId}'.");
        }

        try
        {
            branch.UpdateDetails(command.Name, command.Address, command.Code);
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                exception,
                "IX_Branches_OrganizationId_Code")
            || PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                exception,
                "IX_Branches_OrganizationId_Name"))
        {
            return UpdateBranchResult.Failure(
                UpdateBranchStatus.Conflict,
                "A branch with the same name or code already exists in this organization.");
        }

        return UpdateBranchResult.Success(
            new UpdatedBranch(
                branch.Id,
                branch.Name,
                branch.Code,
                branch.IsArchived,
                branch.Address));
    }
}
