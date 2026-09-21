using API.Modules.Catalog.UnitOfMeasure;
using Application.DTOs.Auth;
using Domain.Entities;
using Domain.Enums;
using Domain.Models.Catalog;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shared.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace IntegrationTests;

[Collection(nameof(PostgreSqlIntegrationTestCollection))]
public sealed class UnitOfMeasurePostgreSqlIntegrationTests
{
    private readonly PostgreSqlWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UnitOfMeasurePostgreSqlIntegrationTests(PostgreSqlWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostgreSql_rejects_duplicate_active_symbol_with_named_constraint()
    {
        var organizationId = await SeedOrganizationAsync();
        var userId = await SeedUserAsync(UniqueEmail("catalog-constraint"));
        var symbol = CreateUniqueSymbol();

        await using var firstScope = _factory.Services.CreateAsyncScope();
        var firstDbContext = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        firstDbContext.UnitsOfMeasure.Add(new UnitOfMeasure(
            organizationId,
            "Kilogram",
            symbol,
            userId));
        await firstDbContext.SaveChangesAsync();

        await using var duplicateScope = _factory.Services.CreateAsyncScope();
        var duplicateDbContext = duplicateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        duplicateDbContext.UnitsOfMeasure.Add(new UnitOfMeasure(
            organizationId,
            "Kilo",
            symbol.ToUpperInvariant(),
            userId));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateDbContext.SaveChangesAsync());
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
        Assert.Equal(
            "UX_UnitOfMeasures_Organization_ActiveSymbol",
            postgresException.ConstraintName);
    }

    [Fact]
    public async Task Concurrent_unit_creation_allows_only_one_active_symbol()
    {
        var organizationId = await SeedOrganizationAsync();
        var adminEmail = UniqueEmail("catalog-concurrent-admin");
        var symbol = CreateUniqueSymbol();
        await SeedUserAsync(adminEmail, UserRole.Admin);
        await AuthenticateAsync(adminEmail);

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync(
                $"/api/organizations/{organizationId}/catalog/units-of-measure",
                new { Name = "Kilogram", Symbol = symbol }),
            _client.PostAsJsonAsync(
                $"/api/organizations/{organizationId}/catalog/units-of-measure",
                new { Name = "Kilo", Symbol = symbol.ToUpperInvariant() }));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await dbContext.UnitsOfMeasure.CountAsync(unit =>
                unit.OrganizationId == organizationId
                && unit.IsActive
                && unit.Symbol == symbol));
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

    private async Task<Guid> SeedUserAsync(string email, UserRole role = UserRole.User)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher<User>();
        var user = User.Create(
            EmailAddress.Create(email),
            DisplayName.Create("Catalog PostgreSQL user"),
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
        var existingOrganization = await dbContext.Organizations
            .SingleOrDefaultAsync(organization => !organization.IsArchived);

        if (existingOrganization is not null)
        {
            return existingOrganization.Id;
        }

        var organization = new DomainOrganization(
            "Catalog PostgreSQL organization",
            CreateAddress(),
            $"PGCAT{Guid.NewGuid():N}"[..12],
            null);
        dbContext.Organizations.Add(organization);
        await dbContext.SaveChangesAsync();
        return organization.Id;
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

    private static string CreateUniqueSymbol()
        => $"u{Guid.NewGuid():N}"[..10];
}