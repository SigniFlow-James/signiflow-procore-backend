// ============================================================
// FILE: Endpoints/ApiEndpoints.cs
// ============================================================

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Procore.APIClasses;
using Signiflow.APIClasses;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

[ApiController]
[Route("api/webhooks")]
public class SigniflowWebhookController : ControllerBase
{
    private readonly ProcoreService _procoreService;
    private readonly SigniflowService _signiflowService;
    private readonly ISigniflowWebhookQueue _queue;

    public SigniflowWebhookController(ProcoreService procoreService, SigniflowService signiflowService, ISigniflowWebhookQueue queue)
    {
        _procoreService = procoreService;
        _signiflowService = signiflowService;
        _queue = queue;
    }

    [HttpPost("signiflow")]
    public async Task<IActionResult> HandleSigniflowEvent(
    [FromBody] SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            Console.WriteLine($"Received SigniFlow webhook: {webhookEvent.EventType}");

            await _queue.EnqueueAsync(webhookEvent);

            return Ok(new { status = "received" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving SigniFlow webhook {ex}");
            return StatusCode(500);
        }
    }

    public async Task<IActionResult> ProcessDocumentCompletedAsync([FromBody] SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            Console.WriteLine($"Received SigniFlow webhook: {webhookEvent.EventType}");
            Console.WriteLine($"Full payload: {JsonSerializer.Serialize(webhookEvent)}");

            Task eventTask = HandleDocumentCompletedAsync(webhookEvent);
            eventTask.Start();

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
            if (string.IsNullOrWhiteSpace(webhookEvent.DocId))
            {
                Console.WriteLine("Webhook Completed event recieved, but could not get Document ID");
                return;
            }

            Console.WriteLine($"Processing completed document: DocID={webhookEvent.DocId}");



            // Parse the metadata from AdditionalData
            var metadata = GetMetadata(webhookEvent);
            if (metadata == null)
            {
                Console.WriteLine($"Could not get metadata");
                return;
            }


            //download document from signiflow
            byte[] pdf = [];
            Console.WriteLine($"Attempting download of document: DocID={webhookEvent.DocId}");
            var document = await _signiflowService.DownloadAsync(webhookEvent.DocId);
            if (string.IsNullOrWhiteSpace(document.DocField))
            {
                Console.WriteLine("Could not get Document Base64");
                return;
            }
            pdf = Convert.FromBase64String(document.DocField);

            // Upload document to procore
            var uploadUuid = await _procoreService.FullUploadDocumentAsync(metadata.ProjectId, webhookEvent.DocumentName, pdf);

            if (string.IsNullOrWhiteSpace(uploadUuid))
            {
                throw new NullReferenceException("Could not get upload uuid");
            }
            
            // Associate upload to commitment
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
            try
            {
                var metadata = GetMetadata(webhookEvent);

                if (metadata == null)
                {
                    Console.WriteLine($"Metadata missing, with errors: {ex}");
                    return;
                }
                var patch = new CommitmentContractPatch
                {
                    Status = new ProcoreEnums.WorkflowStatus().Complete,
                    ContractDate = DateOnly.Parse(webhookEvent.CompletedDate.ToString()),
                };

                await _procoreService.PatchCommitmentAsync(
                        metadata.CommitmentId,
                        metadata.ProjectId,
                        metadata.CompanyId,
                        patch
                    );
                Console.WriteLine($"Updated Procore commitment {metadata.CommitmentId} with errors: {ex}");
            }
            catch (Exception nestedEx)
            {
                Console.WriteLine($"Error handling completed document: {ex}");
                Console.WriteLine($"During handling of above, error: {nestedEx}");
                throw;
            }
        }
    }

    private CommitmentMetadata? GetMetadata(SigniflowWebhookEvent webhookEvent) 
    {
        CommitmentMetadata? metadata = null;
        if (webhookEvent.AdditionalData != null)
        {
            Console.WriteLine($"Got additional Data: {webhookEvent.AdditionalData}");
            metadata = JsonSerializer.Deserialize<CommitmentMetadata>(webhookEvent.AdditionalData);
        }
        if (metadata == null)
        {
            Console.WriteLine("Could not get metadata");
            return null;
        }
        return metadata;
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================