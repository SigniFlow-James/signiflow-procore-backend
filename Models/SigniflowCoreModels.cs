// ============================================================
// FILE: Models/SigniFlowModels.cs
// ============================================================
namespace FullWorkflowRestAPI.APIClasses;

// Login Models
public class LoginRequest
{
    public string? UserNameField { get; set; }
    public string? PasswordField { get; set; }
}

public class LoginResponse
{
    public string? ResultField { get; set; }
    public Token? TokenField { get; set; }
}

// Token
public class Token
{
    public string TokenField { get; set; } = null!;
    public DateTime TokenExpiryField { get; set; }
}

// Full Workflow Models
public class FullWorkflowRequest
{
    public string? AdditionalDataField { get; set; }
    public int AutoRemindField { get; set; }
    public int AutoExpireField { get; set; }
    public string? CustomMessageField { get; set; }
    public string DocField { get; set; } = null!;
    public string DocNameField { get; set; } = null!;
    public DateTime? DueDateField { get; set; }
    public int ExtensionField { get; set; }
    public PortfolioInfo? PortfolioInformationField { get; set; }
    public int PriorityField { get; set; }
    public int SLAField { get; set; }
    public bool SendFirstEmailField { get; set; }
    public bool SendWorkflowEmailsField { get; set; }
    public Token TokenField { get; set; } = null!;
    public bool UseAutoTagsField { get; set; }
    public List<WorkflowUserInfo> WorkflowUsersListField { get; set; } = new();
    public bool FlattenDocumentField { get; set; }
    public bool KeepContentSecurityField { get; set; }
    public bool KeepCustomPropertiesField { get; set; }
    public bool KeepXMPMetadataField { get; set; }
}

public class FullWorkflowResponse
{
    public string? ResultField { get; set; }
    public string? DocIDField { get; set; }
}

public class PortfolioInfo
{
    public bool CreatePortfolioField { get; set; }
    public bool LinkToPortfolioField { get; set; }
    public int? PortfolioIDField { get; set; }
    public string? PortfolioNameField { get; set; }
}

public class WorkflowUserInfo
{
    public int ActionField { get; set; }
    public int AllowProxyField { get; set; }
    public bool AutoSignField { get; set; }
    public string EmailAddressField { get; set; } = null!;
    public string? LanguageCodeField { get; set; }
    public string? LatitudeField { get; set; }
    public string? LongitudeField { get; set; }
    public string? MobileNumberField { get; set; }
    public int SendCompletedEmailField { get; set; }
    public string? SignReasonField { get; set; }
    public string? SignerPasswordField { get; set; }
    public string? UserFirstNameField { get; set; }
    public string UserFullNameField { get; set; } = null!;
    public string? UserLastNameField { get; set; }
    public List<WorkflowUserFieldInformation> WorkflowUserFieldsField { get; set; } = new();
    public int PhotoAtSigningField { get; set; }
    public int SignatureTypeField { get; set; }
}

public class WorkflowUserFieldInformation
{
    public int FieldTypeField { get; set; }
    public string? FontFamilyField { get; set; }
    public int? FontSizeField { get; set; }
    public int? GroupUserNumberField { get; set; }
    public string? HeightField { get; set; }
    public bool IsInvisibleField { get; set; }
    public string? NameField { get; set; }
    public int PageNumberField { get; set; }
    public string? TagNameField { get; set; }
    public string? ValueField { get; set; }
    public string? WidthField { get; set; }
    public string? XCoordinateField { get; set; }
    public int? XOffsetField { get; set; }
    public string? YCoordinateField { get; set; }
    public int? YOffsetField { get; set; }
}

// ============================================================
// END FILE: Models/SigniFlowModels.cs
// ============================================================