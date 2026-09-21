using Application.Modules.PurchaseRequests;
using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Enums;
using Domain.Models.Catalog;
using Domain.Models.Organizations;
using Domain.Models.Organizations.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace IntegrationTests;

[Collection(nameof(PostgreSqlIntegrationTestCollection))]
public sealed class PurchaseRequestsPostgreSqlIntegrationTests
{
    private readonly PostgreSqlWebApplicationFactory _factory;

    public PurchaseRequestsPostgreSqlIntegrationTests(PostgreSqlWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostgreSql_purchase_request_item_constraints_reject_invalid_foreign_key_and_duplicate_line()
    {
        var data = await SeedDraftAsync(includeItem: true);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var foreignKeyException = await Assert.ThrowsAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PurchaseRequestItems"
                    ("Id", "PurchaseRequestId", "ProductId", "ProductNameSnapshot",
                     "ProductCodeSnapshot", "UnitNameSnapshot", "UnitSymbolSnapshot",
                     "UnitPriceSnapshot", "Quantity", "Comment")
                VALUES
                    ({Guid.NewGuid()}, {data.RequestId}, {Guid.NewGuid()}, 'Monitor', 'MON-1',
                     'Piece', 'pc', {10.50m}, {1m}, NULL);
                """));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, foreignKeyException.SqlState);

        var duplicateException = await Assert.ThrowsAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PurchaseRequestItems"
                    ("Id", "PurchaseRequestId", "ProductId", "ProductNameSnapshot",
                     "ProductCodeSnapshot", "UnitNameSnapshot", "UnitSymbolSnapshot",
                     "UnitPriceSnapshot", "Quantity", "Comment")
                VALUES
                    ({Guid.NewGuid()}, {data.RequestId}, {data.ProductId}, 'Monitor', 'MON-1',
                     'Piece', 'pc', {10.50m}, {1m}, NULL);
                """));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicateException.SqlState);
    }

    [Fact]
    public async Task PostgreSql_purchase_request_item_check_constraints_and_numeric_scales_match_the_contract()
    {
        var data = await SeedDraftAsync(includeItem: false);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var checkException = await Assert.ThrowsAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PurchaseRequestItems"
                    ("Id", "PurchaseRequestId", "ProductId", "ProductNameSnapshot",
                     "ProductCodeSnapshot", "UnitNameSnapshot", "UnitSymbolSnapshot",
                     "UnitPriceSnapshot", "Quantity", "Comment")
                VALUES
                    ({Guid.NewGuid()}, {data.RequestId}, {data.ProductId}, 'Monitor', 'MON-1',
                     'Piece', 'pc', {10.50m}, {0m}, NULL);
                """));

        Assert.Equal(PostgresErrorCodes.CheckViolation, checkException.SqlState);

        await using var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "column_name", numeric_precision::int, numeric_scale::int
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'PurchaseRequestItems'
              AND "column_name" IN ('UnitPriceSnapshot', 'Quantity');
            """;

        var columns = new Dictionary<string, (int Precision, int Scale)>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns[reader.GetString(0)] = (
                Convert.ToInt32(reader.GetValue(1)),
                Convert.ToInt32(reader.GetValue(2)));
        }

        Assert.Equal((12, 2), columns["UnitPriceSnapshot"]);
        Assert.Equal((12, 3), columns["Quantity"]);
    }

    [Fact]
    public async Task PostgreSql_purchase_request_stale_writer_is_rejected_by_the_concurrency_stamp()
    {
        var data = await SeedDraftAsync(includeItem: true);

        await using var staleScope = _factory.Services.CreateAsyncScope();
        await using var writerScope = _factory.Services.CreateAsyncScope();
        var staleContext = staleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var writerContext = writerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var staleRequest = await staleContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleAsync(request => request.Id == data.RequestId);
        var writerRequest = await writerContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleAsync(request => request.Id == data.RequestId);

        var itemId = Assert.Single(staleRequest.Items).Id;
        writerRequest.UpdateItemQuantity(itemId, 3m);
        await writerContext.SaveChangesAsync();

        staleRequest.UpdateItemQuantity(itemId, 4m);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => staleContext.SaveChangesAsync());

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedItem = await verificationContext.PurchaseRequestItems
            .SingleAsync(item => item.Id == itemId);
        Assert.Equal(3m, persistedItem.Quantity);
    }

    [Fact]
    public async Task PostgreSql_purchase_request_list_paging_uses_updated_time_and_id_as_stable_order()
    {
        var data = await SeedScopeAsync();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var requests = Enumerable.Range(0, 3)
            .Select(index => PurchaseRequest.Create(
                data.UserId,
                data.OrganizationId,
                data.BranchId,
                $"Request {index}",
                createdAt))
            .ToList();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.PurchaseRequests.AddRange(requests);
            await dbContext.SaveChangesAsync();
        }

        await using var queryScope = _factory.Services.CreateAsyncScope();
        var queryContext = queryScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = new EfListMyPurchaseRequestsStore(queryContext);
        var membership = new PurchaseRequestMembership(
            data.MembershipId,
            data.UserId,
            data.OrganizationId,
            data.BranchId,
            BusinessRole.Employee,
            true,
            true,
            true);

        var firstPage = await store.QueryAsync(membership, 1, 2);
        var secondPage = await store.QueryAsync(membership, 2, 2);
        var expectedIds = requests
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .Select(request => request.Id)
            .ToArray();

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(expectedIds[..2], firstPage.Items.Select(item => item.Id));
        Assert.Equal(expectedIds[2..], secondPage.Items.Select(item => item.Id));
    }

    private async Task<PurchaseRequestSeed> SeedDraftAsync(bool includeItem)
    {
        var data = await SeedScopeAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var request = PurchaseRequest.Create(
            data.UserId,
            data.OrganizationId,
            data.BranchId);
        if (includeItem)
        {
            request.AddItem(
                data.ProductId,
                "Monitor",
                "MON-1",
                "Piece",
                "pc",
                10.50m,
                2m);
        }

        dbContext.PurchaseRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return data with { RequestId = request.Id };
    }

    private async Task<PurchaseRequestSeed> SeedScopeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = User.Create(
            EmailAddress.Create($"purchase-request-postgres-{Guid.NewGuid():N}@example.com"),
            DisplayName.Create("Purchase request PostgreSQL user"),
            UserRole.User,
            isActive: true,
            isEmailConfirmed: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(candidate => !candidate.IsArchived);
        if (organization is null)
        {
            organization = new DomainOrganization(
                "Purchase request PostgreSQL organization",
                CreateAddress(),
                $"PG{Guid.NewGuid():N}"[..10],
                null);
            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync();
        }

        var branch = await dbContext.Branches
            .OrderBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate =>
                candidate.OrganizationId == organization.Id &&
                !candidate.IsArchived);
        if (branch is null)
        {
            branch = new DomainBranch(
                "Purchase request PostgreSQL branch",
                CreateAddress(),
                $"PB{Guid.NewGuid():N}"[..10],
                organization.Id);
            dbContext.Branches.Add(branch);
            await dbContext.SaveChangesAsync();
        }

        var unit = new UnitOfMeasure(
            organization.Id,
            "Piece",
            $"u{Guid.NewGuid():N}"[..10],
            user.Id);
        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();

        var product = new Product(
            organization.Id,
            "Monitor",
            $"MON-{Guid.NewGuid():N}",
            unit.Id,
            10.50m,
            user.Id);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var membership = new DomainMembership(
            organization.Id,
            user.Id,
            branch.Id,
            BusinessRole.Employee);
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        return new PurchaseRequestSeed(
            user.Id,
            organization.Id,
            branch.Id,
            membership.Id,
            product.Id,
            Guid.Empty);
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

    private sealed record PurchaseRequestSeed(
        Guid UserId,
        Guid OrganizationId,
        Guid BranchId,
        Guid MembershipId,
        Guid ProductId,
        Guid RequestId);
}