// ============================================================
// FILE: Models/FilterModels.cs
// ============================================================

public class AdminDashboardData
{
    public List<FilterItem> Filters { get; set; } = new();
    public List<ViewerItem> Viewers { get; set; } = new();
}

public class FrontendDashboardData
{
    public List<Recipient> Signers { get; set; } = new();
    public List<Recipient> Viewers { get; set; } = new();
}

public class FilterItem
{
    public string? CompanyId { get; set; }
    public string? ProjectId { get; set; }
    public int Type { get; set; }
    public object? Value { get; set; }
    public bool Include { get; set; } = true;
}

public class ViewerItem
{
    public string? CompanyId { get; set; }
    public string? ProjectId { get; set; }  // null means all projects
    public string Type { get; set; } = "procore"; // "manual" or "procore" - default changed to "procore"
    public Recipient? Recipient { get; set; } // For Procore users
    public string? Region { get; set; } // Australian state/territory: NSW, VIC, QLD, SA, WA, TAS, NT, ACT
}

public class Recipient
{
    
    public string? UserId { get; set; } // For Procore users
    public string FirstNames { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
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
// END FILE: Models/FilterModels.cs
// ============================================================