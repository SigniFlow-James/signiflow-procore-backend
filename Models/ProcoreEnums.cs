// ============================================================
// FILE: Models/ProcoreEnums.cs
// ============================================================
namespace Procore.APIClasses;

public static class ProcoreEnums
{
    public enum Priority { Low = 0, Normal = 1, High = 2 }
    public enum ProxyAllowed { No = 0, Yes = 1 }


    public class WorkflowStatus
    {
        private WorkflowStatus(string value, bool completed = false) { Value = value; Completed = completed; }

        public string Value { get; private set; }
        public bool Completed { get; private set; }

        public static WorkflowStatus Draft { get { return new WorkflowStatus("Draft"); } }
        public static WorkflowStatus Pending { get { return new WorkflowStatus("Pending"); } }
        public static WorkflowStatus AwaitingSignature { get { return new WorkflowStatus("Awaiting Signature"); } }
        public static WorkflowStatus Approved { get { return new WorkflowStatus("Approved", true); } }
        public static WorkflowStatus Terinated { get { return new WorkflowStatus("Terminated", true); } }
        public static WorkflowStatus Void { get { return new WorkflowStatus("Void", true); } }

        public override string ToString()
        {
            return Value;
        }
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