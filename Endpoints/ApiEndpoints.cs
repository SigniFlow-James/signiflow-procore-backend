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
        app.MapPost("/api/init", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService,
            AuthService authService,
            AdminService adminService
        ) =>
        {
            var companyId = request.Headers["company-id"].ToString();
            var projectId = request.Headers["project-id"].ToString();
            var commitmentId = request.Headers["object-id"].ToString();
            bool success;
            string? error;
            (success, error) = await procoreService.CheckCommitmentAsync(companyId, projectId, commitmentId);
            Console.WriteLine($"API init error: {error}");
            if (!success)
            {
                Console.WriteLine("❌ Invalid context");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "Invalid context" });
                return;
            }
            else
            {
                Console.WriteLine("✅ User context challenge successful");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Challenge successful",
                    token = adminService.GenerateUserToken()
                });
                return;
            }

        });
        // Fetch recipients from Procore
        _ = app.MapGet("/api/recipients", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService,
            AuthService authService,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /api/recipients received");
            bool isAdmin = false;
            string token;
            if (request.Headers.TryGetValue("bearer-token", out var t))
            {
                token = t.ToString();
                if (adminService.ChallengeAdminToken(token))
                {   
                    isAdmin = true;
                }
                else if (!adminService.ChallengeUserToken(token))
                {
                    Console.WriteLine("❌ Invalid token");
                    response.StatusCode = 401;
                    await response.WriteAsJsonAsync(new { error = "Invalid token" });
                    return;
                }
            }
            else
            {
                Console.WriteLine("❌ Missing token");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "Missing token" });
                return;
            }


            // Auth guard
            if (!await authService.CheckAuthResponseAsync(response))
            {
                Console.WriteLine("❌ Invalid auth");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "External Authentication failed, OAuth restart required." });
                return;
            }

            // Read query parameters
            var companyId = request.Headers["company-id"].ToString();
            var projectId = request.Headers["project-id"].ToString();

            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(projectId))
            {
                Console.WriteLine("❌ Missing Procore context IDs");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new
                {
                    error = "Missing required query parameters: company_id and project_id"
                });
                return;
            }

            // Fetch recipients

            var users = await procoreService.GetProcoreUsersAsync(companyId, projectId);
            var filteredUsers = adminService.FilterUsers(users, companyId, projectId);

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                signers = filteredUsers,
                token = isAdmin ? adminService.GenerateAdminToken(token) : adminService.GenerateUserToken(token)
            });
        });


        // Send Procore PDF to SigniFlow
        app.MapPost("/api/send", async (
            HttpRequest request,
            HttpResponse response,
            AuthService authService,
            ProcoreService procoreService,
            SigniflowService signiflowService,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /api/send received");
            var userTokenCheck = adminService.UserTokenCheck(request);
            if (userTokenCheck == null)
            {
                Console.WriteLine("❌ Invalid token");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "Invalid token" });
                return;
            }

            // Auth guard
            if (!await authService.CheckAuthResponseAsync(response))
            {
                Console.WriteLine("❌ Invalid auth");
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



            // Extract info from form
            if (!form.TryGetProperty("generalContractorSigner", out var generalContractorProp))
            {
                Console.WriteLine("❌ Missing General Contractor information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing manager details" });
                return;
            }
            var generalContractor = JsonSerializer.Deserialize<BasicUserInfo>(generalContractorProp);

            if (!form.TryGetProperty("subContractor", out var subContractorProp))
            {
                Console.WriteLine("❌ Missing Sub Contractor information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing sub-contractor email, first names or last name" });
                return;
            }

            var subContractor = JsonSerializer.Deserialize<BasicUserInfo>(subContractorProp);

            if (generalContractor == null || 
                generalContractor.Email == "" ||
                generalContractor.FirstNames == "" ||
                generalContractor.LastName == "")
            {
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "General contractor names or email are missing" });
                return;
            }

            if (subContractor == null ||
                subContractor.Email == "" ||
                subContractor.FirstNames == "" ||
                subContractor.LastName == "")
            {
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Sub contractor names or email are missing" });
                return;
            }
            var customMessage = form.TryGetProperty("customMessage", out var msgProp)
                ? msgProp.GetString()
                : null;


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
            Console.WriteLine(new { generalContractor, subContractor });

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
            var viewers = await adminService.GetAllViewersAsync(companyId);

            var (workflowResponse, signiflowError) = await signiflowService.CreateWorkflowAsync(
                pdfBytes!,
                metadata,
                documentName,
                generalContractor,
                subContractor,
                viewers,
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
            var patch = new CommitmentContractPatch
            {
                Status = new ProcoreEnums.WorkflowStatus().AwaitingSignature,
            };

            await procoreService.PatchCommitmentAsync(
                commitmentId,
                projectId,
                companyId,
                patch
            );

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                success = true,
                pdfSize = pdfBytes!.Length,
                documentId = workflowResponse.DocIDField,
                documentName,
                token = adminService.GenerateUserToken(userTokenCheck)
            });
        });
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================