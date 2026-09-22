using Application.Modules.ProjectTasks.Attachments;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.CreatePurchaseRequestAttachment;
using Domain.Entities;
using Infrastructure.Modules.ProjectTasks.CreateProjectTaskAttachment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Settings;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>
/// Validates, scans and persists one purchase-request attachment.
/// </summary>
public sealed class CreatePurchaseRequestAttachmentHandler
    : ICreatePurchaseRequestAttachmentHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestAttachmentStore _store;
    private readonly IProjectTaskAttachmentStorage _storage;
    private readonly IProjectTaskAttachmentMalwareScanner _malwareScanner;
    private readonly AttachmentSettings _settings;
    private readonly ILogger<CreatePurchaseRequestAttachmentHandler> _logger;

    public CreatePurchaseRequestAttachmentHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestAttachmentStore store,
        IProjectTaskAttachmentStorage storage,
        IProjectTaskAttachmentMalwareScanner malwareScanner,
        IOptions<AttachmentSettings> settings,
        ILogger<CreatePurchaseRequestAttachmentHandler> logger)
    {
        _membershipReader = membershipReader;
        _store = store;
        _storage = storage;
        _malwareScanner = malwareScanner;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestAttachmentView>> HandleAsync(
        CreatePurchaseRequestAttachmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Attachment identifiers are required.");
        }

        if (command.Content is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Attachment content is required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        var request = await _store.GetRequestAsync(
            command.OrganizationId,
            command.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request not found.");
        }

        if (!PurchaseRequestAttachmentAccess.CanUpload(
                membership,
                request,
                command.UserId))
        {
            return Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "Only the request author can upload attachments while the request is a draft.");
        }

        var originalFileName = PurchaseRequestAttachmentValidation.NormalizeFileName(
            command.OriginalFileName);
        var validationError = PurchaseRequestAttachmentValidation.NormalizeAndValidate(
            originalFileName,
            command.ContentType,
            command.SizeBytes,
            _settings);
        if (validationError is not null)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                validationError);
        }

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var inspectionError = await ProjectTaskAttachmentContentInspector.InspectAsync(
            command.Content,
            extension,
            command.SizeBytes,
            cancellationToken);
        if (inspectionError is not null)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                inspectionError);
        }

        command.Content.Position = 0;
        if (_settings.RequireMalwareScan)
        {
            var scanStatus = await _malwareScanner.ScanAsync(
                command.Content,
                originalFileName,
                command.ContentType,
                cancellationToken);
            if (scanStatus == ProjectTaskAttachmentScanStatus.ThreatDetected)
            {
                return Failure(
                    PurchaseRequestOperationStatus.ValidationError,
                    "Attachment content was rejected by malware scanning.");
            }

            if (scanStatus != ProjectTaskAttachmentScanStatus.Clean)
            {
                return Failure(
                    PurchaseRequestOperationStatus.Conflict,
                    "Attachment malware scanning is temporarily unavailable.");
            }
        }

        command.Content.Position = 0;
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var binarySaved = false;

        try
        {
            await _storage.SaveAsync(
                command.Content,
                storedFileName,
                cancellationToken);
            binarySaved = true;

            var attachment = PurchaseRequestAttachment.Create(
                request.Id,
                command.UserId,
                originalFileName,
                storedFileName,
                command.ContentType.Trim(),
                command.SizeBytes);
            var view = await _store.CreateAsync(
                attachment,
                _settings.MaxCountPerTask,
                _settings.MaxBytesPerTask,
                cancellationToken);

            return PurchaseRequestOperationResult<PurchaseRequestAttachmentView>.Success(
                view,
                "Purchase request attachment created.");
        }
        catch (PurchaseRequestAttachmentQuotaExceededException exception)
        {
            await CleanupBinaryAsync(storedFileName, binarySaved);
            _store.ClearChangeTracker();
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                exception.Message);
        }
        catch (Exception)
        {
            await CleanupBinaryAsync(storedFileName, binarySaved);
            _store.ClearChangeTracker();
            throw;
        }
    }

    private async Task CleanupBinaryAsync(
        string storedFileName,
        bool binarySaved)
    {
        if (!binarySaved)
        {
            return;
        }

        try
        {
            await _storage.DeleteAsync(storedFileName);
        }
        catch (Exception cleanupException)
        {
            _logger.LogError(
                cleanupException,
                "Failed to clean up purchase-request attachment binary {StoredFileName}.",
                storedFileName);
        }
    }

    private static PurchaseRequestOperationResult<PurchaseRequestAttachmentView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestAttachmentView>.Failure(
            status,
            message);
}
