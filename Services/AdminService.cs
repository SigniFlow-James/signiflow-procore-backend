// ============================================================
// FILE: Services/AdminService.cs
// ============================================================
using System.Text.Json;
using Procore.APIClasses;
using Signiflow.APIClasses;

public class AdminService
{
    private readonly string _dataFilePath;
    private readonly HashSet<Token> _activeAdminTokens;
    private readonly HashSet<Token> _activeUserTokens;
    private readonly string DISK_PATH = AppConfig.DiskPath
        ?? throw new InvalidOperationException("DISK_PATH environment variable not configured");
    private readonly string ADMIN_USERNAME = AppConfig.AdminUsername
        ?? throw new InvalidOperationException("ADMIN_USERNAME environment variable not configured");
    private readonly string ADMIN_PASSWORD = AppConfig.AdminPassword
        ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable not configured");
    private static readonly SemaphoreSlim _fileLock = new(1, 1);

    public AdminService()
    {
        _dataFilePath = Path.Combine(DISK_PATH, "admin_data.json");
        _activeAdminTokens = [];
        _activeUserTokens = [];
    }

    public async Task<Dictionary<string, AdminDashboardData>> ReadDataAsync()
    {
        await _fileLock.WaitAsync();
        var empty = new Dictionary<string, AdminDashboardData>
        {
                { "all", new AdminDashboardData() }
            };
        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return empty;
            }
            return JsonSerializer.Deserialize<Dictionary<string, AdminDashboardData>>(json) ?? empty;
        }
        catch
        {
            // File is corrupted, backup and generate new
            var tmp = Path.Combine(DISK_PATH, $"admin_data_corrupt_{DateTime.Now:yyyyMMddHHmmss}.json");
            File.Move(_dataFilePath, tmp, overwrite: true);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(empty, options);
            tmp = _dataFilePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, _dataFilePath, overwrite: true);
            return empty;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task WriteDataAsync(Dictionary<string, AdminDashboardData> data)
    {
        await _fileLock.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(data, options);

            // Write atomically
            var tmp = _dataFilePath + ".tmp";
            await File.WriteAllTextAsync(tmp, json);
            File.Move(tmp, _dataFilePath, overwrite: true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public bool IsValidAdminCredentials(string? username, string? password)
    {
        if (username != ADMIN_USERNAME || password != ADMIN_PASSWORD)
        {
            throw new UnauthorizedAccessException("Invalid admin credentials");
        }
        return true;
    }

    public string GenerateAdminToken(string? oldToken = null)
    {
        if (oldToken != null)
        {
            RemoveAdminToken(oldToken);
        }

        var token = CreateToken();
        _activeAdminTokens.Add(token);
        return token.TokenField;
    }

    public string GenerateUserToken(string? oldToken = null)
    {
        if (oldToken != null)
        {
            RemoveUserToken(oldToken);
        }

        var token = CreateToken();
        _activeUserTokens.Add(token);
        return token.TokenField;
    }

    private static Token CreateToken()
    {
        return new Token
        {
            TokenField = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            TokenExpiryField = DateTime.UtcNow.AddHours(1)
        };
    }

    public bool ChallengeAdminToken(string token)
    {
        var t = _activeAdminTokens.FirstOrDefault(x => x.TokenField == token);

        if (t == null)
            return false;

        if (DateTime.UtcNow >= t.TokenExpiryField)
        {
            _activeAdminTokens.Remove(t);
            return false;
        }

        return true;
    }

    public bool ChallengeUserToken(string token)
    {
        var t = _activeUserTokens.FirstOrDefault(x => x.TokenField == token);

        if (t == null)
            return false;

        if (DateTime.UtcNow >= t.TokenExpiryField)
        {
            _activeUserTokens.Remove(t);
            return false;
        }

        return true;
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        return request.Headers.TryGetValue("bearer-token", out var token)
            ? token.ToString()
            : null;
    }

    public bool ChallengeToken(string? token, string list)
    {
        if (token == null)
        {
            return false;
        }
        Token? challengeToken;
        if (list == "admin")
        {
            challengeToken = _activeAdminTokens.FirstOrDefault(t => t.TokenField == token);
        }
        else if (list == "user")
        {
            challengeToken = _activeUserTokens.FirstOrDefault(t => t.TokenField == token);
        }
        else
        {
            return false;
        }

        if (challengeToken == null)
        {
            return false;
        }
        if (DateTime.UtcNow >= challengeToken.TokenExpiryField)
        {
            RemoveAdminToken(token);
            return false;
        }
        return true;
    }

    public string? AdminTokenCheck(HttpRequest request)
    {
        var token = ExtractBearerToken(request);
        if (token == null)
            return null;

        return ChallengeAdminToken(token) ? token : null;
    }

    public string? UserTokenCheck(HttpRequest request)
    {
        var token = ExtractBearerToken(request);
        if (token == null)
            return null;

        return ChallengeUserToken(token) ? token : null;
    }

    private void RemoveAdminToken(string token)
    {
        var t = _activeAdminTokens.FirstOrDefault(x => x.TokenField == token);
        if (t != null)
            _activeAdminTokens.Remove(t);
    }

    private void RemoveUserToken(string token)
    {
        var t = _activeUserTokens.FirstOrDefault(x => x.TokenField == token);
        if (t != null)
            _activeUserTokens.Remove(t);
    }


    public async Task<Dictionary<string, AdminDashboardData>> GetDashboardDataAsync()
    {
        var empty = new Dictionary<string, AdminDashboardData>
            {
                { "all", new AdminDashboardData() }
            };
        if (!File.Exists(_dataFilePath))
        {
            await WriteDataAsync(empty);
            return empty;
        }

        try
        {
            return await ReadDataAsync();
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

        await WriteDataAsync(saveData);
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