// ============================================================
// FILE: Models/ProcoreEnums.cs
// ============================================================
namespace Procore.APIClasses;

public static class ProcoreEnums
{
    public enum Priority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    public enum ProxyAllowed
    {
        No = 0,
        Yes = 1
    }

    // ✅ REAL enum (not a class)
    public enum WorkflowStatus
    {
        Draft,
        Pending,
        AwaitingSignature,
        Approved,
        Terminated,
        Void
    }

    public enum IDType
    {
        SouthAfricanIDNumber = 0,
        Passport = 1,
        None = 2
    }

    public enum FieldType
    {
        Signature = 0,
        NameText = 1,
        DateText = 2,
        EmailAddressText = 3,
        ContactNumberText = 4,
        PlainText = 5,
        PlainTextOptional = 6,
        FaceToface = 7,
        Initial = 8,
        CheckBox = 9,
        F2FInitial = 10,
        AddressText = 11,
        CompanyNameText = 12
    }
}

public static class WorkflowStatusExtensions
{
    // Exact values Procore expects
    public static string ToProcoreValue(this ProcoreEnums.WorkflowStatus status) =>
        status switch
        {
            ProcoreEnums.WorkflowStatus.Draft => "Draft",
            ProcoreEnums.WorkflowStatus.Pending => "Pending",
            ProcoreEnums.WorkflowStatus.AwaitingSignature => "Awaiting Signature",
            ProcoreEnums.WorkflowStatus.Approved => "Approved",
            ProcoreEnums.WorkflowStatus.Terminated => "Terminated",
            ProcoreEnums.WorkflowStatus.Void => "Void",
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

    public static bool IsCompleted(this ProcoreEnums.WorkflowStatus status) =>
        status is ProcoreEnums.WorkflowStatus.Approved
               or ProcoreEnums.WorkflowStatus.Terminated
               or ProcoreEnums.WorkflowStatus.Void;
}


// ============================================================
// END FILE: Models/ProcoreEnums.cs
// ============================================================
