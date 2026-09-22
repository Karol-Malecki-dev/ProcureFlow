using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;
using Application.Modules.PurchaseRequests.Attachments;
using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Enums;
using Domain.Models.Catalog;
using Domain.Models.Organizations;
using Domain.Models.Organizations.Enums;
using Domain.ValueObjects;
using Infrastructure.Data;
using Infrastructure.Modules.PurchaseRequests.Approval;
using Infrastructure.Modules.PurchaseRequests.Attachments;
using Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Infrastructure.Modules.PurchaseRequests.PurchaseRequestMembershipReader;
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
    public async Task PostgreSql_purchase_request_attachment_foreign_key_and_cleanup_queue_are_persisted()
    {
        var data = await SeedDraftAsync(includeItem: false);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attachment = PurchaseRequestAttachment.Create(
            data.RequestId,
            data.UserId,
            "quote.txt",
            $"{Guid.NewGuid():N}.txt",
            "text/plain",
            10);
        dbContext.PurchaseRequestAttachments.Add(attachment);
        await dbContext.SaveChangesAsync();

        var cleanupMessage = PurchaseRequestAttachmentCleanupMessage.Create(
            attachment.StoredFileName);
        dbContext.PurchaseRequestAttachmentCleanupMessages.Add(cleanupMessage);
        await dbContext.SaveChangesAsync();

        Assert.Equal(
            attachment.StoredFileName,
            await dbContext.PurchaseRequestAttachmentCleanupMessages
                .Select(message => message.StoredFileName)
                .SingleAsync());

        var foreignKeyException = await Assert.ThrowsAsync<PostgresException>(() =>
            dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "PurchaseRequestAttachments"
                    ("Id", "PurchaseRequestId", "UploadedByUserId", "OriginalFileName",
                     "StoredFileName", "ContentType", "SizeBytes", "CreatedAt")
                VALUES
                    ({Guid.NewGuid()}, {Guid.NewGuid()}, {data.UserId}, 'orphan.txt',
                     '{Guid.NewGuid():N}.txt', 'text/plain', {10L}, {DateTime.UtcNow});
                """));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, foreignKeyException.SqlState);
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
    public async Task PostgreSql_concurrent_request_attachment_uploads_enforce_count_quota()
    {
        var data = await SeedDraftAsync(includeItem: false);

        await using var firstScope = _factory.Services.CreateAsyncScope();
        await using var secondScope = _factory.Services.CreateAsyncScope();
        var firstStore = new EfPurchaseRequestAttachmentStore(
            firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        var secondStore = new EfPurchaseRequestAttachmentStore(
            secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        var results = await Task.WhenAll(
            CaptureAttachmentCreateResultAsync(
                firstStore,
                PurchaseRequestAttachment.Create(
                    data.RequestId,
                    data.UserId,
                    "first.txt",
                    $"{Guid.NewGuid():N}.txt",
                    "text/plain",
                    4)),
            CaptureAttachmentCreateResultAsync(
                secondStore,
                PurchaseRequestAttachment.Create(
                    data.RequestId,
                    data.UserId,
                    "second.txt",
                    $"{Guid.NewGuid():N}.txt",
                    "text/plain",
                    4)));

        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is PurchaseRequestAttachmentQuotaExceededException);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await verificationContext.PurchaseRequestAttachments
                .CountAsync(attachment => attachment.PurchaseRequestId == data.RequestId));
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

    [Fact]
    public async Task PostgreSql_parallel_manager_approvals_cannot_overallocate_monthly_budget()
    {
        var data = await SeedApprovalScopeAsync([1m, 1m], 10.50m);

        var results = await Task.WhenAll(
            data.Requests.Select(request => ExecuteManagerDecisionAsync(data, request)));

        Assert.Equal(
            1,
            results.Count(result =>
                result.IsSuccess
                && result.Value?.Status == PurchaseRequestStatus.Approved));
        Assert.All(
            results,
            result => Assert.True(
                result.IsSuccess || result.Status == PurchaseRequestOperationStatus.Conflict));

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var requests = await verificationContext.PurchaseRequests
            .Where(request => data.Requests.Select(seed => seed.Id).Contains(request.Id))
            .ToListAsync();
        var budget = await verificationContext.BranchMonthlyBudgets
            .SingleAsync(item => item.Id == data.BudgetId);
        var decisions = await verificationContext.PurchaseRequestApprovalDecisions
            .Where(decision => data.Requests.Select(seed => seed.Id).Contains(decision.PurchaseRequestId))
            .ToListAsync();

        Assert.Single(requests, request => request.Status == PurchaseRequestStatus.Approved);
        Assert.Equal(10.50m, budget.UsedAmount);
        Assert.Single(decisions, decision => decision.Decision == PurchaseRequestDecisionType.Approved);
        Assert.DoesNotContain(
            requests,
            request => request.Status == PurchaseRequestStatus.Approved
                && decisions.Count(decision => decision.PurchaseRequestId == request.Id) != 1);
    }

    [Fact]
    public async Task PostgreSql_parallel_decisions_for_one_request_have_one_winner_and_one_atomic_result()
    {
        var data = await SeedApprovalScopeAsync([1m], 100m);
        var request = Assert.Single(data.Requests);

        var results = await Task.WhenAll(
            ExecuteManagerDecisionAsync(data, request),
            ExecuteManagerDecisionAsync(data, request));

        Assert.Equal(1, results.Count(result => result.IsSuccess));
        Assert.Equal(
            1,
            results.Count(result =>
                !result.IsSuccess
                && result.Status == PurchaseRequestOperationStatus.Conflict));

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await verificationContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        var persistedBudget = await verificationContext.BranchMonthlyBudgets
            .SingleAsync(item => item.Id == data.BudgetId);
        var decisions = await verificationContext.PurchaseRequestApprovalDecisions
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync();
        var history = await verificationContext.PurchaseRequestStatusHistories
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync();

        Assert.Equal(PurchaseRequestStatus.Approved, persistedRequest.Status);
        Assert.Equal(10.50m, persistedBudget.UsedAmount);
        Assert.Single(decisions);
        Assert.Single(history);
    }

    [Fact]
    public async Task PostgreSql_failed_approval_does_not_leave_partial_budget_usage()
    {
        var data = await SeedApprovalScopeAsync([1m], 100m);
        var request = Assert.Single(data.Requests);
        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);

        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedContext.PurchaseRequestApprovalDecisions.Add(
                PurchaseRequestApprovalDecision.Create(
                    request.Id,
                    data.ManagerUserId,
                    BusinessRole.Manager,
                    PurchaseRequestDecisionType.Escalated,
                    request.TotalValue,
                    availableBudget: 0m,
                    overBudgetAmount: request.TotalValue,
                    budgetYear: period.Year,
                    budgetMonth: period.Month));
            await seedContext.SaveChangesAsync();
        }

        var result = await ExecuteManagerDecisionAsync(data, request);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Conflict, result.Status);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await verificationContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        var persistedBudget = await verificationContext.BranchMonthlyBudgets
            .SingleAsync(item => item.Id == data.BudgetId);
        var decisions = await verificationContext.PurchaseRequestApprovalDecisions
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync();
        var history = await verificationContext.PurchaseRequestStatusHistories
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync();

        Assert.Equal(PurchaseRequestStatus.Submitted, persistedRequest.Status);
        Assert.Equal(0m, persistedBudget.UsedAmount);
        Assert.Single(decisions);
        Assert.Empty(history);
    }

    private async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> ExecuteManagerDecisionAsync(
        ApprovalSeed data,
        ApprovalRequestSeed request,
        string? expectedConcurrencyStamp = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handler = new DecidePurchaseRequestHandler(
            new EfPurchaseRequestMembershipReader(dbContext),
            new EfPurchaseRequestApprovalStore(dbContext));

        return await handler.HandleAsync(
            new DecidePurchaseRequestCommand(
                data.ManagerUserId,
                data.OrganizationId,
                request.Id,
                expectedConcurrencyStamp ?? request.ConcurrencyStamp,
                Approve: true,
                RejectionReason: null));
    }

    private async Task<ApprovalSeed> SeedApprovalScopeAsync(
        IReadOnlyList<decimal> quantities,
        decimal budgetLimit)
    {
        if (quantities.Count == 0)
        {
            throw new ArgumentException("At least one approval request is required.", nameof(quantities));
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employee = User.Create(
            EmailAddress.Create($"purchase-request-approval-employee-{Guid.NewGuid():N}@example.com"),
            DisplayName.Create("Purchase request approval employee"),
            UserRole.User,
            isActive: true,
            isEmailConfirmed: true);
        var manager = User.Create(
            EmailAddress.Create($"purchase-request-approval-manager-{Guid.NewGuid():N}@example.com"),
            DisplayName.Create("Purchase request approval manager"),
            UserRole.User,
            isActive: true,
            isEmailConfirmed: true);
        dbContext.Users.AddRange(employee, manager);
        await dbContext.SaveChangesAsync();

        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(candidate => !candidate.IsArchived);
        if (organization is null)
        {
            organization = new DomainOrganization(
                "Purchase request approval PostgreSQL organization",
                CreateAddress(),
                $"PA{Guid.NewGuid():N}"[..10],
                null);
            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync();
        }

        var branch = new DomainBranch(
            $"Purchase request approval PostgreSQL branch {Guid.NewGuid():N}",
            CreateAddress(),
            $"PB{Guid.NewGuid():N}"[..10],
            organization.Id);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        var unit = new UnitOfMeasure(
            organization.Id,
            "Piece",
            $"u{Guid.NewGuid():N}"[..10],
            employee.Id);
        var product = new Product(
            organization.Id,
            "Monitor",
            $"MON-{Guid.NewGuid():N}",
            unit.Id,
            10.50m,
            employee.Id);
        dbContext.UnitsOfMeasure.Add(unit);
        dbContext.Products.Add(product);
        dbContext.Memberships.AddRange(
            new DomainMembership(
                organization.Id,
                employee.Id,
                branch.Id,
                BusinessRole.Employee),
            new DomainMembership(
                organization.Id,
                manager.Id,
                branch.Id,
                BusinessRole.Manager));

        var requests = quantities
            .Select(quantity =>
            {
                var request = PurchaseRequest.Create(
                    employee.Id,
                    organization.Id,
                    branch.Id,
                    "PostgreSQL approval concurrency request");
                request.AddItem(
                    product.Id,
                    "Monitor",
                    "MON-1",
                    "Piece",
                    "pc",
                    10.50m,
                    quantity);
                request.Submit();
                return request;
            })
            .ToList();
        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        var budget = BranchMonthlyBudget.Create(
            branch.Id,
            period.Year,
            period.Month,
            budgetLimit);

        dbContext.PurchaseRequests.AddRange(requests);
        dbContext.BranchMonthlyBudgets.Add(budget);
        await dbContext.SaveChangesAsync();

        return new ApprovalSeed(
            organization.Id,
            branch.Id,
            manager.Id,
            budget.Id,
            requests
                .Select(request => new ApprovalRequestSeed(
                    request.Id,
                    request.ConcurrencyStamp,
                    request.TotalValue))
                .ToArray());
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

    private static async Task<Exception?> CaptureAttachmentCreateResultAsync(
        EfPurchaseRequestAttachmentStore store,
        PurchaseRequestAttachment attachment)
    {
        try
        {
            await store.CreateAsync(attachment, maxCount: 1, maxBytes: 100);
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private sealed record PurchaseRequestSeed(
        Guid UserId,
        Guid OrganizationId,
        Guid BranchId,
        Guid MembershipId,
        Guid ProductId,
        Guid RequestId);

    private sealed record ApprovalSeed(
        Guid OrganizationId,
        Guid BranchId,
        Guid ManagerUserId,
        Guid BudgetId,
        IReadOnlyList<ApprovalRequestSeed> Requests);

    private sealed record ApprovalRequestSeed(
        Guid Id,
        string ConcurrencyStamp,
        decimal TotalValue);
}