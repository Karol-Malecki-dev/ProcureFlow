using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Shared.Settings;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

internal static class PurchaseRequestAttachmentValidation
{
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = "application/pdf",
                [".png"] = "image/png",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                [".txt"] = "text/plain",
            });

    public static string? NormalizeAndValidate(
        string? originalFileName,
        string? contentType,
        long sizeBytes,
        AttachmentSettings settings)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return "A file name is required.";
        }

        if (sizeBytes <= 0 || sizeBytes > settings.MaxFileSizeBytes)
        {
            return $"Attachment size must be between 1 byte and {settings.MaxFileSizeBytes / (1024 * 1024)} MB.";
        }

        var normalizedFileName = NormalizeFileName(originalFileName);
        if (normalizedFileName.Length > 255)
        {
            return "Attachment file name cannot exceed 255 characters.";
        }

        var extension = Path.GetExtension(normalizedFileName).ToLowerInvariant();
        if (!AllowedContentTypes.TryGetValue(extension, out var expectedContentType)
            || !string.Equals(expectedContentType, contentType?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "Attachment format or content type is not allowed.";
        }

        return null;
    }

    public static string NormalizeFileName(string fileName)
        => Path.GetFileName(fileName.Trim().Replace('\\', '/'));
}
