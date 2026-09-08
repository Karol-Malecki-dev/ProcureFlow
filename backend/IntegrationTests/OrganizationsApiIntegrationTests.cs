using Application.DTOs.Auth;
using API.Modules.Organization.ArchiveBranch;
using API.Modules.Organization.CreateBranch;
using API.Modules.Organization.GetBranchDetails;
using API.Modules.Organization.GetActiveOrganization;
using API.Modules.Organization.ListBranches;
using API.Modules.Organization.UpdateBranch;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainBranch = Domain.Models.Organizations.Organization.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization.Organization;

namespace IntegrationTests;

[Collection(nameof(PostgreSqlIntegrationTestCollection))]
public sealed class OrganizationsApiIntegrationTests
{
    private readonly PostgreSqlWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganizationsApiIntegrationTests(PostgreSqlWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Branch_list_returns_unauthorized_without_token()
    {
        var response = await _client.GetAsync($"/api/organizations/{Guid.NewGuid()}/branches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Branch_list_returns_forbidden_for_non_admin_user()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var email = UniqueEmail("branch-user");
        await SeedUserAsync(email, UserRole.User);
        await AuthenticateAsync(email);

        var response = await _client.GetAsync($"/api/organizations/{organizationId}/branches");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_discover_the_active_organization()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var email = UniqueEmail("active-organization");
        await SeedUserAsync(email, UserRole.Admin);
        await AuthenticateAsync(email);

        var response = await _client.GetAsync("/api/organizations/active");

        response.EnsureSuccessStatusCode();
        var activeOrganization = await response.Content
            .ReadFromJsonAsync<ApiResponse<GetActiveOrganizationResponse>>();

        Assert.Equal(organizationId, activeOrganization?.Data?.Id);
        Assert.False(string.IsNullOrWhiteSpace(activeOrganization?.Data?.Name));
        Assert.False(string.IsNullOrWhiteSpace(activeOrganization?.Data?.Code));
    }

    [Fact]
    public async Task Admin_can_create_read_update_archive_and_filter_a_branch()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var email = UniqueEmail("branch-admin");
        await SeedUserAsync(email, UserRole.Admin);
        await AuthenticateAsync(email);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/branches",
            CreateBranchPayload($"Warsaw {suffix}", $"WAW{suffix}"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<BranchResponse>>();
        Assert.NotNull(created?.Data);
        var branchId = created.Data.Id;

        var detailsResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/branches/{branchId}");
        detailsResponse.EnsureSuccessStatusCode();
        var details = await detailsResponse.Content
            .ReadFromJsonAsync<ApiResponse<GetBranchDetailsResponse>>();
        Assert.Equal(branchId, details?.Data?.Id);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/organizations/{organizationId}/branches/{branchId}",
            new
            {
                Name = $"Updated Warsaw {suffix}",
                Code = $"UPD{suffix}",
                Address = CreateAddressPayload("Updated Street")
            });

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<ApiResponse<UpdateBranchResponse>>();
        Assert.Equal($"UPD{suffix}", updated?.Data?.Code);

        var archiveResponse = await _client.PostAsync(
            $"/api/organizations/{organizationId}/branches/{branchId}/archive",
            content: null);
        archiveResponse.EnsureSuccessStatusCode();
        var archived = await archiveResponse.Content
            .ReadFromJsonAsync<ApiResponse<ArchiveBranchResponse>>();
        Assert.True(archived?.Data?.IsArchived);

        var activeBranchesResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/branches");
        activeBranchesResponse.EnsureSuccessStatusCode();
        var activeBranches = await activeBranchesResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<BranchListItemResponse>>>();
        Assert.DoesNotContain(activeBranches?.Data ?? [], branch => branch.Id == branchId);

        var allBranchesResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/branches?includeArchived=true");
        allBranchesResponse.EnsureSuccessStatusCode();
        var allBranches = await allBranchesResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<BranchListItemResponse>>>();
        Assert.Contains(allBranches?.Data ?? [], branch =>
            branch.Id == branchId && branch.IsArchived);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedBranch = await dbContext.Branches.SingleAsync(branch => branch.Id == branchId);
        Assert.True(persistedBranch.IsArchived);
    }

    [Fact]
    public async Task Branch_create_rejects_duplicate_code_and_name()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var email = UniqueEmail("branch-duplicates");
        await SeedUserAsync(email, UserRole.Admin);
        await AuthenticateAsync(email);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"Duplicate branch {suffix}";
        var code = $"DUP{suffix}";

        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/branches",
            CreateBranchPayload(name, code));
        firstResponse.EnsureSuccessStatusCode();

