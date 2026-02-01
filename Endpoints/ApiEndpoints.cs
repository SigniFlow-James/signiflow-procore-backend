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
        app.MapGet("/api/test", async (SigniflowService sf) =>
        {
            try
            {
                Console.WriteLine("Test 1");
                var res = await sf.Test();
                Console.WriteLine(res);
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
            AdminService adminService,
            ISendRequestQueue sendQueue
        ) =>
        {
            Console.WriteLine("📥 /api/send received");

            // User token check
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

            Console.WriteLine($"📥 Received body");

            if (!body.TryGetProperty("form", out var form) ||
                !body.TryGetProperty("context", out var contextProp))
            {
                Console.WriteLine("❌ Missing form or context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing form or context" });
                return;
            }

            // Extract General Contractor info
            if (!form.TryGetProperty("generalContractorSigner", out var generalContractorProp))
            {
                Console.WriteLine("❌ Missing General Contractor information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing general contractor details" });
                return;
            }

            var generalContractor = JsonSerializer.Deserialize<BasicUserInfo>(generalContractorProp);

            // Extract Sub Contractor info
            if (!form.TryGetProperty("subContractorSigner", out var subContractorProp))
            {
                Console.WriteLine("❌ Missing Sub Contractor information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing sub-contractor details" });
                return;
            }

            var subContractor = JsonSerializer.Deserialize<BasicUserInfo>(subContractorProp);

            // Validate General Contractor
            if (generalContractor == null ||
                string.IsNullOrWhiteSpace(generalContractor.Email) ||
                string.IsNullOrWhiteSpace(generalContractor.FirstNames) ||
                string.IsNullOrWhiteSpace(generalContractor.LastName))
            {
                Console.WriteLine("❌ Missing General Contractor names or email");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "General contractor names or email are missing" });
                return;
            }

            // Validate Sub Contractor
            if (subContractor == null ||
                string.IsNullOrWhiteSpace(subContractor.Email) ||
                string.IsNullOrWhiteSpace(subContractor.FirstNames) ||
                string.IsNullOrWhiteSpace(subContractor.LastName))
            {
                Console.WriteLine("❌ Missing Sub Contractor names or email");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Sub contractor names or email are missing" });
                return;
            }

            // Extract custom message (optional)
            var customMessage = form.TryGetProperty("customMessage", out var msgProp)
                ? msgProp.GetString()
                : null;

            // Extract and validate Procore context
            var context = JsonSerializer.Deserialize<Procore.APIClasses.ProcoreContext>(contextProp);
            if (context == null ||
                string.IsNullOrWhiteSpace(context.CompanyId) ||
                string.IsNullOrWhiteSpace(context.ProjectId) ||
                string.IsNullOrWhiteSpace(context.CommitmentId) ||
                string.IsNullOrWhiteSpace(context.CommitmentType))
            {
                Console.WriteLine("❌ Invalid Procore context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Invalid Procore context" });
                return;
            }

            // Verify commitment exists (quick validation before queuing)
            var (commitment, error) = await procoreService.GetCommitmentAsync(
                context.CompanyId,
                context.ProjectId,
                context.CommitmentId);

            if (commitment == null || error != null)
            {
                Console.WriteLine($"❌ Commitment validation failed: {error ?? "Commitment not found"}");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = error ?? "Invalid commitment" });
                return;
            }

            // All validation passed - enqueue the request for background processing
            var sendRequest = new SendRequest
            {
                GeneralContractor = generalContractor,
                SubContractor = subContractor,
                Context = context,
                CustomMessage = customMessage,
            };

            await sendQueue.EnqueueAsync(sendRequest);

            Console.WriteLine($"✅ Send request queued for commitment: {context.CommitmentId}");

            // Return success immediately
            response.StatusCode = 202; // 202 Accepted - request queued for processing
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Sent successfully!",
                commitmentId = context.CommitmentId,
                token = adminService.GenerateUserToken(userTokenCheck)
            });
        });
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================