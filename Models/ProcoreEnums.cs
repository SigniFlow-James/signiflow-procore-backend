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

    public class ProcoreCommitmentType
    {
        public const string WorkOrder = "WorkOrderContract";
        public const string PurchaseOrder = "PurchaseOrderContract";
    }

    public class SubcontractWorkflowStatus
    {
        public const string Draft = "Draft";
        public const string AwaitingBids = "Out For Bid";
        public const string AwaitingSignature = "Out For Signature";
        public const string Approved = "Approved";
        public const string Complete = "Complete";
        public const string Terminated = "Terminated";
        public const string Void = "Void";
    }

    public class PurchaseOrderWorkflowStatus
    {
        public const string Draft = "Draft";
        public const string Processing = "Processing";
        public const string Submitted = "Submitted";
        public const string PartiallyRecieved = "Partially Recieved";
        public const string Received = "Received";
        public const string Approved = "Approved";
        public const string Closed = "Closed";
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


// ============================================================
// END FILE: Models/ProcoreEnums.cs
// ============================================================
