using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Application.Modules.Attachments;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Settings;

namespace Infrastructure.Modules.Attachments;

/// <summary>
/// Registers storage and malware-scanning adapters shared by attachment features.
/// </summary>
public static class AttachmentsModule
{
    public static IServiceCollection AddAttachmentsModule(this IServiceCollection services)
    {
        services.AddSingleton<LocalAttachmentStorage>();
        services.AddSingleton<S3AttachmentStorage>();
        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var settings = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AttachmentSettings>>()
                .Value;
            var config = new AmazonS3Config
            {
                ForcePathStyle = settings.S3ForcePathStyle,
                MaxErrorRetry = 3
            };

            if (!string.IsNullOrWhiteSpace(settings.S3ServiceUrl))
            {
                config.ServiceURL = settings.S3ServiceUrl;
            }
            else
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.S3Region);
            }

            if (!string.IsNullOrWhiteSpace(settings.S3AccessKey))
            {
                return new AmazonS3Client(
                    new BasicAWSCredentials(settings.S3AccessKey, settings.S3SecretKey),
                    config);
            }

            return new AmazonS3Client(config);
        });
        services.AddSingleton<IAttachmentStorage>(serviceProvider =>
        {
            var settings = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AttachmentSettings>>()
                .Value;
            return string.Equals(settings.StorageProvider, "S3", StringComparison.OrdinalIgnoreCase)
                ? serviceProvider.GetRequiredService<S3AttachmentStorage>()
                : serviceProvider.GetRequiredService<LocalAttachmentStorage>();
        });
        services.AddSingleton<IAttachmentMalwareScanner>(serviceProvider =>
        {
            var settings = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<AttachmentSettings>>()
                .Value;
            return string.IsNullOrWhiteSpace(settings.MalwareScannerHost)
                ? new UnavailableAttachmentMalwareScanner()
                : serviceProvider.GetRequiredService<ClamAvAttachmentMalwareScanner>();
        });
            services.AddSingleton<ClamAvAttachmentMalwareScanner>();

        return services;
    }
}