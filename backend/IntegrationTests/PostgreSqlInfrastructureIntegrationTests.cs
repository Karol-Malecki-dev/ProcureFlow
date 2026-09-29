using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.JWT;
using Domain.Enums;
using Domain.Interfaces;
using Domain.ValueObjects;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace IntegrationTests;

[Collection(nameof(PostgreSqlIntegrationTestCollection))]
public sealed class PostgreSqlInfrastructureIntegrationTests
{
    private readonly PostgreSqlWebApplicationFactory _factory;

    public PostgreSqlInfrastructureIntegrationTests(PostgreSqlWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostgreSql_container_applies_all_migrations_and_serves_health_check()
    {
        using var client = _factory.CreateClient();

        var healthResponse = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        var knownMigrations = dbContext.Database.GetMigrations();
        Assert.Equal(knownMigrations.Order(), appliedMigrations.Order());
        Assert.Contains(
            "20260922113941_AddPurchaseRequestAttachmentConstraints",
            appliedMigrations);
    }

    [Fact]
    public async Task PostgreSql_readiness_reports_attachment_storage_as_healthy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.Trim(), ignoreCase: true);
    }

    [Fact]
    public async Task PostgreSql_refresh_rotation_accepts_only_one_concurrent_successor()
    {
        await SeedUserAsync();

        JwtTokens initialTokens;
        Guid userId;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var serviceProvider = setupScope.ServiceProvider;
            var tokenService = serviceProvider.GetRequiredService<IJwtTokenService>();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users
                .SingleAsync(candidate => candidate.Email == EmailAddress.Create("postgres-concurrent@example.com"));

            userId = user.Id;
            initialTokens = await tokenService.GenerateTokensAsync(user);
        }

        await using var firstScope = _factory.Services.CreateAsyncScope();
        await using var secondScope = _factory.Services.CreateAsyncScope();
        var firstTokenService = firstScope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var secondTokenService = secondScope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var refreshResults = await Task.WhenAll(
            firstTokenService.RefreshTokensAsync(initialTokens.RefreshToken),
            secondTokenService.RefreshTokensAsync(initialTokens.RefreshToken));

        Assert.Single(refreshResults, tokens => tokens is not null);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedTokens = await verificationContext.RefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync();

        Assert.Equal(2, storedTokens.Count);
        Assert.Single(storedTokens, token => token.RevocationReason == RevocationReason.TokenRotated);
        Assert.Single(storedTokens, token => token.RevocationReason == RevocationReason.RefreshTokenReplay);
        Assert.DoesNotContain(storedTokens, token => !token.RevokedAt.HasValue);
    }

    private async Task SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(User.Create(
            EmailAddress.Create("postgres-concurrent@example.com"),
            DisplayName.Create("PostgreSQL Concurrent User"),
            UserRole.User,
            isActive: true,
            isEmailConfirmed: true));
        await dbContext.SaveChangesAsync();
    }
}