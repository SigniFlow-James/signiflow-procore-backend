// ============================================================
// FILE: Endpoints/WebhookEndpoints.cs
// ============================================================

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signiflow.APIClasses;

public static class WebhookEndpoints
{
    // In-memory cache to track processed webhooks
    private static readonly ConcurrentDictionary<string, DateTime> _processedWebhooks = new();
    private static readonly TimeSpan _deduplicationWindow = TimeSpan.FromMinutes(5);

    public static void MapWebhookEndpoints(this WebApplication app)
    {
        // Background task to clean up old entries
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                var cutoff = DateTime.UtcNow - _deduplicationWindow;
                var expiredKeys = _processedWebhooks
                    .Where(kvp => kvp.Value < cutoff)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in expiredKeys)
                {
                    _processedWebhooks.TryRemove(key, out _);
                }
            }
        });

        app.MapPost("/api/webhooks/signiflow", async (
            HttpRequest request,
            ISigniflowWebhookQueue queue) =>
        {
            try
            {
                // Read the form data
                var form = await request.ReadFormAsync();

                // Log all form keys to see what we're getting
                Console.WriteLine($"Received form data with keys: {string.Join(", ", form.Keys)}");

                // Create deduplication key based on event type, doc ID, and status
                var eventType = form["ET"].ToString();
                var docId = form["DI"].ToString();
                var status = form["SFS"].ToString();
                var deduplicationKey = $"{eventType}:{docId}:{status}";

                // Check if we've already processed this webhook recently
                if (_processedWebhooks.TryGetValue(deduplicationKey, out var lastProcessed))
                {
                    var timeSinceLastProcess = DateTime.UtcNow - lastProcessed;
                    if (timeSinceLastProcess < _deduplicationWindow)
                    {
                        Console.WriteLine($"Duplicate webhook detected: {deduplicationKey} (last processed {timeSinceLastProcess.TotalSeconds:F1}s ago)");
                        return Results.Ok(new { status = "duplicate_ignored" });
                    }
                }

                // Mark this webhook as processed
                _processedWebhooks[deduplicationKey] = DateTime.UtcNow;

                // Try to parse the webhook event from form data
                var webhookEvent = new SigniflowWebhookEvent
                {
                    EventType = eventType,
                    DocId = docId,
                    DocumentName = form["FN"].ToString(),
                    Status = status,
                    CompletedDate = DateTime.TryParse(form["ED"], out var date) ? date : DateTime.MinValue,
                    PortfolioId = int.TryParse(form["PortfolioID"], out var portfolioId) ? portfolioId : 0
                };

                // Parse the AdditionalData JSON
                var additionalDataJson = form["ADF"].ToString();
                if (!string.IsNullOrWhiteSpace(additionalDataJson))
                {
                    webhookEvent.Metadata = JsonSerializer.Deserialize<Procore.APIClasses.ProcoreContext>(additionalDataJson);
                }

                Console.WriteLine($"Received SigniFlow webhook: {webhookEvent.EventType}");
                Console.WriteLine($"Full form data: {JsonSerializer.Serialize(form.ToDictionary(k => k.Key, k => k.Value.ToString()))}");
                await queue.EnqueueAsync(webhookEvent);
                Console.WriteLine("Added to queue");
                return Results.Ok(new { status = "received" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving SigniFlow webhook: {ex}");
                return Results.StatusCode(500);
            }
        });
    }
}

// ============================================================
// END FILE: Endpoints/WebhookEndpoints.cs
// ============================================================