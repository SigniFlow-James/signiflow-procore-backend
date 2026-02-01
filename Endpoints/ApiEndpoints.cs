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
                Console.WriteLine("Test 1");
                var context = new ProcoreContext
                {
                    CommitmentId = "116891",
                    ProjectId = "310481",
                    CompanyId = "4279506",
                    CommitmentType = ""
                };

                CommitmentBase? commitment;
                string? error;
                (commitment, error) = await procoreService.GetCommitmentAsync(context.CompanyId, context.ProjectId, context.CommitmentId);
                if (commitment == null)
                {
                    Console.WriteLine("❌ Commitment 1 returned null");
                    return;
                }
                context.CommitmentType = commitment.Type;

                CommitmentPatchBase patch;
                if (context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
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

                await procoreService.PatchCommitmentAsync(
                    context,
                    patch
                );

                Console.WriteLine("Test 2");
                if (context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
                {
                    patch = new WorkOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedContractReceivedDate = DateOnly.FromDateTime(DateTime.Today)
                    };
                }
                else
                {
                    patch = new PurchaseOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(DateTime.Today)
                    };
                }

                await procoreService.PatchCommitmentAsync(
                        context,
                        patch
                    );

                Console.WriteLine("Test 3");
                context = new ProcoreContext
                {
                    CommitmentId = "112291",
                    ProjectId = "310481",
                    CompanyId = "4279506",
                    CommitmentType = ""
                };

                (commitment, error) = await procoreService.GetCommitmentAsync(context.CompanyId, context.ProjectId, context.CommitmentId);
                if (commitment == null)
                {
                    Console.WriteLine("❌ Commitment 2 returned null");
                    return;
                }
                context.CommitmentType = commitment.Type;

                if (context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
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

                await procoreService.PatchCommitmentAsync(
                    context,
                    patch
                );

                Console.WriteLine("Test 4");
                if (context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
                {
                    patch = new WorkOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedContractReceivedDate = DateOnly.FromDateTime(DateTime.Today)
                    };
                }
                else
                {
                    patch = new PurchaseOrderPatch
                    {
                        Status = ProcoreEnums.SubcontractWorkflowStatus.Approved,
                        SignedPurchaseOrderReceivedDate = DateOnly.FromDateTime(DateTime.Today)
                    };
                }

                await procoreService.PatchCommitmentAsync(
                        context,
                        patch
                    );

                Results.Ok("OK");
            }
            catch
            {
                Results.BadRequest("FAIL");
            }
        }
        );

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
            CommitmentBase? success;
            string? error;
            (success, error) = await procoreService.GetCommitmentAsync(companyId, projectId, commitmentId);
            if (success == null)
            {
                Console.WriteLine($"❌ Invalid context - API init error: {error}");
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
                !body.TryGetProperty("context", out var contextProp))
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
                await response.WriteAsJsonAsync(new { error = "Missing general contractor details" });
                return;
            }

            var generalContractor = JsonSerializer.Deserialize<BasicUserInfo>(generalContractorProp);

            if (!form.TryGetProperty("subContractorSigner", out var subContractorProp))
            {
                Console.WriteLine("❌ Missing Sub Contractor information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing sub-contractor details" });
                return;
            }

            var subContractor = JsonSerializer.Deserialize<BasicUserInfo>(subContractorProp);

            if (generalContractor == null ||
                generalContractor.Email == "" ||
                generalContractor.FirstNames == "" ||
                generalContractor.LastName == "")
            {
                Console.WriteLine("❌ Missing General Contractor names or email");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "General contractor names or email are missing" });
                return;
            }

            if (subContractor == null ||
                subContractor.Email == "" ||
                subContractor.FirstNames == "" ||
                subContractor.LastName == "")
            {
                Console.WriteLine("❌ Missing Sub Contractor names or email");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Sub contractor names or email are missing" });
                return;
            }
            var customMessage = form.TryGetProperty("customMessage", out var msgProp)
                ? msgProp.GetString()
                : null;


            // Extract Procore context
            var context = JsonSerializer.Deserialize<ProcoreContext>(contextProp);
            if (context == null ||
                context.CompanyId == "" ||
                context.ProjectId == "" ||
                context.CommitmentId == "" ||
                context.CommitmentType == "")
            {
                Console.WriteLine("❌ Invalid Procore context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Invalid Procore context" });
                return;
            }
            // var chunks = context.CommitmentType.Split('/');
            // if (chunks.Contains(ProcoreEnums.ProcoreCommitmentType.WorkOrder))
            // {
            //     context.CommitmentType = ProcoreEnums.ProcoreCommitmentType.WorkOrder;
            // }
            // else if (chunks.Contains(ProcoreEnums.ProcoreCommitmentType.PurchaseOrder))
            // {
            //     context.CommitmentType = ProcoreEnums.ProcoreCommitmentType.PurchaseOrder;
            // }
            // else
            // {
            //     Console.WriteLine("❌ Commitment doesn't match a known type");
            //     response.StatusCode = 400;
            //     await response.WriteAsJsonAsync(new { error = "Invalid Procore context" });
            //     return;
            // }

            // Get full commitment info
            CommitmentBase? commitment;
            string? error;
            (commitment, error) = await procoreService.GetCommitmentAsync(context.CompanyId, context.ProjectId, context.CommitmentId);
            if (commitment == null)
            {
                Console.WriteLine("❌ Commitment returned null");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Invalid Procore context" });
                return;
            }

            context.CommitmentType = commitment.Type;

            Console.WriteLine("📥 /api/send received");
            Console.WriteLine($"Procore context: {JsonSerializer.Serialize(context)}");
            Console.WriteLine(new { generalContractor, subContractor });

            // Export PDF from Procore
            var (pdfBytes, exportError) = await procoreService.ExportCommitmentPdfAsync(
                context
            );

            if (exportError != null)
            {
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = exportError });
                return;
            }

            Console.WriteLine("📤 Sending PDF to SigniFlow...");

            // Send to SigniFlow
            var documentName = $"Procore_Commitment_{context.CommitmentId}";
            var viewers = await adminService.GetAllViewersAsync(context.CompanyId);

            var (workflowResponse, signiflowError) = await signiflowService.CreateWorkflowAsync(
                pdfBytes!,
                context,
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
            CommitmentPatchBase patch;
            if (context.CommitmentType == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
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

            await procoreService.PatchCommitmentAsync(
                context,
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