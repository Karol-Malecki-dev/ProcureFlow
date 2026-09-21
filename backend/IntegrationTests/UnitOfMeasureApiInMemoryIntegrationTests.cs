using API.Modules.Catalog.UnitOfMeasure;
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
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace IntegrationTests;

public sealed class UnitOfMeasureApiInMemoryIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public UnitOfMeasureApiInMemoryIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Unit_of_measure_create_returns_unauthorized_without_token()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{Guid.NewGuid()}/catalog/units-of-measure",
            new
            {
                Name = "Kilogram",
                Symbol = "kg"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_normalized_unit_of_measure()
    {
        var organizationId = await SeedOrganizationAsync();
        var adminEmail = UniqueEmail("catalog-admin");
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/catalog/units-of-measure",
            new
            {
                Name = " Kilogram ",
                Symbol = " KG "
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<UnitOfMeasureResponse>>();

        Assert.NotNull(payload?.Data);
        Assert.Equal(organizationId, payload.Data.OrganizationId);
        Assert.Equal("Kilogram", payload.Data.Name);
        Assert.Equal("kg", payload.Data.Symbol);
        Assert.True(payload.Data.IsActive);
    }

    [Fact]
    public async Task Procurement_member_can_create_unit_of_measure_for_organization()
    {
        var organizationId = await SeedOrganizationAsync();
        var procurementEmail = UniqueEmail("catalog-procurement");
        var procurementUserId = await SeedUserAsync(procurementEmail, UserRole.User);
        await SeedProcurementMembershipAsync(organizationId, procurementUserId);
        await AuthenticateAsync(procurementEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/catalog/units-of-measure",
            new
            {
                Name = "Liter",
                Symbol = "L"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task User_without_procurement_membership_cannot_create_unit_of_measure()
    {
        var organizationId = await SeedOrganizationAsync();
        var userEmail = UniqueEmail("catalog-reader");
        await SeedUserAsync(userEmail, UserRole.User);
        await AuthenticateAsync(userEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/catalog/units-of-measure",
            new
            {
                Name = "Hour",
                Symbol = "h"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unit_of_measure_create_returns_conflict_for_duplicate_active_symbol()
    {
        var organizationId = await SeedOrganizationAsync();
        var adminEmail = UniqueEmail("catalog-duplicate-admin");
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var request = new
        {
            Name = "Kilogram",
            Symbol = "kg"
        };

        var firstResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/catalog/units-of-measure",
            request);
        var secondResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/catalog/units-of-measure",
            new
            {
                Name = "Kilo",
                Symbol = " KG "
            });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
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
            DisplayName.Create("Catalog test user"),
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
            "Catalog test organization",
            CreateAddress(),
            $"CAT{Guid.NewGuid():N}"[..10],
            null);
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private async Task SeedProcurementMembershipAsync(Guid organizationId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Memberships.Add(new DomainMembership(
            organizationId,
            userId,
            branchId: null,
            BusinessRole.Procurement));
        await dbContext.SaveChangesAsync();
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