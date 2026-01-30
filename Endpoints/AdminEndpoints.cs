// ============================================================
// FILE: Endpoints/AdminEndpoints.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Procore.APIClasses;
using Signiflow.APIClasses;

public static class AdminEndpoints
{
    private static string? TokenCheck(HttpRequest request, AdminService adminService)
    {
        try
        {
            request.Headers.TryGetValue("bearer-token", out var token);
            bool tokenCheck = adminService.ChallengeToken(token);
            if (tokenCheck)
            {
                return token;
            }
            Console.WriteLine("❌ Invalid token");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }
    public static void MapAdminEndpoints(this WebApplication app)
    {

        // Admin login endpoint
        app.MapPost("/admin/login", async (
            HttpRequest request,
            HttpResponse response,
            AdminService adminService
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

            try
            {
                if (adminService.IsValidAdminCredentials(username, password))
                {
                    Console.WriteLine("✅ Admin login successful");
                    response.StatusCode = 200;
                    await response.WriteAsJsonAsync(new
                    {
                        success = true,
                        message = "Login successful",
                        token = adminService.GenerateAdminToken()
                    });
                    return;
                }
            }
            catch
            {
                Console.WriteLine("❌ Invalid credentials");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Username or password is incorrect",
                });
                return;
            }
        });

