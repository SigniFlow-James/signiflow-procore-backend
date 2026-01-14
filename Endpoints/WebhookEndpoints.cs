using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Procore.APIClasses;
using Signiflow.APIClasses;

[ApiController]
[Route("api/webhooks")]
public class SigniflowWebhookController : ControllerBase
{
    private readonly ProcoreService _procoreService;
    private readonly SigniflowService _signiflowService;

    public SigniflowWebhookController(ProcoreService procoreService, SigniflowService signiflowService)
    {
        _procoreService = procoreService;
        _signiflowService = signiflowService;
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
            if (metadata == null)
            {
                Console.WriteLine("Could not get metadata");
                return;
            }

            //download document from signiflow
            byte[] pdf = [];

            if (!string.IsNullOrWhiteSpace(webhookEvent.DocumentUrl))
            {
                pdf = await _signiflowService.DownloadAsync(webhookEvent.DocumentUrl);
            }
            else
            {
                // fallback (older tenants only)
                //pdf = await DownloadViaApi(@event.DocId);
            }


            // Upload document to procore
            var uploadUuid = await _procoreService.FullUploadContractAsync(metadata.ProjectId, webhookEvent.DocumentName, pdf);

            
            var patch = new CommitmentContractPatch
            {
                Status = new ProcoreEnums.WorkflowStatus().Complete,
                ContractDate = DateOnly.Parse(webhookEvent.CompletedDate.ToString()),
                UploadIds = [uploadUuid]
            };

            await _procoreService.PatchCommitmentAsync(
                    metadata.CommitmentId,
                    metadata.ProjectId,
                    metadata.CompanyId,
                    patch
                );
            Console.WriteLine($"Successfully updated Procore commitment {metadata.CommitmentId}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling completed document: {ex}");
            throw;
        }
    }
}