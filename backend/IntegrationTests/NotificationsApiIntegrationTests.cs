using Application.DTOs.Auth;
using Application.DTOs.Notification;
using Application.Interfaces;
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

namespace IntegrationTests;

public sealed class NotificationsApiIntegrationTests
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public NotificationsApiIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetNotifications_Returns_unauthorized_when_token_is_missing()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_can_page_filter_and_mark_only_their_own_notifications_as_read()
    {
        var firstUserId = await SeedUserAsync("notifications.first@example.com", "password123", "First User");
        var secondUserId = await SeedUserAsync("notifications.second@example.com", "password123", "Second User");
        var firstUnreadId = await SeedNotificationAsync(firstUserId, "First unread", readAt: null);
        await SeedNotificationAsync(firstUserId, "First read", readAt: DateTime.UtcNow);
        await SeedNotificationAsync(firstUserId, "Second unread", readAt: null);
        await SeedNotificationAsync(secondUserId, "Other user unread", readAt: null);

        await AuthenticateAsync("notifications.first@example.com", "password123");

        var pageResponse = await _client.GetAsync("/api/notifications?pageNumber=1&pageSize=1");
        pageResponse.EnsureSuccessStatusCode();
        var page = await pageResponse.Content.ReadFromJsonAsync<ApiResponse<NotificationPageDto>>();

        Assert.NotNull(page?.Data);
        Assert.Single(page.Data.Items);
        Assert.Equal(3, page.Data.TotalCount);
        Assert.Equal(2, page.Data.UnreadCount);

        var unreadResponse = await _client.GetAsync("/api/notifications?unreadOnly=true");
        unreadResponse.EnsureSuccessStatusCode();
        var unreadPage = await unreadResponse.Content.ReadFromJsonAsync<ApiResponse<NotificationPageDto>>();
        Assert.NotNull(unreadPage?.Data);
        Assert.Equal(2, unreadPage.Data.TotalCount);
        Assert.All(unreadPage.Data.Items, notification => Assert.False(notification.IsRead));

        var markResponse = await _client.PatchAsync($"/api/notifications/{firstUnreadId}/read", null);
        markResponse.EnsureSuccessStatusCode();
        var marked = await markResponse.Content.ReadFromJsonAsync<ApiResponse<NotificationDto>>();
        Assert.True(marked?.Data?.IsRead);

        var unreadCountResponse = await _client.GetAsync("/api/notifications/unread-count");
        unreadCountResponse.EnsureSuccessStatusCode();
        var unreadCount = await unreadCountResponse.Content.ReadFromJsonAsync<ApiResponse<int>>();
        Assert.Equal(1, unreadCount?.Data);

        var markAllResponse = await _client.PatchAsync("/api/notifications/read-all", null);
        markAllResponse.EnsureSuccessStatusCode();
        var markedCount = await markAllResponse.Content.ReadFromJsonAsync<ApiResponse<int>>();
        Assert.Equal(1, markedCount?.Data);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(0, await dbContext.Notifications.CountAsync(notification => notification.UserId == firstUserId && notification.ReadAt == null));
        Assert.Equal(1, await dbContext.Notifications.CountAsync(notification => notification.UserId == secondUserId && notification.ReadAt == null));
    }

    [Fact]
    public async Task User_cannot_mark_another_users_notification_as_read()
    {
        var firstUserId = await SeedUserAsync("notifications.owner@example.com", "password123", "Owner User");
        await SeedUserAsync("notifications.other@example.com", "password123", "Other User");
        var notificationId = await SeedNotificationAsync(firstUserId, "Private notification", readAt: null);

        await AuthenticateAsync("notifications.other@example.com", "password123");

        var response = await _client.PatchAsync($"/api/notifications/{notificationId}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = await dbContext.Notifications.FindAsync(notificationId);
        Assert.NotNull(notification);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task Email_preference_controls_whether_new_notifications_are_queued_for_delivery()
    {
        var firstMemberId = await SeedUserAsync("notifications.preference-first@example.com", "password123", "First Member");
        var secondMemberId = await SeedUserAsync("notifications.preference-second@example.com", "password123", "Second Member");

        await AuthenticateAsync("notifications.preference-first@example.com", "password123");
        var defaultPreferenceResponse = await _client.GetAsync("/api/notifications/email-preference");
        defaultPreferenceResponse.EnsureSuccessStatusCode();
        var defaultPreference = await defaultPreferenceResponse.Content.ReadFromJsonAsync<ApiResponse<NotificationEmailPreferenceDto>>();
        Assert.True(defaultPreference?.Data?.IsEmailEnabled);

        var updatePreferenceResponse = await _client.PatchAsJsonAsync(
            "/api/notifications/email-preference",
            new { IsEmailEnabled = false });
        updatePreferenceResponse.EnsureSuccessStatusCode();

        await using (var notificationScope = _factory.Services.CreateAsyncScope())
        {
            var writer = notificationScope.ServiceProvider.GetRequiredService<INotificationWriter>();
            await writer.CreateAsync(firstMemberId, NotificationType.System, "Disabled email", "Email is disabled.");
            await writer.CreateAsync(secondMemberId, NotificationType.System, "Enabled email", "Email is enabled.");
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await dbContext.Notifications.CountAsync());
        Assert.DoesNotContain(await dbContext.NotificationEmailOutboxMessages.ToListAsync(), message => message.UserId == firstMemberId);
        Assert.Contains(await dbContext.NotificationEmailOutboxMessages.ToListAsync(), message => message.UserId == secondMemberId);
    }

    private async Task AuthenticateAsync(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthTokenResponse>>();
        Assert.NotNull(login?.Data);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Data.AccessToken);
    }

    private async Task<Guid> SeedUserAsync(string email, string password, string displayName)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher<User>();
        var user = User.Create(
            EmailAddress.Create(email),
            DisplayName.Create(displayName),
            UserRole.User,
            isActive: true,
            isEmailConfirmed: true);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password));
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Guid> SeedNotificationAsync(Guid userId, string title, DateTime? readAt)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.System,
            Title = title,
            Message = title,
            CreatedAt = DateTime.UtcNow,
            ReadAt = readAt
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification.Id;
    }
}