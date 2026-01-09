// ============================================================
// FILE: Services/SigniflowService.cs
// ============================================================
using static FullWorkflowRestAPI.APIClasses.Enums;

namespace FullWorkflowRestAPI.APIClasses;

public class SigniflowService
{
    private readonly SigniflowApiClient _client;

    public SigniflowService(SigniflowApiClient client)
    {
        _client = client;
    }

    // ------------------------------------------------------------
    // Create full SigniFlow workflow from a PDF byte array
    // ------------------------------------------------------------
    public async Task<(FullWorkflowResponse? response, string? error)> CreateWorkflowAsync(
        byte[] pdfBytes,
        string documentName,
        string signerEmail,
        string signerFullName,
        string? customMessage = null)
    {
        try
        {
            // ---------------- LOGIN ----------------
            var loginResponse = await _client.LoginAsync();
            if (loginResponse.ResultField != "Success")
                return (null, "SigniFlow login failed: " + loginResponse.ResultField);

            var token = loginResponse.TokenField;

            if (token == null)
            {
                return (null, "token is null") ;
            }

            // ---------------- DOCUMENT ----------------
            var encodedPdf = Convert.ToBase64String(pdfBytes, Base64FormattingOptions.InsertLineBreaks);

            var workflowRequest = new FullWorkflowRequest
            {
                TokenField = token,
                DocField = encodedPdf,
                DocNameField = documentName,
                ExtensionField = (int)DocExtension.pdf,
                DueDateField = DateTime.UtcNow.AddDays(7),
                AutoRemindField = (int)AutoRemind.No,
                AutoExpireField = (int)AutoExpire.No,
                PriorityField = (int)Priority.Normal,
                SendFirstEmailField = true,
                SendWorkflowEmailsField = true,
                SLAField = 0,
                UseAutoTagsField = false,
                CustomMessageField = customMessage, // Now comes from frontend
                PortfolioInformationField = new PortfolioInfo
                {
                    CreatePortfolioField = true,
                    PortfolioNameField = "Procore Commitments",
                    LinkToPortfolioField = false,
                    PortfolioIDField = 0
                }
            };

            // ---------------- SIGNER ----------------
            var signerFields = new List<WorkflowUserFieldInformation>
            {
                new WorkflowUserFieldInformation
                {
                    FieldTypeField = (int)FieldType.Signature,
                    PageNumberField = 1,
                    TagNameField = "ProcoreGeneralContractorSignHere",
                    WidthField = "133",
                    HeightField = "40",
                    XOffsetField = -40,
                    YOffsetField = 0
                },
                new WorkflowUserFieldInformation
                {
                    FieldTypeField = (int)FieldType.DateText,
                    PageNumberField = 1,
                    TagNameField = "ProcoreGeneralContractorSignedDate",
                    WidthField = "200",
                    HeightField = "25",
                    XOffsetField = -25,
                    YOffsetField = 0
                }
            };

            var signer = new WorkflowUserInfo
            {
                ActionField = (int)ActionRequired.SignDocument,
                AllowProxyField = (int)ProxyAllowed.Yes,
                AutoSignField = false,
                EmailAddressField = signerEmail,
                LanguageCodeField = "en",
                MobileNumberField = string.Empty,
                SendCompletedEmailField = (int)SendCompletedEmail.Yes,
                SignReasonField = "I Accept this document.",
                UserFullNameField = signerFullName,
                WorkflowUserFieldsField = signerFields
            };

            workflowRequest.WorkflowUsersListField = new List<WorkflowUserInfo> { signer };

            // ---------------- EXECUTE ----------------
            var workflowResponse = await _client.FullWorkflowAsync(workflowRequest);
            if (workflowResponse.ResultField != "Success")
                return (null, workflowResponse.ResultField);

            return (workflowResponse, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ SigniFlow error:");
            Console.WriteLine(ex);
            return (null, "Unexpected SigniFlow error");
        }
    }
}

// ============================================================
// END FILE: Services/SigniflowService.cs
// ============================================================