// ============================================================
// FILE: Endpoints/ApiEndpoints.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Signiflow.APIClasses;
using Procore.APIClasses;
using System.Net.Http.Headers;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // Admin login endpoint
        app.MapPost("/admin/login", async (
            HttpRequest request,
            HttpResponse response
        ) =>
        {
            Console.WriteLine("📥 /admin/login received");

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

            if (!body.TryGetProperty("username", out var usernameProp) ||
                !body.TryGetProperty("password", out var passwordProp))
            {
                Console.WriteLine("❌ Missing username or password");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new {
                    success = false,
                    message = "Username or password is missing",
                });
                return;
            }

            var username = usernameProp.GetString();
            var password = passwordProp.GetString();

            // TODO: Replace with proper authentication
            // This is a placeholder - implement proper credential checking
            if (username == "admin" && password == "admin123")
            {
                Console.WriteLine("✅ Admin login successful");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Login successful",
                });
            }
            else
            {
                Console.WriteLine("❌ Invalid credentials");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new {
                    success = false,
                    message = "Username or password is incorrect",
                });
            }
        });

        // Save admin filters
        app.MapPost("/admin/filters", async (
            HttpRequest request,
            HttpResponse response,
            FilterService filterService
        ) =>
        {
            Console.WriteLine("📥 /admin/filters POST received");

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

            if (!body.TryGetProperty("managers", out var managersJson) ||
                !body.TryGetProperty("recipients", out var recipientsJson))
            {
                Console.WriteLine("❌ Missing managers or recipients");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing managers or recipients" });
                return;
            }

            try
            {
                var managers = JsonSerializer.Deserialize<List<FilterItem>>(managersJson.GetRawText());
                var recipients = JsonSerializer.Deserialize<List<FilterItem>>(recipientsJson.GetRawText());

                if (managers == null || recipients == null)
                {
                    throw new Exception("Failed to deserialize filter items");
                }

                await filterService.SaveFiltersAsync(managers, recipients);

                Console.WriteLine($"✅ Filters saved: {managers.Count} managers, {recipients.Count} recipients");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Filters saved successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving filters: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to save filters" });
            }
        });

        // Get filters
        app.MapGet("/admin/filters", async (
            HttpResponse response,
            FilterService filterService
        ) =>
        {
            Console.WriteLine("📥 /admin/filters GET received");

            try
            {
                var filters = await filterService.GetFiltersAsync();
                
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    managers = filters.Users,
                    recipients = filters.Vendors
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading filters: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load filters" });
            }
        });
    }
}

// ============================================================
// END FILE: Endpoints/ApiEndpoints.cs
// ============================================================