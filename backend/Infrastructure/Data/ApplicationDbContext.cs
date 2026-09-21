using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Entities.JWT;
using Domain.Models.Catalog;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Główny DbContext aplikacji
/// Definiuje wszystkie DbSets (tabele) w bazie danych
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<AccountSecurityEvent> AccountSecurityEvents => Set<AccountSecurityEvent>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<EmailConfirmationToken> EmailConfirmationTokens => Set<EmailConfirmationToken>();

    public DbSet<EmailTwoFactorChallenge> EmailTwoFactorChallenges => Set<EmailTwoFactorChallenge>();
    public DbSet<AuthenticatorLoginChallenge> AuthenticatorLoginChallenges => Set<AuthenticatorLoginChallenge>();
    public DbSet<AuthenticatorRecoveryCode> AuthenticatorRecoveryCodes => Set<AuthenticatorRecoveryCode>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationEmailPreference> NotificationEmailPreferences => Set<NotificationEmailPreference>();
    public DbSet<NotificationEmailOutboxMessage> NotificationEmailOutboxMessages => Set<NotificationEmailOutboxMessage>();

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectActivity> ProjectActivities => Set<ProjectActivity>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ProjectTaskComment> ProjectTaskComments => Set<ProjectTaskComment>();
    public DbSet<ProjectTaskAttachment> ProjectTaskAttachments => Set<ProjectTaskAttachment>();
    public DbSet<ProjectTaskAttachmentCleanupMessage> ProjectTaskAttachmentCleanupMessages => Set<ProjectTaskAttachmentCleanupMessage>();
    public DbSet<ProjectTaskLabel> ProjectTaskLabels => Set<ProjectTaskLabel>();
    public DbSet<ProjectTaskDeadlineReminder> ProjectTaskDeadlineReminders => Set<ProjectTaskDeadlineReminder>();
    public DbSet<ProjectInvitation> ProjectInvitations => Set<ProjectInvitation>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();


    // That Project
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();

    /// <summary>
    /// Konfiguracja modeli i relacji między encjami
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
