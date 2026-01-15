// ============================================================
// FILE: Endpoints/WebhookEndpoints.cs
// ============================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signiflow.APIClasses;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/signiflow", async (
            [FromBody] SigniflowWebhookEvent webhookEvent,
            ISigniflowWebhookQueue queue) =>
        {
            try
            {
                Console.WriteLine($"Received SigniFlow webhook: {webhookEvent.EventType}");
                await queue.EnqueueAsync(webhookEvent);
                return Results.Ok(new { status = "received" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving SigniFlow webhook {ex}");
                return Results.StatusCode(500);
            }
        });
    }
}

// ============================================================
// END FILE: Endpoints/WebhookEndpoints.cs
// ============================================================