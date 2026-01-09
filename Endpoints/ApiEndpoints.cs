// ============================================================
// FILE: Endpoints/ApiEndpoints.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        
        // ------------------------------------------------------------
        // Refresh token
        // ------------------------------------------------------------
        
        app.MapPost("/api/auth/refresh", async (
            HttpResponse response,
            AuthService authService,
            OAuthSession oauthSession
        ) =>
        {
            var (refreshed, loginRequired) = await authService.RefreshTokenAsync();

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                refreshed,
                loginRequired,
                auth = refreshed
                    ? new { authenticated = true, expiresAt = oauthSession.Procore.ExpiresAt }
                    : (object)oauthSession.Procore
            });
        });


        // ------------------------------------------------------------
        // Auth status
        // ------------------------------------------------------------

        app.MapGet("/api/auth/status", (AuthService authService, OAuthSession oauthSession) =>
        {
            var isAuthenticated = authService.IsProcoreAuthenticated();

            return Results.Json(new
            {
                authenticated = isAuthenticated,
                expiresAt = oauthSession.Procore.ExpiresAt
            });
        });


        // ------------------------------------------------------------
        // Send Procore PDF to SigniFlow
        // ------------------------------------------------------------

        app.MapPost("/api/send", async (
            HttpRequest request,
            HttpResponse response,
            AuthService authService,
            ProcoreService procoreService
        ) =>
        {
            // Auth guard
            if (!await authService.CheckAuthAsync(response))
                return;

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

            if (!body.TryGetProperty("form", out var form) ||
                !body.TryGetProperty("context", out var context))
            {
                Console.WriteLine("❌ Missing form or context");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing form or context" });
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

            // Export PDF from Procore
            var (pdfBytes, error) = await procoreService.ExportCommitmentPdfAsync(
                companyId,
                projectId,
                commitmentId
            );

            if (error != null)
            {
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error });
                return;
            }

            // Convert to base64
            var pdfBase64 = Convert.ToBase64String(pdfBytes!);
            Console.WriteLine(pdfBase64);
            Console.WriteLine("📤 Sending PDF to SigniFlow...");

            // (SigniFlow integration goes here)

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                success = true,
                pdfSize = pdfBytes!.Length
            });
        });
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================