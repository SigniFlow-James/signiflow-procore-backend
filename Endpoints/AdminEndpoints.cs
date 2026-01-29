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

        app.MapGet("/admin/projects", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService
        ) =>
        {

            try
            {
                Console.WriteLine("📥 /admin/projects received");
                var companyID = request.Headers["company-id"].ToString();
                var projects = await procoreService.GetProjectsAsync(companyID);
                response.StatusCode = 200;
                Console.WriteLine($"✅ Returning {projects.Count} projects for company {companyID}");
                await response.WriteAsJsonAsync(new
                {
                    projects
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading project data: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load project data" });
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

        // Save viewers endpoint with region support
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

        // Validate region values if provided
        var validRegions = new HashSet<string> { "NSW", "VIC", "QLD", "SA", "WA", "TAS", "NT", "ACT" };
        var validViewers = new List<ViewerItem>();
        foreach (var viewer in viewers)
        {
            Console.WriteLine($"🔍 Validating viewer: {JsonSerializer.Serialize(viewer)}");
            if (!string.IsNullOrEmpty(viewer.Region) && !validRegions.Contains(viewer.Region))
            {
                Console.WriteLine($"❌ Invalid region: {viewer.Region}");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { 
                    error = $"Invalid region '{viewer.Region}'. Must be one of: NSW, VIC, QLD, SA, WA, TAS, NT, ACT" 
                });
                continue;
            }

            // Validate viewer type
            if (viewer.Type != "manual" && viewer.Type != "procore")
            {
                Console.WriteLine($"❌ Invalid viewer type: {viewer.Type}");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { 
                    error = $"Invalid viewer type '{viewer.Type}'. Must be 'manual' or 'procore'" 
                });
                continue;
            }

            var user = viewer.Recipient;
            if (user == null)   
            {
                Console.WriteLine("❌ Missing recipient information");
                response.StatusCode = 400;
                await response.WriteAsJsonAsync(new { 
                    error = "Recipient information is required" 
                });
                continue;
            }

            // Validate required fields based on type
            if (viewer.Type == "manual")
            {
                if (string.IsNullOrWhiteSpace(user.FirstNames) || 
                    string.IsNullOrWhiteSpace(user.LastName) || 
                    string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine("❌ Manual viewer missing required fields");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { 
                        error = "Manual viewers require FirstNames, LastName, and Email" 
                    });
                    continue;
                }
            }
            else if (viewer.Type == "procore")
            {
                if (string.IsNullOrWhiteSpace(user.UserId) ||
                    string.IsNullOrWhiteSpace(user.FirstNames) || 
                    string.IsNullOrWhiteSpace(user.LastName) || 
                    string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine("❌ Procore viewer missing UserId");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { 
                        error = "Procore viewers require UserId" 
                    });
                    continue;
                }
            }
            validViewers.Add(viewer);
        }

        if (validViewers.Count == 0)
        {
            Console.WriteLine("❌ No valid viewers to save");
            response.StatusCode = 400;
            await response.WriteAsJsonAsync(new { error = "Error validating viewers" });
            return;
        }

        await adminService.SaveViewersAsync(validViewers);

        // Log summary
        var regionCounts = validViewers
            .GroupBy(v => v.Region ?? "No Region")
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        
        Console.WriteLine($"✅ Viewers saved: {validViewers.Count} total viewers");
        Console.WriteLine($"   By region: {string.Join(", ", regionCounts)}");
        Console.WriteLine($"   All projects: {validViewers.Count(v => v.ProjectId == null)}");
        Console.WriteLine($"   Specific projects: {validViewers.Count(v => v.ProjectId != null)}");

        response.StatusCode = 200;
        await response.WriteAsJsonAsync(new
        {
            success = true,
            message = viewers.Count == validViewers.Count ? "Viewers saved successfully" : $"Unable to save {viewers.Count - validViewers.Count} invalid viewer(s)",
            summary = new
            {
                total = validViewers.Count,
                allProjects = validViewers.Count(v => v.ProjectId == null),
                specificProjects = validViewers.Count(v => v.ProjectId != null),
                byRegion = validViewers.GroupBy(v => v.Region ?? "No Region")
                    .ToDictionary(g => g.Key, g => g.Count())
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error saving viewers: {ex.Message}");
        Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        response.StatusCode = 500;
        await response.WriteAsJsonAsync(new { error = "Failed to save viewers" });
    }
});

        // Get user info
        app.MapGet("/admin/users", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService
        ) =>
        {
            Console.WriteLine("📥 /admin/users GET received");

            try
            {
                var company = request.Headers["company-id"].ToString();
                var project = request.Headers["project-id"].ToString();
                if (string.IsNullOrEmpty(company))
                {
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company ID header" });
                    return;
                }
                var users = await procoreService.GetProcoreUsersAsync(company, project);

                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    value = users
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