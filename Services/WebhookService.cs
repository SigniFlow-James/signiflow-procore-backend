// ============================================================
// FILE: Services/WebhookService.cs
// ============================================================
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Signiflow.APIClasses;
using Procore.APIClasses;
using System.Text.Json;

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
        Console.WriteLine("Background listener is live");
        await foreach (var evt in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                Console.WriteLine($"Executing item in listener: {evt}");
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<SigniflowWebhookProcessor>();
                if (evt.EventType == "Document Completed" || evt.Status == "Completed")
                {
                    Console.WriteLine("Document Completed");
                    await _authService.CheckRefreshAuthAsync();
                    await processor.ProcessDocumentCompletedAsync(evt);
                }
                else if (evt.EventType == "Document Rejected" || evt.Status == "Rejected")
                {
                    Console.WriteLine("Document Rejected");
                    await _authService.CheckRefreshAuthAsync();
                    await processor.ProcessDocumentRejectedAsync(evt);
                }
                else if (evt.EventType == "Document Cancelled" || evt.Status == "Cancelled")
                {
                    Console.WriteLine("Document Cancelled");
                    await _authService.CheckRefreshAuthAsync();
                    await processor.ProcessDocumentCancelledAsync(evt);
                }
                else
                {
                    Console.WriteLine($"Unhandled event: {evt.EventType}");
                }
                Console.WriteLine("Execution complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Background SigniFlow processing failed: {ex}");
            }
        }
    }
}


public class SigniflowWebhookProcessor
{
    private readonly ProcoreService _procoreService;
    private readonly SigniflowService _signiflowService;

    public SigniflowWebhookProcessor(ProcoreService procoreService, SigniflowService signiflowService)
    {
        _procoreService = procoreService;
        _signiflowService = signiflowService;
    }

    public async Task ProcessDocumentCompletedAsync(SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookEvent.DocId))
            {
                Console.WriteLine("Webhook Completed event received, but could not get Document ID");
                return;
            }

            Console.WriteLine($"Processing completed document: DocID={webhookEvent.DocId}");

            // Parse the metadata from AdditionalData
            var metadata = webhookEvent.Metadata;
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

            string docName = webhookEvent.DocumentName.Length > 0 ? $"{webhookEvent.DocumentName.Replace(".pdf", "")} Signed.pdf" : $"Signed Commitment: {metadata.CommitmentId}.pdf";

            // Upload document to procore
            var uploadUuid = await _procoreService.FullUploadDocumentAsync(metadata.ProjectId, docName, pdf);
            if (string.IsNullOrWhiteSpace(uploadUuid))
            {
                throw new NullReferenceException("Could not get upload uuid");
            }

            // Associate upload to commitment
            CommitmentPatchBase patch;
            if (metadata.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
            {
                patch = new WorkOrderPatch
                {
                    Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                    SignedContractReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                    UploadIds = [uploadUuid]
                };
            }
            else
            {
                patch = new PurchaseOrderPatch
                {
                    Status = ProcoreEnums.PurchaseOrderWorkflowStatus.Approved,
                    SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                    UploadIds = [uploadUuid]
                };
            }

            await _procoreService.PatchCommitmentAsync(
                    metadata,
                    patch
                );
            Console.WriteLine($"Successfully updated Procore commitment {metadata.CommitmentId}");
        }
        catch (Exception ex)
        {
            try
            {
                var metadata = webhookEvent.Metadata;
                if (metadata == null)
                {
                    Console.WriteLine($"Metadata missing, with errors: {ex}");
                    return;
                }

                CommitmentPatchBase patch;
                if (metadata.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
                {
                    patch = new WorkOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedContractReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate)
                    };
                }
                else
                {
                    patch = new PurchaseOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate)
                    };
                }

                await _procoreService.PatchCommitmentAsync(
                        metadata,
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

    public async Task ProcessDocumentRejectedAsync(SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookEvent.DocId))
            {
                Console.WriteLine("Webhook Completed event received, but could not get Document ID");
                return;
            }

            Console.WriteLine($"Processing rejected document: DocID={webhookEvent.DocId}");

            // Parse the metadata from AdditionalData
            var metadata = webhookEvent.Metadata;
            if (metadata == null)
            {
                Console.WriteLine($"Could not get metadata");
                return;
            }

            // Associate upload to commitment
            CommitmentPatchBase patch;
            if (metadata.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
            {
                patch = new WorkOrderPatch
                {
                    Status = ProcoreEnums.SubcontractWorkflowStatus.Terminated,
                    SignedContractReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                };
            }
            else
            {
                patch = new PurchaseOrderPatch
                {
                    Status = ProcoreEnums.PurchaseOrderWorkflowStatus.PartiallyRecieved,
                    SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                };
            }

            await _procoreService.PatchCommitmentAsync(
                    metadata,
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

    public async Task ProcessDocumentCancelledAsync(SigniflowWebhookEvent webhookEvent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookEvent.DocId))
            {
                Console.WriteLine("Webhook Completed event received, but could not get Document ID");
                return;
            }

            Console.WriteLine($"Processing rejected document: DocID={webhookEvent.DocId}");

            // Parse the metadata from AdditionalData
            var metadata = webhookEvent.Metadata;
            if (metadata == null)
            {
                Console.WriteLine($"Could not get metadata");
                return;
            }

            // Associate upload to commitment
            CommitmentPatchBase patch;
            if (metadata.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
            {
                patch = new WorkOrderPatch
                {
                    Status = ProcoreEnums.SubcontractWorkflowStatus.Void,
                    SignedContractReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                };
            }
            else
            {
                patch = new PurchaseOrderPatch
                {
                    Status = ProcoreEnums.PurchaseOrderWorkflowStatus.Closed,
                    SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(webhookEvent.CompletedDate),
                };
            }

            await _procoreService.PatchCommitmentAsync(
                    metadata,
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


// ============================================================
// END FILE: Services/WebhookService.cs
// ============================================================