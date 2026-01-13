// ============================================================
// FILE: Models/SigniflowEnums.cs
// ============================================================
namespace Signiflow.APIClasses;


public static class SigniflowEnums
{
    public enum AutoRemind { No = 0, Yes = 1 }
    public enum AutoExpire { No = 0, Yes = 1 }
    public enum SendCompletedEmail { No = 0, Yes = 1 }


    public enum DocExtension
    {
        pdf = 0, xls = 1, txt = 2, xlsx = 3, docx = 4,
        doc = 5, xml = 6, png = 7, jpg = 8, gif = 9
    }


    public enum Priority { Low = 0, Normal = 1, High = 2 }
    public enum ProxyAllowed { No = 0, Yes = 1 }


    public enum ActionRequired
    {
        SignDocument = 0,
        ViewDocument = 1,
        ApproveDocument = 2
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


    public enum SentOTP { No = 0, Yes = 1 }
}

// ============================================================
// END FILE: Models/SigniflowEnums.cs
// ============================================================