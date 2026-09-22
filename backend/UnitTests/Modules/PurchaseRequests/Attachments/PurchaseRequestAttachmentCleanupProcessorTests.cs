using Application.Modules.ProjectTasks.Attachments;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Modules.PurchaseRequests.Attachments;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnitTests.TestHelpers;

namespace UnitTests.Modules.PurchaseRequests.Attachments;

public sealed class PurchaseRequestAttachmentCleanupProcessorTests
{
    [Fact]
    public async Task ProcessPendingMessages_marks_message_processed_after_storage_delete()
    {
        var options = UnitTestHelper.CreateInMemoryDatabaseOptions($"purchase-attachment-cleanup-success-{Guid.NewGuid():N}");
        await using var dbContext = new ApplicationDbContext(options);
        var message = PurchaseRequestAttachmentCleanupMessage.Create($"{Guid.NewGuid():N}.txt");
        dbContext.PurchaseRequestAttachmentCleanupMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var storage = new Mock<IProjectTaskAttachmentStorage>();
        var processor = CreateProcessor(dbContext, storage);

        await processor.ProcessPendingMessagesAsync();

        storage.Verify(
            service => service.DeleteAsync(message.StoredFileName, It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.LastError);
        Assert.Equal(0, message.AttemptCount);
    }

    [Fact]
    public async Task ProcessPendingMessages_records_truncated_retry_state_when_storage_delete_fails()
    {
        var options = UnitTestHelper.CreateInMemoryDatabaseOptions($"purchase-attachment-cleanup-retry-{Guid.NewGuid():N}");
        await using var dbContext = new ApplicationDbContext(options);
        var message = PurchaseRequestAttachmentCleanupMessage.Create($"{Guid.NewGuid():N}.txt");
        dbContext.PurchaseRequestAttachmentCleanupMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var storage = new Mock<IProjectTaskAttachmentStorage>();
        storage
            .Setup(service => service.DeleteAsync(
                message.StoredFileName,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException(new string('x', 2_100)));
        var processor = CreateProcessor(dbContext, storage);

        await processor.ProcessPendingMessagesAsync();

        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.AttemptCount);
        Assert.NotNull(message.LastError);
        Assert.Equal(2_000, message.LastError.Length);
        Assert.True(message.NextAttemptAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessPendingMessages_ignores_messages_that_reached_attempt_limit()
    {
        var options = UnitTestHelper.CreateInMemoryDatabaseOptions($"purchase-attachment-cleanup-limit-{Guid.NewGuid():N}");
        await using var dbContext = new ApplicationDbContext(options);
        var message = PurchaseRequestAttachmentCleanupMessage.Create(
            $"{Guid.NewGuid():N}.txt",
            DateTime.UtcNow.AddMinutes(-10));
        message.MarkFailed("first", DateTime.UtcNow.AddMinutes(-3));
        message.MarkFailed("second", DateTime.UtcNow.AddMinutes(-2));
        message.MarkFailed("third", DateTime.UtcNow.AddMinutes(-1));
        dbContext.PurchaseRequestAttachmentCleanupMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var storage = new Mock<IProjectTaskAttachmentStorage>();
        var processor = CreateProcessor(dbContext, storage);

        await processor.ProcessPendingMessagesAsync();

        storage.Verify(
            service => service.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(3, message.AttemptCount);
        Assert.Equal("third", message.LastError);
        Assert.Null(message.ProcessedAt);
    }

    private static PurchaseRequestAttachmentCleanupProcessor CreateProcessor(
        ApplicationDbContext dbContext,
        Mock<IProjectTaskAttachmentStorage> storage)
        => new(
            dbContext,
            storage.Object,
            NullLogger<PurchaseRequestAttachmentCleanupProcessor>.Instance);
}
