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

    private async Task<Guid> SeedUserAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher<User>();
        var user = User.Create(
            EmailAddress.Create(email),
            DisplayName.Create("Purchase request test user"),
            UserRole.User,
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
        Guid branchId,
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
