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
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace IntegrationTests;

public sealed class MembershipsApiInMemoryIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public MembershipsApiInMemoryIntegrationTests()
    {
        _client = _factory.CreateClient();
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
        var organizationId = await SeedOrganizationAsync();
        var email = UniqueEmail("inmemory-user");
        await SeedUserAsync(email, UserRole.User);
        await AuthenticateAsync(email);

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
    public async Task Admin_can_create_membership_and_target_user_can_read_current_context()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var targetEmail = UniqueEmail("inmemory-target");
        var targetUserId = await SeedUserAsync(targetEmail, UserRole.User);
        var adminEmail = UniqueEmail("inmemory-admin");
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = branchId,
                Role = (int)BusinessRole.Manager
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<MembershipResponse>>();
        Assert.NotNull(created?.Data);
        Assert.Equal(targetUserId, created.Data.UserId);
        Assert.Equal(branchId, created.Data.BranchId);

        await AuthenticateAsync(targetEmail);

        var currentResponse = await _client.GetAsync("/api/memberships/current");

        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        var current = await currentResponse.Content
            .ReadFromJsonAsync<ApiResponse<CurrentMembershipResponse>>();
        Assert.NotNull(current?.Data);
        Assert.Equal(organizationId, current.Data.OrganizationId);
        Assert.Equal(targetUserId, current.Data.UserId);
        Assert.Equal(BusinessRole.Manager, current.Data.Role);
    }

    [Fact]
    public async Task Membership_create_hides_a_branch_from_another_organization()
    {
        var organizationId = await SeedOrganizationAsync();
        var foreignBranchId = await SeedArchivedOrganizationBranchAsync();
        var targetUserId = await SeedUserAsync(UniqueEmail("inmemory-scope-target"), UserRole.User);
        var adminEmail = UniqueEmail("inmemory-scope-admin");
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/memberships",
            new
            {
                UserId = targetUserId,
                BranchId = foreignBranchId,
                Role = (int)BusinessRole.Employee
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();
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
            DisplayName.Create("Membership InMemory user"),
            role,
            isActive: true,
            isEmailConfirmed: true);
        user.SetPasswordHash(passwordHasher.HashPassword(user, "password123"));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedOrganizationAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organization = new DomainOrganization(
            "InMemory membership organization",
            CreateAddress(),
            $"IM{Guid.NewGuid():N}"[..10],
            null);
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private async Task<Guid> SeedBranchAsync(Guid organizationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var branch = new DomainBranch(
            "InMemory membership branch",
            CreateAddress(),
            $"IB{Guid.NewGuid():N}"[..10],
            organizationId);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        return branch.Id;
    }

    private async Task<Guid> SeedArchivedOrganizationBranchAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organization = new DomainOrganization(
            "InMemory foreign organization",
            CreateAddress(),
            $"IF{Guid.NewGuid():N}"[..10],
            null);
        organization.Archive();
        var branch = new DomainBranch(
            "InMemory foreign branch",
            CreateAddress(),
            $"FB{Guid.NewGuid():N}"[..10],
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