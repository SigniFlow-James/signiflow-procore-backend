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
    public string? ProjectId { get; set; }
    public string Type { get; set; } = "manual"; // "manual" or "procore"
    public string? UserId { get; set; } // For Procore users
    public string? FirstNames { get; set; } // For manual entry
    public string? LastName { get; set; } // For manual entry
    public string? Email { get; set; } // For manual entry
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

// ============================================================
// END FILE: Models/FilterModels.cs
// ============================================================