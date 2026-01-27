// ============================================================
// FILE: Endpoints/AdminEndpoints.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Procore.APIClasses;

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
                await response.WriteAsJsonAsync(new
                {
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
                await response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Username or password is incorrect",
                });
            }
        });

        app.MapGet("/admin/companies", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService
        ) =>
        {

            try
            {
                Console.WriteLine("📥 /admin/companies received");
                var companies = await procoreService.GetCompaniesAsync();
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    companies
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading company data: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load company data" });
            }
        });

        // Get dashboard data (filters and viewers)
        app.MapGet("/admin/dashboard", async (
            HttpResponse response,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/dashboard GET received");

            try
            {
                var data = await adminService.GetDashboardDataAsync();

                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    filters = data.Filters,
                    viewers = data.Viewers
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading dashboard data: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load dashboard data" });
            }
        });

        // Save filters
        app.MapPost("/admin/filters", async (
            HttpRequest request,
            HttpResponse response,
            AdminService adminService
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

            if (!body.TryGetProperty("filters", out var filtersJson))
            {
                Console.WriteLine("❌ Missing filters");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing filters" });
                return;
            }

            try
            {
                var filters = JsonSerializer.Deserialize<List<FilterItem>>(filtersJson.GetRawText());

                if (filters == null)
                {
                    throw new Exception("Failed to deserialize filter items");
                }

                await adminService.SaveFiltersAsync(filters);

                Console.WriteLine($"✅ Filters saved: {filters.Count} filters");
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

        // Save viewers
        app.MapPost("/admin/viewers", async (
            HttpRequest request,
            HttpResponse response,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/viewers POST received");

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

            if (!body.TryGetProperty("viewers", out var viewersJson))
            {
                Console.WriteLine("❌ Missing viewers");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { error = "Missing viewers" });
                return;
            }

            try
            {
                var viewers = JsonSerializer.Deserialize<List<ViewerItem>>(viewersJson.GetRawText());

                if (viewers == null)
                {
                    throw new Exception("Failed to deserialize viewer items");
                }

                await adminService.SaveViewersAsync(viewers);

                Console.WriteLine($"✅ Viewers saved: {viewers.Count} viewers");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Viewers saved successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving viewers: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to save viewers" });
            }
        });

        // Get user info
        app.MapGet("/admin/users", async (
            HttpResponse response,
            ProcoreService procoreService
        ) =>
        {
            Console.WriteLine("📥 /admin/users GET received");

            try
            {
                var company = response.Headers["company-id"].ToString();
                if (string.IsNullOrEmpty(company))
                {
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company ID header" });
                    return;
                }
                var companies = await procoreService.GetProcoreUsersAsync(company);

                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    value = companies
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading users: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load users" });
            }
        });
    }
}

// ============================================================
// END FILE: Endpoints/AdminEndpoints.cs
// ============================================================