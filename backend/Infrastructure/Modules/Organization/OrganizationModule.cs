using Infrastructure.Modules.Organization.GetActiveOrganization;
using Microsoft.Extensions.DependencyInjection;
using Application.Modules.Organization.Branch.CreateBranch;
using Application.Modules.Organization.Branch.GetBranchDetails;
using Application.Modules.Organization.Branch.ListBranches;
using Application.Modules.Organization.Branch.UpdateBranch;
using Application.Modules.Organization.Membership.Archive;
using Application.Modules.Organization.Membership.Create;
using Application.Modules.Organization.Membership.Get;
using Application.Modules.Organization.Membership.GetList;
using Application.Modules.Organization.Membership.Update;
using Infrastructure.Modules.Organization.Branch.UpdateBranch;
using Infrastructure.Modules.Organization.Branch.GetBranchDetails;
using Infrastructure.Modules.Organization.Branch.ListBranches;
using Infrastructure.Modules.Organization.Branch.CreateBranch;
using Infrastructure.Modules.Organization.Branch.ArchiveBranch;
using Infrastructure.Modules.Organization.Membership.Archive;
using Infrastructure.Modules.Organization.Membership.Create;
using Infrastructure.Modules.Organization.Membership.Get;
using Infrastructure.Modules.Organization.Membership.GetList;
using Infrastructure.Modules.Organization.Membership.Update;
using Application.Modules.Organization.GetActiveOrganization;
using Application.Modules.Organization.Branch.ArchiveBranch;

namespace Infrastructure.Modules.Organization;

public static class OrganizationModule
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services)
    {
        // Organization use cases
        services.AddScoped<IGetActiveOrganizationStore, EfGetActiveOrganizationStore>();
        services.AddScoped<IGetActiveOrganizationHandler, GetActiveOrganizationHandler>();

        // Branch use cases
        services.AddScoped<ICreateBranchStore, EfCreateBranchStore>();
        services.AddScoped<ICreateBranchHandler, CreateBranchHandler>();

        services.AddScoped<IListBranchesStore, EfListBranchesStore>();
        services.AddScoped<IListBranchesHandler, ListBranchesHandler>();

        services.AddScoped<IGetBranchDetailsStore, EfGetBranchDetailsStore>();
        services.AddScoped<IGetBranchDetailsHandler, GetBranchDetailsHandler>();

        services.AddScoped<IUpdateBranchStore, EfUpdateBranchStore>();
        services.AddScoped<IUpdateBranchHandler, UpdateBranchHandler>();

        services.AddScoped<IArchiveBranchStore, EfArchiveBranchStore>();
        services.AddScoped<IArchiveBranchHandler, ArchiveBranchHandler>();

        // Membership use cases
        services.AddScoped<ICreateMembershipStore, EfCreateMembershipStore>();
        services.AddScoped<ICreateMembershipHandler, CreateMembershipHandler>();

        services.AddScoped<IGetMembershipDetailsStore, EfGetMembershipDetailsStore>();
        services.AddScoped<IGetMembershipDetailsHandler, GetMembershipDetailsHandler>();

        services.AddScoped<IListMembershipStore, EfListMembershipStore>();
        services.AddScoped<IListMembershipHandler, GetListMembershipHandler>();

        services.AddScoped<IUpdateMembershipStore, EfUpdateMembershipStore>();
        services.AddScoped<IUpdateMembershipHandler, UpdateMembershipHandler>();

        services.AddScoped<IArchiveMembershipStore, EfArchiveMembershipStore>();
        services.AddScoped<IArchiveMembershipHandler, ArchiveMembershipHandler>();

        return services;
    }
}