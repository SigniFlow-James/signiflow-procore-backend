using System.Threading.Channels;
using Microsoft.Extensions.Hosting;

namespace Signiflow.APIClasses;

public interface ISigniflowWebhookQueue
{
    ValueTask EnqueueAsync(SigniflowWebhookEvent evt);
    IAsyncEnumerable<SigniflowWebhookEvent> DequeueAsync(CancellationToken ct);
}

public class SigniflowWebhookQueue : ISigniflowWebhookQueue
{
    private readonly Channel<SigniflowWebhookEvent> _channel =
        Channel.CreateUnbounded<SigniflowWebhookEvent>();

    public ValueTask EnqueueAsync(SigniflowWebhookEvent evt)
        => _channel.Writer.WriteAsync(evt);

    public IAsyncEnumerable<SigniflowWebhookEvent> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}



public class SigniflowWebhookWorker : BackgroundService
{
    private readonly ISigniflowWebhookQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthService _authService;

    public SigniflowWebhookWorker(
        ISigniflowWebhookQueue queue,
        IServiceProvider serviceProvider,
        AuthService authService)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _authService = authService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var controller = scope.ServiceProvider
                    .GetRequiredService<SigniflowWebhookController>();

                // Event routing logic
                if (evt.EventType == "DocumentCompleted" ||
                    evt.Status == "Completed")
                {
                    // Refresh login if needed
                    await _authService.CheckRefreshAuthAsync();
                    // Fire event
                    await controller.ProcessDocumentCompletedAsync(evt);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Background SigniFlow processing failed: {ex}");
                // TODO: retry / dead-letter storage
            }
        }
    }
}

