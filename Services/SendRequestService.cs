// ============================================================
// FILE: Services/SendRequestService.cs
// ============================================================
using System.Threading.Channels;
using System.Text.Json;
using Signiflow.APIClasses;
using Procore.APIClasses;

public class SendRequest
{
    public required BasicUserInfo GeneralContractor { get; set; }
    public required BasicUserInfo SubContractor { get; set; }
    public required ProcoreContext Context { get; set; }
    public string? CustomMessage { get; set; }
}

public interface ISendRequestQueue
{
    ValueTask EnqueueAsync(SendRequest request);
    IAsyncEnumerable<SendRequest> DequeueAsync(CancellationToken ct);
}

public class SendRequestQueue : ISendRequestQueue
{
    private readonly Channel<SendRequest> _channel =
        Channel.CreateUnbounded<SendRequest>();

    public ValueTask EnqueueAsync(SendRequest request)
        => _channel.Writer.WriteAsync(request);

    public IAsyncEnumerable<SendRequest> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}

public class SendRequestWorker : BackgroundService
{
    private readonly ISendRequestQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthService _authService;

    public SendRequestWorker(
        ISendRequestQueue queue,
        IServiceProvider serviceProvider,
        AuthService authService)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _authService = authService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("📤 Send Request Background Worker is live");
        await foreach (var request in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                Console.WriteLine($"Processing send request for commitment: {request.Context.CommitmentId}");
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<SendRequestProcessor>();
                
                await _authService.CheckRefreshAuthAsync();
                await processor.ProcessSendRequestAsync(request);
                
                Console.WriteLine($"✅ Successfully processed send request for commitment: {request.Context.CommitmentId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Background send processing failed for commitment {request.Context.CommitmentId}: {ex}");
            }
        }
    }
}

public class SendRequestProcessor
{
    private readonly ProcoreService _procoreService;
    private readonly SigniflowService _signiflowService;
    private readonly AdminService _adminService;

    public SendRequestProcessor(
        ProcoreService procoreService,
        SigniflowService signiflowService,
        AdminService adminService)
    {
        _procoreService = procoreService;
        _signiflowService = signiflowService;
        _adminService = adminService;
    }

    public async Task ProcessSendRequestAsync(SendRequest request)
    {
        try
        {
            // Get full commitment info
            var (commitment, error) = await _procoreService.GetCommitmentAsync(
                request.Context.CompanyId,
                request.Context.ProjectId,
                request.Context.CommitmentId);

            if (commitment == null)
            {
                Console.WriteLine($"❌ Commitment returned null for ID: {request.Context.CommitmentId}");
                return;
            }

            request.Context.CommitmentType = commitment.Type;

            Console.WriteLine($"📤 Processing send request for commitment: {request.Context.CommitmentId}");
            Console.WriteLine($"Procore context: {JsonSerializer.Serialize(request.Context)}");
            Console.WriteLine($"GC: {request.GeneralContractor.Email}, SC: {request.SubContractor.Email}");

            // Export PDF from Procore
            var (pdfBytes, exportError) = await _procoreService.ExportCommitmentPdfAsync(request.Context);

            if (exportError != null)
            {
                Console.WriteLine($"❌ Failed to export PDF: {exportError}");
                return;
            }

            Console.WriteLine("📤 Sending PDF to SigniFlow...");

            // Send to SigniFlow
            var documentName = $"Procore_Commitment_{request.Context.CommitmentId}";
            var viewers = await _adminService.GetAllViewersAsync(request.Context.CompanyId);

            var (workflowResponse, signiflowError) = await _signiflowService.CreateWorkflowAsync(
                pdfBytes!,
                request.Context,
                documentName,
                request.GeneralContractor,
                request.SubContractor,
                viewers,
                request.CustomMessage ?? ""
            );

            if (signiflowError != null)
            {
                Console.WriteLine($"❌ SigniFlow workflow creation failed: {signiflowError}");
                return;
            }

            Console.WriteLine("✅ Workflow created successfully");
            Console.WriteLine($"Document ID: {workflowResponse!.DocIDField}");

            // Update status on Procore
            CommitmentPatchBase patch;
            if (request.Context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
            {
                patch = new WorkOrderPatch
                {
                    Status = ProcoreEnums.SubcontractWorkflowStatus.AwaitingSignature,
                    IssuedOnDate = DateOnly.FromDateTime(DateTime.Today)
                };
            }
            else
            {
                patch = new PurchaseOrderPatch
                {
                    Status = ProcoreEnums.PurchaseOrderWorkflowStatus.Submitted,
                    IssuedOnDate = DateOnly.FromDateTime(DateTime.Today)
                };
            }

            await _procoreService.PatchCommitmentAsync(request.Context, patch);

            Console.WriteLine($"✅ Commitment {request.Context.CommitmentId} status updated to awaiting signature");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing send request: {ex}");
            throw;
        }
    }
}

// ============================================================
// END FILE: Services/SendRequestService.cs
// ============================================================