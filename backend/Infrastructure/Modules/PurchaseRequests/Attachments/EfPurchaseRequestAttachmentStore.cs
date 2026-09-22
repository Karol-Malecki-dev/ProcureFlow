using Application.Modules.PurchaseRequests.Attachments;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>EF adapter for request-scoped attachment metadata and cleanup messages.</summary>
public sealed class EfPurchaseRequestAttachmentStore : IPurchaseRequestAttachmentStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestAttachmentStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseRequest?> GetRequestAsync(
        Guid organizationId,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                request => request.Id == purchaseRequestId
                    && request.OrganizationId == organizationId,
                cancellationToken);

    public async Task<IReadOnlyList<PurchaseRequestAttachmentView>> ListAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => await _dbContext.PurchaseRequestAttachments
            .AsNoTracking()
            .Where(attachment => attachment.PurchaseRequestId == purchaseRequestId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .ThenByDescending(attachment => attachment.Id)
            .Select(attachment => new PurchaseRequestAttachmentView(
                attachment.Id,
                attachment.PurchaseRequestId,
                attachment.UploadedByUserId,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes,
                attachment.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<PurchaseRequestAttachment?> GetAttachmentAsync(
        Guid purchaseRequestId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequestAttachments
            .SingleOrDefaultAsync(
                attachment => attachment.Id == attachmentId
                    && attachment.PurchaseRequestId == purchaseRequestId,
                cancellationToken);

    public async Task<PurchaseRequestAttachmentView> CreateAsync(
        PurchaseRequestAttachment attachment,
        int maxCount,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var isPostgreSql = string.Equals(
            _dbContext.Database.ProviderName,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal);
        await using var transaction = isPostgreSql
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (isPostgreSql)
        {
            await _dbContext.PurchaseRequests
                .FromSqlInterpolated(
                    $"SELECT * FROM \"PurchaseRequests\" WHERE \"Id\" = {attachment.PurchaseRequestId} FOR UPDATE")
                .Select(request => request.Id)
                .SingleAsync(cancellationToken);
        }

        var existingCount = await _dbContext.PurchaseRequestAttachments
            .CountAsync(
                item => item.PurchaseRequestId == attachment.PurchaseRequestId,
                cancellationToken);
        if (maxCount > 0 && existingCount >= maxCount)
        {
            throw new PurchaseRequestAttachmentQuotaExceededException(
                $"A purchase request cannot contain more than {maxCount} attachments.");
        }

        var existingBytes = await _dbContext.PurchaseRequestAttachments
            .Where(item => item.PurchaseRequestId == attachment.PurchaseRequestId)
            .Select(item => (long?)item.SizeBytes)
            .SumAsync(cancellationToken) ?? 0L;
        if (maxBytes > 0 && existingBytes + attachment.SizeBytes > maxBytes)
        {
            throw new PurchaseRequestAttachmentQuotaExceededException(
                $"Purchase request attachment storage cannot exceed {maxBytes} bytes.");
        }

        _dbContext.PurchaseRequestAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return ToView(attachment);
    }

    public async Task DeleteAsync(
        PurchaseRequestAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        _dbContext.PurchaseRequestAttachments.Remove(attachment);
        _dbContext.PurchaseRequestAttachmentCleanupMessages.Add(
            PurchaseRequestAttachmentCleanupMessage.Create(attachment.StoredFileName));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void ClearChangeTracker()
        => _dbContext.ChangeTracker.Clear();

    private static PurchaseRequestAttachmentView ToView(
        PurchaseRequestAttachment attachment)
        => new(
            attachment.Id,
            attachment.PurchaseRequestId,
            attachment.UploadedByUserId,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt);
}
