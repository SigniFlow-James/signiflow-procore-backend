// ============================================================
// END FILE: Models/FilterModels.cs
// ============================================================

public class FilterData
{
    public List<FilterItem> Users { get; set; } = new();
    public List<FilterItem> Vendors { get; set; } = new();
}

public class FilterItem
{
    public string? ProjectId { get; set; } // if null, company wide
    public int Type { get; set; }
    public object? Value { get; set; }
    public bool Include { get; set; } = true;
}

public enum UserFilterTypeEnum
{
    EmployeeId = 0,
    JobTitle = 1,
    FirstName = 2,
    LastName = 3,
    EmailAddress = 4
}

public enum VendorFilterTypeEnum
{
    VendorId = 0,
    VendorName = 1,
    EmailAddress = 2,
    PrimaryContactFirstName = 3,
    PrimaryContactLastName = 4
}

// ============================================================
// END FILE: Models/FilterModels.cs
// ============================================================