        var duplicateCodeResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/branches",
            CreateBranchPayload($"Different branch {suffix}", code));
        Assert.Equal(HttpStatusCode.Conflict, duplicateCodeResponse.StatusCode);

        var duplicateNameResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/branches",
            CreateBranchPayload(name, $"OTHER{suffix}"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateNameResponse.StatusCode);
    }

    [Fact]
    public async Task Branch_operations_return_not_found_for_wrong_organization_scope()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var email = UniqueEmail("branch-scope");
        await SeedUserAsync(email, UserRole.Admin);
        await AuthenticateAsync(email);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/branches",
            CreateBranchPayload($"Scoped branch {suffix}", $"SCP{suffix}"));
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<BranchResponse>>();
        Assert.NotNull(created?.Data);

        var otherOrganizationId = Guid.NewGuid();
        var detailsResponse = await _client.GetAsync(
            $"/api/organizations/{otherOrganizationId}/branches/{created.Data.Id}");
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/organizations/{otherOrganizationId}/branches/{created.Data.Id}",
            new
            {
                Name = $"Wrong scope {suffix}",
                Code = $"WRG{suffix}",
                Address = CreateAddressPayload()
            });
        var archiveResponse = await _client.PostAsync(
            $"/api/organizations/{otherOrganizationId}/branches/{created.Data.Id}/archive",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, detailsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, archiveResponse.StatusCode);
    }

    [Fact]
    public async Task PostgreSql_enforces_branch_name_active_organization_and_foreign_key_constraints()
    {
        var organizationId = await SeedActiveOrganizationAsync();

        await using (var duplicateNameScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = duplicateNameScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var firstBranch = new DomainBranch(
                "Constraint branch",
                CreateAddress(),
                $"CST{Guid.NewGuid():N}"[..11],
                organizationId);
            var duplicateName = new DomainBranch(
                "Constraint branch",
                CreateAddress(),
                $"CST{Guid.NewGuid():N}"[..11],
                organizationId);
            dbContext.Branches.AddRange(firstBranch, duplicateName);

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }

        await using (var duplicateOrganizationScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = duplicateOrganizationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Organizations.Add(new DomainOrganization(
                "Second active organization",
                CreateAddress(),
                $"SECOND{Guid.NewGuid():N}"[..12],
                null));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }

        await using (var deleteScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = deleteScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var organization = await dbContext.Organizations
                .SingleAsync(candidate => candidate.Id == organizationId);
            dbContext.Organizations.Remove(organization);

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
    }

    private async Task AuthenticateAsync(string email)
    {
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = email, Password = "password123" });
        loginResponse.EnsureSuccessStatusCode();
        var apiResponse = await loginResponse.Content
            .ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();
        Assert.NotNull(apiResponse?.Data);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiResponse.Data.AccessToken);
    }

    private async Task<Guid> SeedUserAsync(string email, UserRole role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher<User>();
        var user = User.Create(
            EmailAddress.Create(email),
            DisplayName.Create("Branch test user"),
            role,
            isActive: true,
            isEmailConfirmed: true);
        user.SetPasswordHash(passwordHasher.HashPassword(user, "password123"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedActiveOrganizationAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existingOrganization = await dbContext.Organizations
            .SingleOrDefaultAsync(organization => !organization.IsArchived);
        if (existingOrganization is not null)
        {
            return existingOrganization.Id;
        }

        var organization = new DomainOrganization(
            "ProcureFlow MVP",
            CreateAddress(),
            $"PF{Guid.NewGuid():N}"[..10],
            "Organization used by PF1 integration tests.");
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private static object CreateBranchPayload(string name, string code)
        => new
        {
            Name = name,
            Code = code,
            Address = CreateAddressPayload()
        };

    private static object CreateAddressPayload(string street = "Main Street")
        => new
        {
            Street = street,
            BuildingNumber = "1",
            ApartmentNumber = (string?)null,
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };

    private static Address CreateAddress()
        => new()
        {
            Street = "Main Street",
            BuildingNumber = "1",
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };

    private static string UniqueEmail(string prefix)
        => $"{prefix}.{Guid.NewGuid():N}@example.com";
}