        app.MapPost("/admin/token", async (
            HttpRequest request,
            HttpResponse response,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/token received");
            // Token challenge
            string? tokenCheck = TokenCheck(request, adminService);

            if (tokenCheck != null)
            {
                Console.WriteLine("✅ Admin token login successful");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Login successful",
                    token = adminService.GenerateAdminToken(tokenCheck)
                });
                return;
            }
            else
            {
                Console.WriteLine("❌ Invalid token");
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Session Expired",
                });
                return;
            }
        });

        app.MapGet("/admin/companies", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/companies received");
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
                return;
            }

            try
            {
                var companies = await procoreService.GetCompaniesAsync();
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    companies,
                    token = adminService.GenerateAdminToken(tokenCheck)
                });
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading company data: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to load company data" });
                return;
            }
        });

        app.MapGet("/admin/projects", async (
            HttpRequest request,
            HttpResponse response,
            ProcoreService procoreService,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/projects received");
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
                return;
            }

            try
            {
                var companyID = request.Headers["company-id"].ToString();
                var projects = await procoreService.GetProjectsAsync(companyID);
                response.StatusCode = 200;
                Console.WriteLine($"✅ Returning {projects.Count} projects for company {companyID}");
                await response.WriteAsJsonAsync(new
                {
                    projects,
                    token = adminService.GenerateAdminToken(tokenCheck)
                });
                return;
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
            HttpRequest request,
            HttpResponse response,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/dashboard GET received");
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
                return;
            }

            try
            {
                if (!request.Headers.TryGetValue("company-id", out var companyId) || string.IsNullOrEmpty(companyId))
                {
                    Console.WriteLine("❌ Missing Company ID header");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company ID header" });
                    return;
                }
                var data = await adminService.GetDashboardDataAsync();
                var dashboardData = new AdminDashboardData();
                if (!data.ContainsKey(companyId!))
                {
                    Console.WriteLine($"⚠️ No dashboard data found for company {companyId}. Returning empty data.");
                    response.StatusCode = 200;
                    
                    await response.WriteAsJsonAsync(new
                    {
                        filters = dashboardData.Filters,
                        viewers = dashboardData.Viewers,
                        token = adminService.GenerateAdminToken(tokenCheck)
                    });
                    return;
                }
                dashboardData = data[companyId!];

                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    filters = dashboardData.Filters,
                    viewers = dashboardData.Viewers,
                    token = adminService.GenerateAdminToken(tokenCheck)
                });
                return;
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
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
                return;
            }

            JsonElement body;
            string? companyId;
            try
            {
                companyId = request.Headers["company-id"].ToString();
                if (string.IsNullOrEmpty(companyId))
                {
                    Console.WriteLine("❌ Missing Company ID header");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company ID header" });
                    return;
                }
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

                await adminService.SaveFiltersAsync(companyId, filters);

                Console.WriteLine($"✅ Filters saved: {filters.Count} filters");
                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    success = true,
                    message = "Filters saved successfully",
                    token = adminService.GenerateAdminToken(tokenCheck)
                });
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving filters: {ex.Message}");
                response.StatusCode = 500;
                await response.WriteAsJsonAsync(new { error = "Failed to save filters" });
                return;
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
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
                return;
            }

            JsonElement body;
            string? companyId;
            try
            {
                companyId = request.Headers["company-id"].ToString();
                if (string.IsNullOrEmpty(companyId))
                {
                    Console.WriteLine("❌ Missing Company ID header");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company ID header" });
                    return;
                }
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
                var errors = new List<string> {};
                foreach (var viewer in viewers)
                {
                    Console.WriteLine($"🔍 Validating viewer: {JsonSerializer.Serialize(viewer)}");
                    if (!string.IsNullOrEmpty(viewer.Region) && !validRegions.Contains(viewer.Region))
                    {
                        Console.WriteLine($"❌ Invalid region: {viewer.Region}");
                        response.StatusCode = 400;
                        errors.Add($"Invalid region '{viewer.Region}'. Must be one of: NSW, VIC, QLD, SA, WA, TAS, NT, ACT");
                        continue;
                    }

                    // Validate viewer type
                    if (viewer.Type != "manual" && viewer.Type != "procore")
                    {
                        Console.WriteLine($"❌ Invalid viewer type: {viewer.Type}");
                        response.StatusCode = 400;
                        errors.Add($"Invalid viewer type '{viewer.Type}'. Must be 'manual' or 'procore'");
                        continue;
                    }

                    var user = viewer.Recipient;
                    if (user == null)
                    {
                        Console.WriteLine("❌ Missing recipient information");
                        response.StatusCode = 400;
                        errors.Add("Recipient information is required");
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
                            errors.Add("Manual viewers require FirstNames, LastName, and Email");
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
                            errors.Add("Procore viewers require UserId");
                            continue;
                        }
                    }
                    validViewers.Add(viewer);
                }

                if (viewers.Count > 0 && validViewers.Count == 0)
                {
                    Console.WriteLine("❌ No valid viewers to save");
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = $"Unable to save {viewers.Count} invalid viewer(s)", errors });
                    return;
                }

                await adminService.SaveViewersAsync(companyId, validViewers);

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
                    message = viewers.Count == validViewers.Count ? $"{validViewers.Count} Viewer(s) saved successfully" : $"{validViewers.Count} Viewer(s) saved successfully; Unable to save {viewers.Count - validViewers.Count} invalid viewer(s)",
                    token = adminService.GenerateAdminToken(tokenCheck),
                    errors
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
            ProcoreService procoreService,
            AdminService adminService
        ) =>
        {
            Console.WriteLine("📥 /admin/users GET received");
            // Token challenge
            var tokenCheck = TokenCheck(request, adminService);
            if (tokenCheck == null)
            {
                response.StatusCode = 401;
                await response.WriteAsJsonAsync(new { error = "invalid token" });
            }

            try
            {
                var company = request.Headers["company-id"].ToString();
                var project = request.Headers["project-id"].ToString();
                if (string.IsNullOrEmpty(company))
                {
                    response.StatusCode = 400;
                    await response.WriteAsJsonAsync(new { error = "Missing Company or project ID header" });
                    return;
                }
                var users = await procoreService.GetProcoreUsersAsync(company, project);

                response.StatusCode = 200;
                await response.WriteAsJsonAsync(new
                {
                    value = users,
                    token = adminService.GenerateAdminToken(tokenCheck)
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