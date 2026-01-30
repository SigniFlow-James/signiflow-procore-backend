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
        public string SubContract = "work_order_contracts";
        public string PurchaseOrder = "purchase_order_contracts";
    }

    public class SubcontractWorkflowStatus
    {
        public string Draft = "Draft";
        public string AwaitingBids = "Out For Bid";
        public string AwaitingSignature = "Out For Signature";
        public string Approved = "Approved";
        public string Complete = "Complete";
        public string Terminated = "Terminated";
        public string Void = "Void";
    }

    public class PurchaseOrderWorkflowStatus
    {
        public string Draft = "Draft";
        public string Processing = "Processing";
        public string Submitted = "Submitted";
        public string PartiallyRecieved = "Partially Recieved";
        public string Received = "Received";
        public string Approved = "Approved";
        public string Closed = "Closed";
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
