using Application.Modules.ProjectTasks.Attachments;
using Application.Modules.PurchaseRequests.Attachments;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>Processes durable cleanup messages for deleted request attachments.</summary>
public sealed class PurchaseRequestAttachmentCleanupProcessor
    : IPurchaseRequestAttachmentCleanupProcessor
{
    private const int MaxAttempts = 3;
    private const int BatchSize = 20;

    private readonly ApplicationDbContext _dbContext;
    private readonly IProjectTaskAttachmentStorage _storage;
    private readonly ILogger<PurchaseRequestAttachmentCleanupProcessor> _logger;

    public PurchaseRequestAttachmentCleanupProcessor(
        ApplicationDbContext dbContext,
        IProjectTaskAttachmentStorage storage,
        ILogger<PurchaseRequestAttachmentCleanupProcessor> logger)
    {
        _dbContext = dbContext;
        _storage = storage;
        _logger = logger;
    }

    public async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var messages = await _dbContext.PurchaseRequestAttachmentCleanupMessages
            .Where(message => message.ProcessedAt == null
                && message.AttemptCount < MaxAttempts
                && message.NextAttemptAt <= now)
            .OrderBy(message => message.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await _storage.DeleteAsync(
                    message.StoredFileName,
                    cancellationToken);
                message.MarkProcessed(DateTime.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.MarkFailed(
                    exception.Message,
                    DateTime.UtcNow.AddMinutes(message.AttemptCount + 1));
                _logger.LogWarning(
                    exception,
                    "Purchase request attachment cleanup failed for message {CleanupMessageId}.",
                    message.Id);
            }
        }

        if (messages.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
