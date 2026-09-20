using API.Modules.Organization.Membership;
using API.Modules.Organization.Membership.Current;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Enums;
using Domain.Models.Organizations.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace IntegrationTests;

[Collection(nameof(PostgreSqlIntegrationTestCollection))]
public sealed class MembershipsApiIntegrationTests
{
    private readonly PostgreSqlWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MembershipsApiIntegrationTests(PostgreSqlWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Membership_create_returns_unauthorized_without_token()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{Guid.NewGuid()}/memberships",
            new
            {
                UserId = Guid.NewGuid(),
                BranchId = (Guid?)null,
                Role = (int)BusinessRole.Procurement
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Membership_create_returns_forbidden_for_non_admin_user()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var callerEmail = UniqueEmail("membership-user");
        await SeedUserAsync(callerEmail, UserRole.User);
        await AuthenticateAsync(callerEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = Guid.NewGuid(),
                BranchId = (Guid?)null,
                Role = (int)BusinessRole.Procurement
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_assign_manager_membership_to_active_branch()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var targetEmail = UniqueEmail("membership-target");
        var targetUserId = await SeedUserAsync(targetEmail, UserRole.User);
        var adminEmail = UniqueEmail("membership-admin");
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = branchId,
                Role = (int)BusinessRole.Manager
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<MembershipResponse>>();

        Assert.NotNull(payload?.Data);
        Assert.Equal(organizationId, payload.Data.OrganizationId);
        Assert.Equal(targetUserId, payload.Data.UserId);
        Assert.Equal(branchId, payload.Data.BranchId);
        Assert.Equal(BusinessRole.Manager, payload.Data.Role);
        Assert.True(payload.Data.IsActive);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await dbContext.Memberships.AnyAsync(membership =>
            membership.Id == payload.Data.Id
            && membership.OrganizationId == organizationId
            && membership.UserId == targetUserId
            && membership.BranchId == branchId
            && membership.IsActive));
    }

    [Fact]
    public async Task Membership_create_rejects_archived_branch()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        await ArchiveBranchAsync(branchId);
        var targetUserId = await SeedUserAsync(UniqueEmail("archived-branch-target"), UserRole.User);
        await AuthenticateAsNewAdminAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = branchId,
                Role = (int)BusinessRole.Employee
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Membership_create_rejects_branch_from_another_organization_scope()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var foreignBranchId = await SeedArchivedOrganizationBranchAsync();
        var targetUserId = await SeedUserAsync(UniqueEmail("scope-target"), UserRole.User);
        await AuthenticateAsNewAdminAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = foreignBranchId,
                Role = (int)BusinessRole.Manager
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Membership_create_returns_conflict_for_existing_active_membership()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var targetUserId = await SeedUserAsync(UniqueEmail("duplicate-target"), UserRole.User);
        await AuthenticateAsNewAdminAsync();
        var payload = new
        {
            UserId = targetUserId,
            BranchId = (Guid?)null,
            Role = (int)BusinessRole.Procurement
        };

        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            payload);
        var secondResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            payload);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Membership_create_allows_only_one_concurrent_active_assignment()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var targetUserId = await SeedUserAsync(UniqueEmail("concurrent-target"), UserRole.User);
        await AuthenticateAsNewAdminAsync();
        var payload = new
        {
            UserId = targetUserId,
            BranchId = (Guid?)null,
            Role = (int)BusinessRole.Procurement
        };

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync($"/api/organizations/{organizationId}/memberships", payload),
            _client.PostAsJsonAsync($"/api/organizations/{organizationId}/memberships", payload));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Current_membership_returns_active_database_state_for_authenticated_user()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var targetEmail = UniqueEmail("current-membership");
        var targetUserId = await SeedUserAsync(targetEmail, UserRole.User);
        await AuthenticateAsNewAdminAsync();

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = (Guid?)null,
                Role = (int)BusinessRole.Procurement
            });
        createResponse.EnsureSuccessStatusCode();

        await AuthenticateAsync(targetEmail);

        var currentResponse = await _client.GetAsync("/api/memberships/current");

        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        var current = await currentResponse.Content
            .ReadFromJsonAsync<ApiResponse<CurrentMembershipResponse>>();

        Assert.NotNull(current?.Data);
        Assert.Equal(organizationId, current.Data.OrganizationId);
        Assert.Equal(targetUserId, current.Data.UserId);
        Assert.Equal(BusinessRole.Procurement, current.Data.Role);
        Assert.True(current.Data.IsActive);
    }

    [Fact]
    public async Task PostgreSql_allows_inactive_history_but_rejects_second_active_membership()
    {
        var organizationId = await SeedActiveOrganizationAsync();
        var userId = await SeedUserAsync(UniqueEmail("membership-constraint"), UserRole.User);

        Guid firstMembershipId;
        await using (var firstScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var firstMembership = new DomainMembership(
                organizationId,
                userId,
                branchId: null,
                BusinessRole.Procurement);
            firstMembershipId = firstMembership.Id;
            dbContext.Memberships.Add(firstMembership);
            await dbContext.SaveChangesAsync();
        }

        await using (var duplicateScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = duplicateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Memberships.Add(new DomainMembership(
                organizationId,
                userId,
                branchId: null,
                BusinessRole.Procurement));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }

        await using (var historyScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = historyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var inactiveMembership = new DomainMembership(
                organizationId,
                userId,
                branchId: null,
                BusinessRole.Procurement);
            inactiveMembership.Deactivate();
            dbContext.Memberships.Add(inactiveMembership);
            await dbContext.SaveChangesAsync();

            Assert.Equal(2, await dbContext.Memberships.CountAsync(membership =>
                membership.UserId == userId));
            Assert.True(await dbContext.Memberships.AnyAsync(membership =>
                membership.Id == firstMembershipId && membership.IsActive));
        }
    }

    private async Task AuthenticateAsNewAdminAsync()
    {
        var email = UniqueEmail("membership-admin");
        await SeedUserAsync(email, UserRole.Admin);
        await AuthenticateAsync(email);
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
            DisplayName.Create("Membership test user"),
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

    private async Task<Guid> SeedBranchAsync(Guid organizationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var branch = new DomainBranch(
            $"Membership test branch {suffix}",
            CreateAddress(),
            $"BR{suffix}",
            organizationId);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        return branch.Id;
    }

    private async Task ArchiveBranchAsync(Guid branchId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var branch = await dbContext.Branches.SingleAsync(candidate => candidate.Id == branchId);
        branch.Archive();
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedArchivedOrganizationBranchAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organization = new DomainOrganization(
            "Archived membership organization",
            CreateAddress(),
            $"ARCH{Guid.NewGuid():N}"[..12],
            null);
        organization.Archive();
        var branch = new DomainBranch(
            "Foreign membership branch",
            CreateAddress(),
            $"FBR{Guid.NewGuid():N}"[..11],
            organization.Id);
        dbContext.Organizations.Add(organization);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        return branch.Id;
    }

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