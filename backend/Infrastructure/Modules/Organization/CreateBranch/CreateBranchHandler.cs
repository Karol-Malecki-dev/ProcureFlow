using Application.Modules.Organization.CreateBranch;
using Domain.Models.Organizations.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.CreateBranch;

public sealed class CreateBranchHandler : ICreateBranchHandler
{
    private readonly ICreateBranchStore _store;

    public CreateBranchHandler(ICreateBranchStore store)
    {
        _store = store;
    }

    public async Task<BranchOperationResult<BranchView>> HandleAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        var organization = await _store.GetOrganizationAsync(
            command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return BranchOperationResult<BranchView>.Failure(
                BranchOperationStatus.NotFound,
                $"Organization '{command.OrganizationId}' does not exist.");
        }

        if (organization.IsArchived)
        {
            return BranchOperationResult<BranchView>.Failure(
                BranchOperationStatus.Conflict,
                "Archived organization cannot accept new branches.");
        }

        var codeExists = await _store.CodeExistsAsync(
            command.OrganizationId,
            command.Code,
            cancellationToken);

        if (codeExists)
        {
            return BranchOperationResult<BranchView>.Failure(
                BranchOperationStatus.Conflict,
                $"Branch code '{command.Code}' already exists for organization '{command.OrganizationId}'.");
        }

        var nameExists = await _store.NameExistsAsync(
            command.OrganizationId,
            command.Name,
            cancellationToken);

        if (nameExists)
        {
            return BranchOperationResult<BranchView>.Failure(
                BranchOperationStatus.Conflict,
                $"Branch name '{command.Name}' already exists for organization '{command.OrganizationId}'.");
        }

        var branch = new Branch(
            name: command.Name,
            address: command.Address,
            code: command.Code,
            organizationId: command.OrganizationId);

        organization.AddBranch(branch);
        _store.Add(branch);

        try
        {
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
            return BranchOperationResult<BranchView>.Failure(
                BranchOperationStatus.Conflict,
                "A branch with the same name or code already exists in this organization.");
        }

        var branchView = new BranchView(
            branch.Id,
            branch.Name,
            branch.Code,
            branch.IsArchived,
            branch.Address);

        return BranchOperationResult<BranchView>.Success(
            branchView,
            "Branch created",
            201);
    }
}
