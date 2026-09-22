using Application.Modules.PurchaseRequests.Attachments;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>Polls the durable request-attachment cleanup queue.</summary>
public sealed class PurchaseRequestAttachmentCleanupWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    public const string WorkerName = "purchase-request-attachment-cleanup";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PurchaseRequestAttachmentCleanupWorker> _logger;
    private readonly BackgroundWorkerHealthState _healthState;

    public PurchaseRequestAttachmentCleanupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PurchaseRequestAttachmentCleanupWorker> logger,
        BackgroundWorkerHealthState healthState)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _healthState = healthState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IPurchaseRequestAttachmentCleanupProcessor>();
                await processor.ProcessPendingMessagesAsync(stoppingToken);
                _healthState.ReportSuccess(WorkerName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _healthState.ReportFailure(WorkerName, exception);
                _logger.LogError(
                    exception,
                    "Purchase-request attachment cleanup worker iteration failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
