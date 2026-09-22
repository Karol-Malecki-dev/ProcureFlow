using Application.Modules.ProjectTasks.Attachments;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.CreatePurchaseRequestAttachment;
using Domain.Entities;
using Domain.Models.Organizations.Enums;
using Infrastructure.Modules.PurchaseRequests.Attachments;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Settings;

namespace UnitTests.Modules.PurchaseRequests.Attachments;

public sealed class CreatePurchaseRequestAttachmentHandlerTests
{
    private readonly Mock<IPurchaseRequestMembershipReader> _membershipReader = new();
    private readonly Mock<IPurchaseRequestAttachmentStore> _store = new();
    private readonly Mock<IProjectTaskAttachmentStorage> _storage = new();
    private readonly Mock<IProjectTaskAttachmentMalwareScanner> _malwareScanner = new();

    [Fact]
    public async Task Clean_attachment_is_stored_after_content_and_malware_validation()
    {
        var context = CreateContext();
        var content = new MemoryStream("safe text"u8.ToArray());
        var command = CreateCommand(context, content, "quote.txt", "text/plain");
        SetupRequestScope(context);
        _malwareScanner
            .Setup(scanner => scanner.ScanAsync(
                It.IsAny<Stream>(),
                "quote.txt",
                "text/plain",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectTaskAttachmentScanStatus.Clean);
        _storage
            .Setup(storage => storage.SaveAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _store
            .Setup(store => store.CreateAsync(
                It.IsAny<PurchaseRequestAttachment>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateView(context, command));

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        _malwareScanner.Verify(scanner => scanner.ScanAsync(
            It.IsAny<Stream>(),
            "quote.txt",
            "text/plain",
            It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(storage => storage.SaveAsync(
            It.IsAny<Stream>(),
            It.Is<string>(name => name.EndsWith(".txt", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.CreateAsync(
            It.IsAny<PurchaseRequestAttachment>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Threat_detected_is_rejected_before_binary_storage()
    {
        var context = CreateContext();
        var command = CreateCommand(
            context,
            new MemoryStream("safe text"u8.ToArray()),
            "quote.txt",
            "text/plain");
        SetupRequestScope(context);
        _malwareScanner
            .Setup(scanner => scanner.ScanAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectTaskAttachmentScanStatus.ThreatDetected);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.ValidationError, result.Status);
        _storage.Verify(storage => storage.SaveAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.CreateAsync(
            It.IsAny<PurchaseRequestAttachment>(),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unavailable_malware_scanner_fails_closed_with_conflict()
    {
        var context = CreateContext();
        var command = CreateCommand(
            context,
            new MemoryStream("safe text"u8.ToArray()),
            "quote.txt",
            "text/plain");
        SetupRequestScope(context);
        _malwareScanner
            .Setup(scanner => scanner.ScanAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectTaskAttachmentScanStatus.Unavailable);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Conflict, result.Status);
        _storage.Verify(storage => storage.SaveAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Content_that_does_not_match_the_declared_extension_is_rejected()
    {
        var context = CreateContext();
        var command = CreateCommand(
            context,
            new MemoryStream("safe text"u8.ToArray()),
            "quote.pdf",
            "application/pdf");
        SetupRequestScope(context);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.ValidationError, result.Status);
        _malwareScanner.Verify(scanner => scanner.ScanAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Metadata_failure_compensates_the_saved_binary()
    {
        var context = CreateContext();
        var command = CreateCommand(
            context,
            new MemoryStream("safe text"u8.ToArray()),
            "quote.txt",
            "text/plain");
        SetupRequestScope(context);
        _malwareScanner
            .Setup(scanner => scanner.ScanAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProjectTaskAttachmentScanStatus.Clean);
        _storage
            .Setup(storage => storage.SaveAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _store
            .Setup(store => store.CreateAsync(
                It.IsAny<PurchaseRequestAttachment>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("metadata write failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler().HandleAsync(command));

        _storage.Verify(storage => storage.DeleteAsync(
            It.Is<string>(name => name.EndsWith(".txt", StringComparison.Ordinal)),
            It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(store => store.ClearChangeTracker(), Times.Once);
    }

    private CreatePurchaseRequestAttachmentHandler CreateHandler()
        => new(
            _membershipReader.Object,
            _store.Object,
            _storage.Object,
            _malwareScanner.Object,
            Options.Create(new AttachmentSettings
            {
                MaxFileSizeBytes = 10 * 1024 * 1024,
                MaxCountPerTask = 20,
                MaxBytesPerTask = 100 * 1024 * 1024,
                RequireMalwareScan = true
            }),
            NullLogger<CreatePurchaseRequestAttachmentHandler>.Instance);

    private void SetupRequestScope(AttachmentContext context)
    {
        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                context.UserId,
                context.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseRequestMembership(
                Guid.NewGuid(),
                context.UserId,
                context.OrganizationId,
                context.BranchId,
                BusinessRole.Employee,
                true,
                true,
                true));
        _store
            .Setup(store => store.GetRequestAsync(
                context.OrganizationId,
                context.Request.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(context.Request);
    }

    private static CreatePurchaseRequestAttachmentCommand CreateCommand(
        AttachmentContext context,
        Stream content,
        string fileName,
        string contentType)
        => new(
            context.UserId,
            context.OrganizationId,
            context.Request.Id,
            fileName,
            contentType,
            content.Length,
            content);

    private static PurchaseRequestAttachmentView CreateView(
        AttachmentContext context,
        CreatePurchaseRequestAttachmentCommand command)
        => new(
            Guid.NewGuid(),
            context.Request.Id,
            command.UserId,
            command.OriginalFileName,
            command.ContentType,
            command.SizeBytes,
            DateTime.UtcNow);

    private static AttachmentContext CreateContext()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        return new AttachmentContext(
            userId,
            organizationId,
            branchId,
            PurchaseRequest.Create(userId, organizationId, branchId));
    }

    private sealed record AttachmentContext(
        Guid UserId,
        Guid OrganizationId,
        Guid BranchId,
        PurchaseRequest Request);
}
