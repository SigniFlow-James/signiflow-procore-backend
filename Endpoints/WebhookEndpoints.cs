using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Procore.APIClasses;
using Signiflow.APIClasses;

[ApiController]
[Route("api/webhooks")]
public class SigniflowWebhookController : ControllerBase
{
    private readonly ProcoreService _procoreService;

    public SigniflowWebhookController(ProcoreService procoreService)
    {
        _procoreService = procoreService;
    }

    [HttpPost("signiflow")]
    public async Task<IActionResult> HandleSigniflowEvent([FromBody] SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            Console.WriteLine($"Received SigniFlow webhook: {webhookEvent.EventType}");
            Console.WriteLine($"Full payload: {JsonSerializer.Serialize(webhookEvent)}");

            // Handle document completion
            if (webhookEvent.EventType == "DocumentCompleted" || 
                webhookEvent.Status == "Completed")
            {
                await HandleDocumentCompletedAsync(webhookEvent);
            }

            return Ok(new { status = "received" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing SigniFlow webhook {ex}");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task HandleDocumentCompletedAsync(SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            Console.WriteLine($"Processing completed document: DocID={webhookEvent.DocId}");

             // Parse the metadata from AdditionalData
            CommitmentMetadata? metadata = null;
            if (webhookEvent.AdditionalData != null)
            {
                metadata = JsonSerializer.Deserialize<CommitmentMetadata>(webhookEvent.AdditionalData);
            }
            
            
            if (metadata != null)
            {
                await _procoreService.UpdateCommitmentStatusAsync(
                    metadata.CommitmentId,
                    metadata.ProjectId,
                    metadata.CompanyId,
                    new ProcoreEnums.WorkflowStatus().Pending,
                    webhookEvent.CompletedDate //,
                    // webhookEvent.DocumentUrl
                );
                Console.WriteLine($"Successfully updated Procore commitment {metadata.CommitmentId}");
            }
            else
            {
                Console.WriteLine("Could not get metadata");
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling completed document: {ex}");
            throw;
        }
    }
}