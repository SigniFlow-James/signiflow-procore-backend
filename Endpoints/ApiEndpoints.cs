// ============================================================
// FILE: Endpoints/ApiEndpoints.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Signiflow.APIClasses;
using Procore.APIClasses;
using System.Net.Http.Headers;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/test", async (ProcoreService procoreService) =>
        {
            try
            {
                // Update status on procore
                await procoreService.UpdateCommitmentStatusAsync(
                    "112291",
                    "310481",
                    "4279506",
                    new ProcoreEnums.WorkflowStatus().AwaitingSignature,
                    null
                );

                Results.Ok("OK");
            }
            catch
            {
                Results.BadRequest("FAIL");
            }
        }
        );

        // Send Procore PDF to SigniFlow
        app.MapPost("/api/send", async (
            HttpRequest request,
            HttpResponse response,
            AuthService authService,
            ProcoreService procoreService,
            SigniflowService signiflowService
        ) =>
        {
            // Auth guard
            if (!await authService.CheckAuthAsync(response))
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "Authentication failed" });
                return;
            }
            // Parse body
            JsonElement body;
            try
            {
                body = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);
            }
            catch
            {
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Invalid JSON body" });
                return;
            }

            Console.WriteLine($"📥 form: {body}");
            if (!body.TryGetProperty("form", out var form) ||
                !body.TryGetProperty("context", out var context))
            {
                Console.WriteLine("❌ Missing form or context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing form or context" });
                return;
            }

            // Extract signer info from form

            if (!form.TryGetProperty("email", out var signerEmailProp) ||
                !form.TryGetProperty("firstNames", out var signerFirstNamesProp) ||
                !form.TryGetProperty("lastName", out var signerLastNameProp))
            {
                Console.WriteLine("❌ Missing signer information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing email, firstNames or lastName" });
                return;
            }

            var signerEmail = signerEmailProp.GetString();
            var signerFirstNames = signerFirstNamesProp.GetString();
            var signerLastName = signerLastNameProp.GetString();
            var customMessage = form.TryGetProperty("customMessage", out var msgProp)
                ? msgProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(signerEmail) || string.IsNullOrWhiteSpace(signerFirstNames) || string.IsNullOrWhiteSpace(signerLastName))
            {
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Signer email and full name are required" });
                return;
            }

            // Extract Procore context
            if (!context.TryGetProperty("company_id", out var companyIdProp) ||
                !context.TryGetProperty("project_id", out var projectIdProp) ||
                !context.TryGetProperty("object_id", out var commitmentIdProp))
            {
                Console.WriteLine("❌ Invalid Procore context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Invalid Procore context" });
                return;
            }

            var companyId = companyIdProp.GetString();
            var projectId = projectIdProp.GetString();
            var commitmentId = commitmentIdProp.GetString();

            if (companyId == null || projectId == null || commitmentId == null)
            {
                Console.WriteLine("❌ Missing Procore context IDs");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing Procore context IDs" });
                return;
            }

            Console.WriteLine("📥 /api/send received");
            Console.WriteLine($"Company: {companyId}, Project: {projectId}, Commitment: {commitmentId}");
            Console.WriteLine($"Signer: {signerFirstNames} {signerLastName} ({signerEmail})");

            // Export PDF from Procore
            var (pdfBytes, exportError) = await procoreService.ExportCommitmentPdfAsync(
                companyId,
                projectId,
                commitmentId
            );

            if (exportError != null)
            {
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = exportError });
                return;
            }

            Console.WriteLine("📤 Sending PDF to SigniFlow...");

            // Send to SigniFlow
            var metadata = new CommitmentMetadata
            {
                CompanyId = companyId,
                ProjectId = projectId,
                CommitmentId = commitmentId,
                IntegrationType = "Procore"
            };
            var documentName = $"Procore_Commitment_{commitmentId}";
            var (workflowResponse, signiflowError) = await signiflowService.CreateWorkflowAsync(
                pdfBytes!,
                metadata,
                documentName,
                signerEmail,
                signerFirstNames,
                signerLastName,
                customMessage ?? ""
            );

            if (signiflowError != null)
            {
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = signiflowError });
                return;
            }

            Console.WriteLine("✅ Workflow created successfully");
            Console.WriteLine($"Document ID: {workflowResponse!.DocIDField}");

            // Update status on procore
            await procoreService.UpdateCommitmentStatusAsync(
                commitmentId,
                projectId,
                companyId,
                new ProcoreEnums.WorkflowStatus().AwaitingSignature,
                null
            );

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                success = true,
                pdfSize = pdfBytes!.Length,
                documentId = workflowResponse.DocIDField,
                documentName
            });
        });
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================