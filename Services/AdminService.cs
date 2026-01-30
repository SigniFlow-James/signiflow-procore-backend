// ============================================================
// FILE: Services/AdminService.cs
// ============================================================
using System.Text.Json;
using Procore.APIClasses;

public class AdminService
{
    private readonly string _dataFilePath;

    public AdminService()
    {
        // Store filters and viewers in a single JSON file
        var appDirectory = AppContext.BaseDirectory;
        _dataFilePath = Path.Combine(appDirectory, "admin_data.json");
    }

    public async Task<Dictionary<string, AdminDashboardData>> GetDashboardDataAsync()
    {
        var empty = new Dictionary<string, AdminDashboardData>
            {
                { "all", new AdminDashboardData() }
            };
        if (!File.Exists(_dataFilePath))
        {
            await File.WriteAllTextAsync(
                _dataFilePath,
                JsonSerializer.Serialize(empty)
            );

            return empty;
        }

        var json = await File.ReadAllTextAsync(_dataFilePath);  // todo: add threading protection to prevent multiple users accessing at once

        if (string.IsNullOrWhiteSpace(json))
        {
            return empty;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, AdminDashboardData>>(json)
                ?? empty;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"⚠️ Invalid JSON in data file: {ex.Message}");

            return empty;
        }
    }

    public async Task<List<ViewerItem>> GetAllViewersAsync(string CompanyId)
    {
        var data = await GetDashboardDataAsync();
        var viewers = data.ContainsKey("all") ? data["all"].Viewers : [];
        if (data.ContainsKey(CompanyId))
        {
            viewers.AddRange(data[CompanyId].Viewers);
        }
        return viewers;
    }

    public async Task SaveFiltersAsync(string CompanyId, List<FilterItem> filters)
    {
        await SaveDashboardDataAsync(CompanyId, filters: filters);
    }

    public async Task SaveViewersAsync(string CompanyId, List<ViewerItem> viewers)
    {
        await SaveDashboardDataAsync(CompanyId, viewers: viewers);
    }

    public async Task SaveDashboardDataAsync(string CompanyId, List<ViewerItem>? viewers = null, List<FilterItem>? filters = null)
    {
        if (viewers == null && filters == null)
        {
            Console.WriteLine("⚠️ No data to save.");
            return;
        }
        var saveData = await GetDashboardDataAsync();
        if (!saveData.ContainsKey(CompanyId))
        {
            saveData[CompanyId] = new AdminDashboardData();
        }
        if (filters != null) saveData[CompanyId].Filters = filters;
        if (viewers != null) saveData[CompanyId].Viewers = viewers;
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(saveData, options);
        await File.WriteAllTextAsync(_dataFilePath, json);  // todo: add threading protection to prevent multiple users accessing at once
        Console.WriteLine($"💾 Dashboard data saved to {_dataFilePath}");
    }

    public List<Recipient> FilterUsers(
        List<ProcoreRecipient> users,
        string companyId,
        string? projectId = null)
    {
        var dashboardData = GetDashboardDataAsync().Result;
        var filters = dashboardData.ContainsKey("all") ? dashboardData["all"].Filters : [];
        if (dashboardData.TryGetValue(companyId, out AdminDashboardData? value))
        {
            filters.AddRange(value.Filters);
        }
        if (filters.Count == 0)
        {
            Console.WriteLine("ℹ️ No filters configured, returning all users");
            return users.Select(u => new Recipient
            {
                UserId = u.Id?.ToString(),
                FirstNames = u.FirstNames,
                LastName = u.LastName,
                Email = u.EmailAddress,
                JobTitle = u.JobTitle
            }).ToList();
        }
        // Apply filters based on project context
        var applicableFilters = filters.Where(f =>
            f.CompanyId == companyId &&
            (
            f.ProjectId == null || // Company-wide filters
            f.ProjectId == projectId // Project-specific filters
            )
        ).ToList();

        // First, collect all included users
        var includedUsers = new List<ProcoreRecipient>();
        var includeFilters = applicableFilters.Where(f => f.Include == true).ToList();

        if (includeFilters.Count == 0)
        {
            // No include filters means include all users
            includedUsers = [.. users];
        }
        else
        {
            foreach (var filter in includeFilters)
            {
                foreach (var user in users)
                {
                    bool match = filter.Type switch
                    {
                        (int)UserFilterTypeEnum.EmployeeId => user.EmployeeId?.ToString() == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.JobTitle => user.JobTitle == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.FirstName => user.FirstNames == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.LastName => user.LastName == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                        _ => false
                    };

                    if (match && !includedUsers.Contains(user))
                    {
                        includedUsers.Add(user);
                    }
                }
            }
        }

        // Then, apply exclude filters
        var filteredUsers = new List<ProcoreRecipient>(includedUsers);
        var excludeFilters = applicableFilters.Where(f => f.Include == false).ToList();

        foreach (var filter in excludeFilters)
        {
            filteredUsers.RemoveAll(user =>
            {
                return filter.Type switch
                {
                    (int)UserFilterTypeEnum.EmployeeId => user.EmployeeId.ToString() == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.JobTitle => user.JobTitle == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.FirstName => user.FirstNames == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.LastName => user.LastName == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                    _ => false
                };
            });
        }

        var recipientSigners = filteredUsers.Select(u => new Recipient
        {
            UserId = u.Id?.ToString(),
            FirstNames = u.FirstNames,
            LastName = u.LastName,
            Email = u.EmailAddress,
            JobTitle = u.JobTitle
        }).ToList();

        Console.WriteLine($"🔍 Filtered users: {recipientSigners.Count} out of {users.Count}, after applying {filters.Count} filters");
        return recipientSigners;
    }

    /// <summary>
    /// Get viewers for a contract, optionally filtered by region
    /// </summary>
    /// <param name="companyId">The company ID</param>
    /// <param name="projectId">The project ID (null for all projects)</param>
    /// <param name="region">Optional region filter (e.g., "NSW", "VIC")</param>
    /// <returns>List of recipients who should be viewers</returns>
    public List<Recipient> GetViewers(
        string companyId,
        string? projectId = null,
        string? region = null)
    {
        // This will be called when sending a contract to get the configured viewers
        var dashboardData = GetDashboardDataAsync().Result;
        if (!dashboardData.ContainsKey(companyId))
        {
            Console.WriteLine($"⚠️ No dashboard data found for company {companyId}. Returning all users.");
            return [];
        }
        // Filter viewers by company, project, and optionally region
        var applicableViewers = dashboardData[companyId].Viewers.Where(v =>
            v.CompanyId == companyId &&
            (v.ProjectId == null || v.ProjectId == projectId) &&
            (region == null || v.Region == null || v.Region == region) // Include if no specified region filter, viewer has no region, or regions match
        ).ToList();

        var recipients = new List<Recipient>();

        foreach (var viewer in applicableViewers)
        {
            recipients.Add(viewer.Recipient!);
        }

        return recipients;
    }

    /// <summary>
    /// Get all viewers grouped by region for reporting/debugging
    /// </summary>
    public async Task<Dictionary<string, List<ViewerItem>>> GetViewersByRegionAsync(string companyId)
    {
        var data = await GetDashboardDataAsync();
        var dashboardData = GetDashboardDataAsync().Result;
        if (!dashboardData.ContainsKey(companyId))
        {
            Console.WriteLine($"⚠️ No dashboard data found for company {companyId}. Returning all users.");
            return [];
        }
        var companyViewers = dashboardData[companyId].Viewers.Where(v => v.CompanyId == companyId).ToList();
        var groupedByRegion = companyViewers
            .GroupBy(v => v.Region ?? "No Region")
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );

        return groupedByRegion;
    }
}

// ============================================================
// END FILE: Services/AdminService.cs
// ============================================================