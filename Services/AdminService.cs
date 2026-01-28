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

    public async Task<AdminDashboardData> GetDashboardDataAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            var empty = new AdminDashboardData
            {
                Filters = new List<FilterItem>(),
                Viewers = new List<ViewerItem>()
            };

            await File.WriteAllTextAsync(
                _dataFilePath,
                JsonSerializer.Serialize(empty)
            );

            return empty;
        }

        var json = await File.ReadAllTextAsync(_dataFilePath);  // todo: add threading protection to prevent multiple users accessing at once

        if (string.IsNullOrWhiteSpace(json))
        {
            return new AdminDashboardData
            {
                Filters = new List<FilterItem>(),
                Viewers = new List<ViewerItem>()
            };
        }

        try
        {
            return JsonSerializer.Deserialize<AdminDashboardData>(json)
                ?? new AdminDashboardData
                {
                    Filters = new List<FilterItem>(),
                    Viewers = new List<ViewerItem>()
                };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"⚠️ Invalid JSON in data file: {ex.Message}");

            return new AdminDashboardData
            {
                Filters = new List<FilterItem>(),
                Viewers = new List<ViewerItem>()
            };
        }
    }

    public async Task SaveFiltersAsync(List<FilterItem> filters)
    {
        var currentData = await GetDashboardDataAsync();
        currentData.Filters = filters;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(currentData, options);
        await File.WriteAllTextAsync(_dataFilePath, json); // todo: add threading protection to prevent multiple users accessing at once
        Console.WriteLine($"💾 Filters saved to {_dataFilePath}");
    }

    public async Task SaveViewersAsync(List<ViewerItem> viewers)
    {
        var currentData = await GetDashboardDataAsync();
        currentData.Viewers = viewers;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(currentData, options);
        await File.WriteAllTextAsync(_dataFilePath, json);  // todo: add threading protection to prevent multiple users accessing at once
        Console.WriteLine($"💾 Viewers saved to {_dataFilePath}");
    }

    public List<Recipient> FilterUsers(
        List<ProcoreRecipient> users, 
        string companyId,
        string? projectId = null)
    {
        var dashboardData = GetDashboardDataAsync().Result;
        var filters = dashboardData.Filters;
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
                        (int)UserFilterTypeEnum.EmployeeId => user.EmployeeId.ToString() == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.JobTitle => user.JobTitle == filter.Value?.ToString(),
                        (int)UserFilterTypeEnum.FirstName => user.FirstName == filter.Value?.ToString(),
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
                    (int)UserFilterTypeEnum.FirstName => user.FirstName == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.LastName => user.LastName == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                    _ => false
                };
            });
        }

        var recipientSigners = filteredUsers.Select(u => new Recipient
        {
            UserId = u.EmployeeId.ToString(),
            FirstNames = u.FirstName,
            LastName = u.LastName,
            Email = u.EmailAddress
        }).ToList();

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
        var data = GetDashboardDataAsync().Result;
        
        // Filter viewers by company, project, and optionally region
        var applicableViewers = data.Viewers.Where(v => 
            v.CompanyId == companyId && 
            (v.ProjectId == null || v.ProjectId == projectId) &&
            (region == null || v.Region == null || v.Region == region) // Include if no region filter, viewer has no region, or regions match
        ).ToList();

        var recipients = new List<Recipient>();

        foreach (var viewer in applicableViewers)
        {
            if (viewer.Type == "manual")
            {
                recipients.Add(new Recipient
                {
                    FirstNames = viewer.FirstNames ?? "",
                    LastName = viewer.LastName ?? "",
                    Email = viewer.Email ?? ""
                });
            }
            else if (viewer.Type == "procore" && viewer.UserId != null)
            {
                // Note: You'll need to fetch the actual user details from Procore
                // This is a placeholder - implement actual user lookup
                recipients.Add(new Recipient
                {
                    UserId = viewer.UserId,
                    FirstNames = "", // Fetch from Procore
                    LastName = "",  // Fetch from Procore
                    Email = ""      // Fetch from Procore
                });
            }
        }

        return recipients;
    }

    /// <summary>
    /// Get all viewers grouped by region for reporting/debugging
    /// </summary>
    public async Task<Dictionary<string, List<ViewerItem>>> GetViewersByRegionAsync(string companyId)
    {
        var data = await GetDashboardDataAsync();
        var companyViewers = data.Viewers.Where(v => v.CompanyId == companyId).ToList();
        
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