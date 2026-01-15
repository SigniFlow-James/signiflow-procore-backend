// ============================================================
// FILE: Endpoints/WebhookEndpoints.cs
// ============================================================

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Signiflow.APIClasses;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
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
                
                // Try to parse the webhook event from form data
                // You'll need to adjust these property names based on what Signiflow actually sends
                var webhookEvent = new SigniflowWebhookEvent
                {
                    EventType = form["EventType"].ToString(),
                    DocId = form["DocId"].ToString(),
                    DocumentName = form["DocumentName"].ToString(),
                    Status = form["Status"].ToString(),
                    CompletedDate = DateTime.TryParse(form["CompletedDate"], out var date) ? date : DateTime.MinValue,
                    AdditionalData = form["AdditionalData"].ToString()
                    // Add other properties as needed based on the actual form keys
                };

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