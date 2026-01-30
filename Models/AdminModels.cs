// ============================================================
// FILE: Models/AdminModels.cs
// ============================================================

using System.Text.Json.Serialization;

public class AdminDashboardData
{
    public List<FilterItem> Filters { get; set; } = [];
    public List<ViewerItem> Viewers { get; set; } = [];
}

public class FilterItem
{
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("include")]
    public bool Include { get; set; } = true;
}

public class ViewerItem
{
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    [JsonPropertyName("projectId")]
    public string? ProjectId { get; set; }  // null means all projects

    [JsonPropertyName("type")]
    public string Type { get; set; } = "procore"; // "manual" or "procore" - default changed to "procore"

    [JsonPropertyName("recipient")]
    public Recipient? Recipient { get; set; } // For Procore users

    [JsonPropertyName("region")]
    public string? Region { get; set; } // Australian state/territory: NSW, VIC, QLD, SA, WA, TAS, NT, ACT
}

public class Recipient
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; } // For Procore users

    [JsonPropertyName("firstNames")]
    public string FirstNames { get; set; } = "";

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = "";

    [JsonPropertyName("email")]
    public string Email { get; set; } = "";

    [JsonPropertyName("jobTitle")]
    public string JobTitle { get; set; } = "";
}

public enum UserFilterTypeEnum
{
    EmployeeId = 0,
    JobTitle = 1,
    FirstName = 2,
    LastName = 3,
    EmailAddress = 4
}

// Australian regions enum for type safety (optional)
public enum AustralianRegion
{
    NSW,  // New South Wales
    VIC,  // Victoria
    QLD,  // Queensland
    SA,   // South Australia
    WA,   // Western Australia
    TAS,  // Tasmania
    NT,   // Northern Territory
    ACT   // Australian Capital Territory
}

// ============================================================
// END FILE: Models/AdminModels.cs
// ============================================================