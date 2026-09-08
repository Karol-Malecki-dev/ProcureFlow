using Application.Modules.Organization.CreateBranch;
using Application.Modules.Organization.ArchiveBranch;
using Application.Modules.Organization.GetBranchDetails;
using Application.Modules.Organization.GetActiveOrganization;
using Application.Modules.Organization.ListBranches;
using Application.Modules.Organization.UpdateBranch;
using Infrastructure.Modules.Organization.ArchiveBranch;
using Infrastructure.Modules.Organization.CreateBranch;
using Infrastructure.Modules.Organization.GetBranchDetails;
using Infrastructure.Modules.Organization.GetActiveOrganization;
using Infrastructure.Modules.Organization.ListBranches;
using Infrastructure.Modules.Organization.UpdateBranch;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Modules.Organization;

public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services)
    {
        services.AddScoped<IArchiveBranchStore, EfArchiveBranchStore>();
        services.AddScoped<IArchiveBranchHandler, ArchiveBranchHandler>();
        services.AddScoped<ICreateBranchStore, EfCreateBranchStore>();
        services.AddScoped<ICreateBranchHandler, CreateBranchHandler>();
        services.AddScoped<IListBranchesStore, EfListBranchesStore>();
        services.AddScoped<IListBranchesHandler, ListBranchesHandler>();
        services.AddScoped<IGetBranchDetailsStore, EfGetBranchDetailsStore>();
        services.AddScoped<IGetBranchDetailsHandler, GetBranchDetailsHandler>();
        services.AddScoped<IGetActiveOrganizationStore, EfGetActiveOrganizationStore>();
        services.AddScoped<IGetActiveOrganizationHandler, GetActiveOrganizationHandler>();
        services.AddScoped<IUpdateBranchStore, EfUpdateBranchStore>();
        services.AddScoped<IUpdateBranchHandler, UpdateBranchHandler>();

        return services;
    }
}