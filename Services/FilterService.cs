// ============================================================
// FILE: Services/FilterService.cs
// ============================================================
using System.Text.Json;
using Procore.APIClasses;
using System.Linq;

public class FilterService
{
    private readonly string _filtersFilePath;

    public FilterService()
    {
        // Store filters in a JSON file in the application directory
        var appDirectory = AppContext.BaseDirectory;
        _filtersFilePath = Path.Combine(appDirectory, "filters.json");
    }

    public async Task<FilterData> GetFiltersAsync()
    {
        if (!File.Exists(_filtersFilePath))
        {
            File.Create(_filtersFilePath).Dispose();
            // Return empty filters if file doesn't exist
            return new FilterData
            {
                Users = new List<FilterItem>(),
                Vendors = new List<FilterItem>()
            };
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filtersFilePath);
            var filters = JsonSerializer.Deserialize<FilterData>(json);
            return filters ?? new FilterData
            {
                Users = new List<FilterItem>(),
                Vendors = new List<FilterItem>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error reading filters file: {ex.Message}");
            return new FilterData
            {
                Users = new List<FilterItem>(),
                Vendors = new List<FilterItem>()
            };
        }
    }

    public async Task SaveFiltersAsync(List<FilterItem> managers, List<FilterItem> recipients)
    {
        var filterData = new FilterData
        {
            Users = managers,
            Vendors = recipients
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(filterData, options);
        await File.WriteAllTextAsync(_filtersFilePath, json);
        Console.WriteLine($"💾 Filters saved to {_filtersFilePath}");
    }

    public List<ProcoreUserRecipient> FilterUsersAsync(List<ProcoreUserRecipient> users, FilterData filters)
    {
        var includedUsers = new List<ProcoreUserRecipient>();
        foreach (var filter in filters.Users.Where(u => u.Include == true))
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

        var filteredUsers = new List<ProcoreUserRecipient>();
        foreach (var filter in filters.Users.Where(u => u.Include == false))
        {
            foreach (var user in includedUsers)
            {
                bool exclude = filter.Type switch
                {
                    (int)UserFilterTypeEnum.EmployeeId => user.EmployeeId.ToString() == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.JobTitle => user.JobTitle == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.FirstName => user.FirstName == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.LastName => user.LastName == filter.Value?.ToString(),
                    (int)UserFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                    _ => false
                };

                if (!exclude && !filteredUsers.Contains(user))
                {
                    filteredUsers.Add(user);
                }
            }
        }

        return filteredUsers;
    }

    public List<ProcoreVendorRecipient> FilterVendorsAsync(List<ProcoreVendorRecipient> vendors, FilterData filters)
    {
        var includedUsers = new List<ProcoreVendorRecipient>();
        foreach (var filter in filters.Vendors.Where(u => u.Include == true))
        {
            foreach (var user in vendors)
            {
                bool match = filter.Type switch
                {
                    (int)VendorFilterTypeEnum.VendorId => user.VendorId.ToString() == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.VendorName => user.VendorName == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.PrimaryContactFirstName => user.PrimaryContactFirstName == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.PrimaryContactLastName => user.PrimaryContactLastName == filter.Value?.ToString(),
                    _ => false
                };

                if (match && !includedUsers.Contains(user))
                {
                    includedUsers.Add(user);
                }
            }
        }

        var filteredUsers = new List<ProcoreVendorRecipient>();
        foreach (var filter in filters.Vendors.Where(u => u.Include == false))
        {
            foreach (var user in includedUsers)
            {
                bool exclude = filter.Type switch
                {
                    (int)VendorFilterTypeEnum.VendorId => user.VendorId.ToString() == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.VendorName => user.VendorName == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.EmailAddress => user.EmailAddress == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.PrimaryContactFirstName => user.PrimaryContactFirstName == filter.Value?.ToString(),
                    (int)VendorFilterTypeEnum.PrimaryContactLastName => user.PrimaryContactLastName == filter.Value?.ToString(),
                    _ => false
                };

                if (!exclude && !filteredUsers.Contains(user))
                {
                    filteredUsers.Add(user);
                }
            }
        }

        return filteredUsers;
    }
}

// ============================================================
// END FILE: Services/FilterService.cs
// ============================================================