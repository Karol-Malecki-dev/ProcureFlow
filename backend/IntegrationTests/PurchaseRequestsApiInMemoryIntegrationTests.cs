using Application.DTOs.Auth;
using API.Modules.Catalog.ProductRead;
using API.Modules.PurchaseRequests;
using Domain.Entities;
using Domain.Enums;
using Domain.Models.Catalog;
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
using DomainProduct = Domain.Models.Catalog.Product;
using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;

namespace IntegrationTests;

public sealed class PurchaseRequestsApiInMemoryIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public PurchaseRequestsApiInMemoryIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Purchase_request_create_returns_unauthorized_without_token()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{Guid.NewGuid()}/purchase-requests",
            new { Note = "Draft" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_can_list_selectable_catalog_products_in_membership_scope()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var email = UniqueEmail("catalog-products-employee");
        var userId = await SeedUserAsync(email);
        var unitId = await SeedUnitAsync(organizationId, userId);
        var productId = await SeedProductAsync(organizationId, unitId, userId);
        await SeedMembershipAsync(organizationId, userId, branchId, BusinessRole.Employee);
        await AuthenticateAsync(email);

        var response = await _client.GetAsync(
            $"/api/organizations/{organizationId}/catalog/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<IReadOnlyList<SelectableProductResponse>>>();

        Assert.NotNull(payload?.Data);
        var product = Assert.Single(payload.Data);
        Assert.Equal(productId, product.Id);
        Assert.Equal("Monitor", product.Name);
        Assert.Equal("MON-1", product.Code);
        Assert.Equal("Piece", product.UnitName);
        Assert.Equal("pc", product.UnitSymbol);
        Assert.Equal(10.50m, product.UnitPrice);
    }

    [Fact]
    public async Task Employee_can_create_empty_draft_using_membership_scope()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var email = UniqueEmail("purchase-request-employee");
        var userId = await SeedUserAsync(email);
        await SeedMembershipAsync(organizationId, userId, branchId, BusinessRole.Employee);
        await AuthenticateAsync(email);

        var forgedBranchId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests",
            new
            {
                Note = "  Office supplies  ",
                AuthorUserId = Guid.NewGuid(),
                BranchId = forgedBranchId,
                Status = "Submitted",
                TotalValue = 999999m,
                Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 10m } }
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.NotNull(payload?.Data);
        Assert.Equal(userId, payload.Data.AuthorUserId);
        Assert.Equal(organizationId, payload.Data.OrganizationId);
        Assert.Equal(branchId, payload.Data.BranchId);
        Assert.Equal(PurchaseRequestStatus.Draft, payload.Data.Status);
        Assert.Equal("Office supplies", payload.Data.Note);
        Assert.Empty(payload.Data.Items);
        Assert.Equal(0m, payload.Data.TotalValue);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.ConcurrencyStamp));

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await dbContext.PurchaseRequests
            .SingleAsync(request => request.Id == payload.Data.Id);
        Assert.Equal(branchId, persisted.BranchId);
        Assert.Equal(userId, persisted.AuthorUserId);
    }

    [Fact]
    public async Task Manager_cannot_create_purchase_request_draft()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var email = UniqueEmail("purchase-request-manager");
        var userId = await SeedUserAsync(email);
        await SeedMembershipAsync(organizationId, userId, branchId, BusinessRole.Manager);
        await AuthenticateAsync(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests",
            new { Note = "Manager must not create" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_request_create_rejects_an_overlong_note()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var email = UniqueEmail("purchase-request-validation");
        var userId = await SeedUserAsync(email);
        await SeedMembershipAsync(organizationId, userId, branchId, BusinessRole.Employee);
        await AuthenticateAsync(email);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests",
            new { Note = new string('x', 2_001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(payload?.Errors);
        Assert.Contains(payload.Errors, error => error.Field == "Note");
    }

    [Fact]
    public async Task Employee_can_edit_a_draft_and_remove_the_last_item()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var email = UniqueEmail("purchase-request-edit");
        var userId = await SeedUserAsync(email);
        var unitId = await SeedUnitAsync(organizationId, userId);
        var productId = await SeedProductAsync(organizationId, unitId, userId);
        await SeedMembershipAsync(organizationId, userId, branchId, BusinessRole.Employee);
        await AuthenticateAsync(email);

        var createResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests",
            new { Note = "Office equipment" });
        var created = await ReadResponseAsync(createResponse);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created.Data);

        var addResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{created.Data.Id}/items",
            new
            {
                ProductId = productId,
                Quantity = 2.5m,
                Comment = "Two screens",
                ConcurrencyStamp = created.Data.ConcurrencyStamp
            });
        var added = await ReadResponseAsync(addResponse);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        Assert.NotNull(added.Data);
        var addedItem = Assert.Single(added.Data.Items);
        Assert.Equal(productId, addedItem.ProductId);
        Assert.Equal("Monitor", addedItem.ProductName);
        Assert.Equal("MON-1", addedItem.ProductCode);
        Assert.Equal("Piece", addedItem.UnitName);
        Assert.Equal("pc", addedItem.UnitSymbol);
        Assert.Equal(10.50m, addedItem.UnitPrice);
        Assert.Equal(2.5m, addedItem.Quantity);
        Assert.Equal(26.25m, added.Data.TotalValue);

        var updateResponse = await _client.PatchAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{created.Data.Id}/items/{addedItem.Id}",
            new
            {
                Quantity = 3m,
                ConcurrencyStamp = added.Data.ConcurrencyStamp
            });
        var updated = await ReadResponseAsync(updateResponse);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated.Data);
        Assert.Equal(3m, Assert.Single(updated.Data.Items).Quantity);
        Assert.Equal(31.50m, updated.Data.TotalValue);

        var staleRemoveRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/organizations/{organizationId}/purchase-requests/{created.Data.Id}/items/{addedItem.Id}")
        {
            Content = JsonContent.Create(new { ConcurrencyStamp = added.Data.ConcurrencyStamp })
        };
        var staleRemoveResponse = await _client.SendAsync(staleRemoveRequest);
        Assert.Equal(HttpStatusCode.Conflict, staleRemoveResponse.StatusCode);

        var removeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/organizations/{organizationId}/purchase-requests/{created.Data.Id}/items/{addedItem.Id}")
        {
            Content = JsonContent.Create(new { ConcurrencyStamp = updated.Data.ConcurrencyStamp })
        };
        var removeResponse = await _client.SendAsync(removeRequest);
        var removed = await ReadResponseAsync(removeResponse);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.NotNull(removed.Data);
        Assert.Empty(removed.Data.Items);
        Assert.Equal(0m, removed.Data.TotalValue);

        var detailsResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{created.Data.Id}");
        var details = await ReadResponseAsync(detailsResponse);
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        Assert.NotNull(details.Data);
        Assert.Empty(details.Data.Items);

        var listResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests?page=1&pageSize=20");
        var list = await listResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestListResponse>>();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list?.Data);
        var listItem = Assert.Single(list.Data.Items);
        Assert.Equal(created.Data.Id, listItem.Id);
        Assert.Equal(0, listItem.ItemCount);
        Assert.Equal(0m, listItem.TotalValue);
    }

    [Fact]
    public async Task Procurement_can_create_and_manager_can_read_a_branch_monthly_budget()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var procurementEmail = UniqueEmail("budget-procurement");
        var managerEmail = UniqueEmail("budget-manager");
        var procurementUserId = await SeedUserAsync(procurementEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        await SeedMembershipAsync(
            organizationId,
            procurementUserId,
            null,
            BusinessRole.Procurement);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);

        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        await AuthenticateAsync(procurementEmail);

        var createResponse = await _client.PutAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/budgets/{branchId}/{period.Year}/{period.Month}",
            new { LimitAmount = 5_000m });
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<BranchMonthlyBudgetResponse>>();

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(created?.Data);
        Assert.Equal(5_000m, created.Data.LimitAmount);
        Assert.Equal(0m, created.Data.UsedAmount);

        await AuthenticateAsync(managerEmail);
        var readResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/budgets/{branchId}/{period.Year}/{period.Month}");
        var read = await readResponse.Content
            .ReadFromJsonAsync<ApiResponse<BranchMonthlyBudgetResponse>>();

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.NotNull(read?.Data);
        Assert.Equal(created.Data.Id, read.Data.Id);
        Assert.Equal(created.Data.ConcurrencyStamp, read.Data.ConcurrencyStamp);
        Assert.Equal(5_000m, read.Data.AvailableAmount);
    }

    [Fact]
    public async Task Manager_can_approve_a_submitted_request_and_reserve_the_budget()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("approval-employee");
        var managerEmail = UniqueEmail("approval-manager");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);

        var request = await SeedSubmittedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            2m);
        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        await SeedBudgetAsync(branchId, period.Year, period.Month, 1_000m);
        await AuthenticateAsync(managerEmail);

        var queueResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/approval-queue");
        var queue = await queueResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestApprovalQueueResponse>>();

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue?.Data);
        var queueItem = Assert.Single(queue.Data.Items);
        Assert.Equal(request.Id, queueItem.Id);
        Assert.True(queueItem.CanDecide);
        Assert.Equal(PurchaseRequestStatus.Submitted, queueItem.Status);

        var decisionResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = request.ConcurrencyStamp,
                Approve = true,
                RejectionReason = (string?)null
            });
        var decision = await decisionResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, decisionResponse.StatusCode);
        Assert.NotNull(decision?.Data);
        Assert.Equal(PurchaseRequestStatus.Approved, decision.Data.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await dbContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        var persistedBudget = await dbContext.BranchMonthlyBudgets
            .SingleAsync(item => item.BranchId == branchId);
        var persistedDecision = await dbContext.PurchaseRequestApprovalDecisions
            .SingleAsync(item => item.PurchaseRequestId == request.Id);
        var persistedHistory = await dbContext.PurchaseRequestStatusHistories
            .SingleAsync(item => item.PurchaseRequestId == request.Id);

        Assert.Equal(PurchaseRequestStatus.Approved, persistedRequest.Status);
        Assert.Equal(request.TotalValue, persistedBudget.UsedAmount);
        Assert.Equal(PurchaseRequestDecisionType.Approved, persistedDecision.Decision);
        Assert.Equal(PurchaseRequestStatus.Submitted, persistedHistory.FromStatus);
        Assert.Equal(PurchaseRequestStatus.Approved, persistedHistory.ToStatus);
    }

    [Fact]
    public async Task Manager_escalates_over_budget_and_procurement_can_approve_the_request()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("escalation-employee");
        var managerEmail = UniqueEmail("escalation-manager");
        var procurementEmail = UniqueEmail("escalation-procurement");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        var procurementUserId = await SeedUserAsync(procurementEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);
        await SeedMembershipAsync(
            organizationId,
            procurementUserId,
            null,
            BusinessRole.Procurement);

        var request = await SeedSubmittedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            10m);
        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        await SeedBudgetAsync(branchId, period.Year, period.Month, 100m);

        await AuthenticateAsync(managerEmail);
        var escalationResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = request.ConcurrencyStamp,
                Approve = true,
                RejectionReason = (string?)null
            });
        var escalation = await escalationResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, escalationResponse.StatusCode);
        Assert.NotNull(escalation?.Data);
        Assert.Equal(
            PurchaseRequestStatus.AwaitingProcurementApproval,
            escalation.Data.Status);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var budget = await dbContext.BranchMonthlyBudgets
                .SingleAsync(item => item.BranchId == branchId);
            Assert.Equal(0m, budget.UsedAmount);
        }

        await AuthenticateAsync(procurementEmail);
        var queueResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/approval-queue");
        var queue = await queueResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestApprovalQueueResponse>>();
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue?.Data);
        var queueItem = Assert.Single(queue.Data.Items);
        Assert.Equal(PurchaseRequestStatus.AwaitingProcurementApproval, queueItem.Status);

        var approvalResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = queueItem.ConcurrencyStamp,
                Approve = true,
                RejectionReason = (string?)null
            });
        var approval = await approvalResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, approvalResponse.StatusCode);
        Assert.NotNull(approval?.Data);
        Assert.Equal(PurchaseRequestStatus.Approved, approval.Data.Status);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedBudget = await verificationContext.BranchMonthlyBudgets
            .SingleAsync(item => item.BranchId == branchId);
        var persistedDecisions = await verificationContext.PurchaseRequestApprovalDecisions
            .Where(item => item.PurchaseRequestId == request.Id)
            .OrderBy(item => item.DecidedAt)
            .ToListAsync();

        Assert.Equal(request.TotalValue, persistedBudget.UsedAmount);
        Assert.Equal(2, persistedDecisions.Count);
        Assert.Equal(PurchaseRequestDecisionType.Escalated, persistedDecisions[0].Decision);
        Assert.Equal(PurchaseRequestDecisionType.Approved, persistedDecisions[1].Decision);
        Assert.Equal(5m, persistedDecisions[1].OverBudgetAmount);
    }

    [Fact]
    public async Task Manager_can_reject_a_submitted_request_with_a_reason()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("rejection-employee");
        var managerEmail = UniqueEmail("rejection-manager");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);

        var request = await SeedSubmittedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            1m);
        await AuthenticateAsync(managerEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = request.ConcurrencyStamp,
                Approve = false,
                RejectionReason = "The request is outside this month's plan."
            });
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.Equal(PurchaseRequestStatus.Rejected, payload.Data.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var decision = await dbContext.PurchaseRequestApprovalDecisions
            .SingleAsync(item => item.PurchaseRequestId == request.Id);
        Assert.Equal(PurchaseRequestDecisionType.Rejected, decision.Decision);
        Assert.Equal("The request is outside this month's plan.", decision.Reason);
    }

    [Fact]
    public async Task Manager_cannot_decide_their_own_submitted_request()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var managerEmail = UniqueEmail("approval-self");
        var managerUserId = await SeedUserAsync(managerEmail);
        var unitId = await SeedUnitAsync(organizationId, managerUserId);
        var productId = await SeedProductAsync(organizationId, unitId, managerUserId);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);

        var request = await SeedSubmittedRequestAsync(
            managerUserId,
            organizationId,
            branchId,
            productId,
            1m);
        await AuthenticateAsync(managerEmail);

        var queueResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/approval-queue");
        var queue = await queueResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestApprovalQueueResponse>>();

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue?.Data);
        var queueItem = Assert.Single(queue.Data.Items);
        Assert.False(queueItem.CanDecide);

        var decisionResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = request.ConcurrencyStamp,
                Approve = true,
                RejectionReason = (string?)null
            });

        Assert.Equal(HttpStatusCode.Forbidden, decisionResponse.StatusCode);
    }

    [Fact]
    public async Task Manager_approval_queue_is_limited_to_their_assigned_branch()
    {
        var organizationId = await SeedOrganizationAsync();
        var managerBranchId = await SeedBranchAsync(organizationId);
        var otherBranchId = await SeedBranchAsync(organizationId);
        var managerEmail = UniqueEmail("approval-scope-manager");
        var firstEmployeeEmail = UniqueEmail("approval-scope-first-employee");
        var secondEmployeeEmail = UniqueEmail("approval-scope-second-employee");
        var managerUserId = await SeedUserAsync(managerEmail);
        var firstEmployeeUserId = await SeedUserAsync(firstEmployeeEmail);
        var secondEmployeeUserId = await SeedUserAsync(secondEmployeeEmail);
        var firstUnitId = await SeedUnitAsync(organizationId, firstEmployeeUserId);
        var secondUnitId = await SeedUnitAsync(organizationId, secondEmployeeUserId);
        var firstProductId = await SeedProductAsync(organizationId, firstUnitId, firstEmployeeUserId);
        var secondProductId = await SeedProductAsync(organizationId, secondUnitId, secondEmployeeUserId);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            managerBranchId,
            BusinessRole.Manager);
        await SeedMembershipAsync(
            organizationId,
            firstEmployeeUserId,
            managerBranchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            secondEmployeeUserId,
            otherBranchId,
            BusinessRole.Employee);

        var visibleRequest = await SeedSubmittedRequestAsync(
            firstEmployeeUserId,
            organizationId,
            managerBranchId,
            firstProductId,
            1m);
        var hiddenRequest = await SeedSubmittedRequestAsync(
            secondEmployeeUserId,
            organizationId,
            otherBranchId,
            secondProductId,
            1m);
        await AuthenticateAsync(managerEmail);

        var response = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/approval-queue");
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestApprovalQueueResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        var item = Assert.Single(payload.Data.Items);
        Assert.Equal(visibleRequest.Id, item.Id);
        Assert.DoesNotContain(payload.Data.Items, queueItem => queueItem.Id == hiddenRequest.Id);
    }

    [Fact]
    public async Task Manager_rejection_without_a_reason_returns_bad_request()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("rejection-validation-employee");
        var managerEmail = UniqueEmail("rejection-validation-manager");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);
        var request = await SeedSubmittedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            1m);
        await AuthenticateAsync(managerEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = request.ConcurrencyStamp,
                Approve = false,
                RejectionReason = "   "
            });
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload?.Errors);
        Assert.Contains(payload.Errors, error => error.Field == "RejectionReason");

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await dbContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        Assert.Equal(PurchaseRequestStatus.Submitted, persistedRequest.Status);
        Assert.Empty(await dbContext.PurchaseRequestApprovalDecisions
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Manager_decision_with_a_stale_request_version_returns_conflict()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("approval-concurrency-employee");
        var managerEmail = UniqueEmail("approval-concurrency-manager");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var managerUserId = await SeedUserAsync(managerEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            managerUserId,
            branchId,
            BusinessRole.Manager);
        var request = await SeedSubmittedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            1m);
        var period = (DateTime.UtcNow.Year, DateTime.UtcNow.Month);
        await SeedBudgetAsync(branchId, period.Year, period.Month, 100m);
        await AuthenticateAsync(managerEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/decision",
            new
            {
                ConcurrencyStamp = $"{request.ConcurrencyStamp}-stale",
                Approve = true,
                RejectionReason = (string?)null
            });
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<object>>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains("modified concurrently", payload.Message, StringComparison.OrdinalIgnoreCase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await dbContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        var persistedBudget = await dbContext.BranchMonthlyBudgets
            .SingleAsync(item => item.BranchId == branchId);
        Assert.Equal(PurchaseRequestStatus.Submitted, persistedRequest.Status);
        Assert.Equal(0m, persistedBudget.UsedAmount);
        Assert.Empty(await dbContext.PurchaseRequestApprovalDecisions
            .Where(item => item.PurchaseRequestId == request.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Procurement_can_move_an_approved_request_to_ordered_and_delivered()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("fulfillment-employee");
        var procurementEmail = UniqueEmail("fulfillment-procurement");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var procurementUserId = await SeedUserAsync(procurementEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            procurementUserId,
            null,
            BusinessRole.Procurement);

        var request = await SeedApprovedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            2m);
        await AuthenticateAsync(procurementEmail);

        var queueResponse = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/fulfillment-queue");
        var queue = await queueResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestFulfillmentQueueResponse>>();

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue?.Data);
        var queueItem = Assert.Single(queue.Data.Items);
        Assert.Equal(request.Id, queueItem.Id);
        Assert.Equal(PurchaseRequestStatus.Approved, queueItem.Status);

        var orderResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/fulfillment/order",
            new
            {
                ConcurrencyStamp = queueItem.ConcurrencyStamp,
                OrderNumber = "PO-2026-001",
                FulfillmentNote = "Supplier confirmed the order."
            });
        var ordered = await orderResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        Assert.NotNull(ordered?.Data);
        Assert.Equal(PurchaseRequestStatus.Ordered, ordered.Data.Status);
        Assert.Equal("PO-2026-001", ordered.Data.FulfillmentOrderNumber);
        Assert.Equal("Supplier confirmed the order.", ordered.Data.FulfillmentNote);

        var deliverResponse = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/fulfillment/deliver",
            new
            {
                ConcurrencyStamp = ordered.Data.ConcurrencyStamp,
                FulfillmentNote = "Received by the branch warehouse."
            });
        var delivered = await deliverResponse.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();

        Assert.Equal(HttpStatusCode.OK, deliverResponse.StatusCode);
        Assert.NotNull(delivered?.Data);
        Assert.Equal(PurchaseRequestStatus.Delivered, delivered.Data.Status);
        Assert.Equal("PO-2026-001", delivered.Data.FulfillmentOrderNumber);
        Assert.Equal("Received by the branch warehouse.", delivered.Data.FulfillmentNote);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var histories = await dbContext.PurchaseRequestStatusHistories
            .Where(item => item.PurchaseRequestId == request.Id)
            .OrderBy(item => item.ChangedAt)
            .ToListAsync();

        Assert.Equal(
            new[]
            {
                PurchaseRequestStatus.Approved,
                PurchaseRequestStatus.Ordered,
                PurchaseRequestStatus.Delivered
            },
            histories.Select(item => item.ToStatus));
        Assert.Equal(procurementUserId, histories[1].ChangedByUserId);
        Assert.Equal(procurementUserId, histories[2].ChangedByUserId);
    }

    [Fact]
    public async Task Employee_cannot_access_the_procurement_fulfillment_queue()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("fulfillment-forbidden-employee");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await AuthenticateAsync(employeeEmail);

        var response = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/fulfillment-queue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Platform_admin_with_an_active_membership_can_access_the_fulfillment_queue()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("fulfillment-admin-employee");
        var adminEmail = UniqueEmail("fulfillment-admin");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var adminUserId = await SeedUserAsync(adminEmail, UserRole.Admin);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            adminUserId,
            branchId,
            BusinessRole.Employee);
        await SeedApprovedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            1m);
        await AuthenticateAsync(adminEmail);

        var response = await _client.GetAsync(
            $"/api/organizations/{organizationId}/purchase-requests/fulfillment-queue");
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestFulfillmentQueueResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.Single(payload.Data.Items);
        Assert.Equal(PurchaseRequestStatus.Approved, payload.Data.Items[0].Status);
    }

    [Fact]
    public async Task Procurement_fulfillment_with_a_stale_request_version_returns_conflict()
    {
        var organizationId = await SeedOrganizationAsync();
        var branchId = await SeedBranchAsync(organizationId);
        var employeeEmail = UniqueEmail("fulfillment-concurrency-employee");
        var procurementEmail = UniqueEmail("fulfillment-concurrency-procurement");
        var employeeUserId = await SeedUserAsync(employeeEmail);
        var procurementUserId = await SeedUserAsync(procurementEmail);
        var unitId = await SeedUnitAsync(organizationId, employeeUserId);
        var productId = await SeedProductAsync(organizationId, unitId, employeeUserId);
        await SeedMembershipAsync(
            organizationId,
            employeeUserId,
            branchId,
            BusinessRole.Employee);
        await SeedMembershipAsync(
            organizationId,
            procurementUserId,
            null,
            BusinessRole.Procurement);

        var request = await SeedApprovedRequestAsync(
            employeeUserId,
            organizationId,
            branchId,
            productId,
            1m);
        await AuthenticateAsync(procurementEmail);

        var response = await _client.PostAsJsonAsync(
            $"/api/organizations/{organizationId}/purchase-requests/{request.Id}/fulfillment/order",
            new
            {
                ConcurrencyStamp = $"{request.ConcurrencyStamp}-stale",
                OrderNumber = "PO-STALE"
            });
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<object>>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains("modified concurrently", payload.Message, StringComparison.OrdinalIgnoreCase);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRequest = await dbContext.PurchaseRequests
            .SingleAsync(item => item.Id == request.Id);
        Assert.Equal(PurchaseRequestStatus.Approved, persistedRequest.Status);
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

    private async Task<Guid> SeedUserAsync(
        string email,
        UserRole role = UserRole.User)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher<User>();
        var user = User.Create(
            EmailAddress.Create(email),
            DisplayName.Create("Purchase request test user"),
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
            "Purchase request test organization",
            CreateAddress(),
            $"PR{Guid.NewGuid():N}"[..10],
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
            "Purchase request test branch",
            CreateAddress(),
            $"PB{Guid.NewGuid():N}"[..10],
            organizationId);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        return branch.Id;
    }

    private async Task<Guid> SeedUnitAsync(Guid organizationId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var unit = new DomainUnitOfMeasure(
            organizationId,
            "Piece",
            "pc",
            userId);
        dbContext.UnitsOfMeasure.Add(unit);
        await dbContext.SaveChangesAsync();
        return unit.Id;
    }

    private async Task<Guid> SeedProductAsync(Guid organizationId, Guid unitId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var product = new DomainProduct(
            organizationId,
            "Monitor",
            "MON-1",
            unitId,
            10.50m,
            userId);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product.Id;
    }

    private async Task SeedMembershipAsync(
        Guid organizationId,
        Guid userId,
        Guid? branchId,
        BusinessRole role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Memberships.Add(new DomainMembership(
            organizationId,
            userId,
            branchId,
            role));
        await dbContext.SaveChangesAsync();
    }

    private async Task<PurchaseRequest> SeedSubmittedRequestAsync(
        Guid authorUserId,
        Guid organizationId,
        Guid branchId,
        Guid productId,
        decimal quantity)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var request = PurchaseRequest.Create(
            authorUserId,
            organizationId,
            branchId,
            "Approval test request");
        request.AddItem(
            productId,
            "Monitor",
            "MON-1",
            "Piece",
            "pc",
            10.50m,
            quantity);
        request.Submit();
        dbContext.PurchaseRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return request;
    }

    private async Task<PurchaseRequest> SeedApprovedRequestAsync(
        Guid authorUserId,
        Guid organizationId,
        Guid branchId,
        Guid productId,
        decimal quantity)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var request = PurchaseRequest.Create(
            authorUserId,
            organizationId,
            branchId,
            "Fulfillment test request");
        request.AddItem(
            productId,
            "Monitor",
            "MON-1",
            "Piece",
            "pc",
            10.50m,
            quantity);
        request.Submit();
        var previousStatus = request.Status;
        request.Approve();
        dbContext.PurchaseRequests.Add(request);
        dbContext.PurchaseRequestStatusHistories.Add(PurchaseRequestStatusHistory.Create(
            request.Id,
            previousStatus,
            request.Status,
            authorUserId));
        await dbContext.SaveChangesAsync();
        return request;
    }

    private async Task<BranchMonthlyBudget> SeedBudgetAsync(
        Guid branchId,
        int year,
        int month,
        decimal limitAmount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var budget = BranchMonthlyBudget.Create(branchId, year, month, limitAmount);
        dbContext.BranchMonthlyBudgets.Add(budget);
        await dbContext.SaveChangesAsync();
        return budget;
    }

    private static async Task<ApiResponse<PurchaseRequestResponse>> ReadResponseAsync(
        HttpResponseMessage response)
    {
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<PurchaseRequestResponse>>();
        Assert.NotNull(payload);
        return payload;
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
