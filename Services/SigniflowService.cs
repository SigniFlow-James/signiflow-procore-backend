// ============================================================
// FILE: Services/SigniflowService.cs
// ============================================================
using System.Text.Json;
using static FullWorkflowRestAPI.APIClasses.Enums;

namespace FullWorkflowRestAPI.APIClasses;

public class SigniflowService
{
    private readonly SigniflowApiClient _client;
    private readonly OAuthSession _oauthSession;

    public SigniflowService(SigniflowApiClient client, OAuthSession oauthSession)
    {
        _client = client;
        _oauthSession = oauthSession;
    }

    public async Task<Token?> LoginAsync()
    {
        var loginResponse = await _client.LoginAsync();

        if (loginResponse.ResultField != "Success")
        {
            Console.WriteLine("❌ SigniFlow login failed:" + loginResponse.ResultField);
            return null;
        }
        return loginResponse.TokenField;
    }

    // ------------------------------------------------------------
    // Create full SigniFlow workflow from a PDF byte array
    // ------------------------------------------------------------
    public async Task<(FullWorkflowResponse? response, string? error)> CreateWorkflowAsync(
    byte[] pdfBytes,
    string documentName,
    string signerEmail,
    string signerFirstNames,
    string signerLastName,
    string customMessage)
    {
        try
        {
            var token = _oauthSession.Signiflow.TokenField;
            if (token == null)
            {
                return (null, "token is null");
            }

            // ---------------- DOCUMENT ----------------
            var encodedPdf = Convert.ToBase64String(pdfBytes);
            Console.WriteLine($"📄 PDF size: {pdfBytes.Length} bytes, encoded size: {encodedPdf.Length} chars");

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
                CustomMessageField = customMessage,
                FlattenDocumentField = false,
                KeepContentSecurityField = false,
                KeepCustomPropertiesField = false,
                KeepXMPMetadataField = false,

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
                YOffsetField = 0,
                IsInvisibleField = false
            },
            new WorkflowUserFieldInformation
            {
                FieldTypeField = (int)FieldType.DateText,
                PageNumberField = 1,
                TagNameField = "ProcoreGeneralContractorSignedDate",
                WidthField = "200",
                HeightField = "25",
                XOffsetField = -25,
                YOffsetField = 0,
                IsInvisibleField = false
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
                UserFullNameField = signerFirstNames + " " + signerLastName,

                // ADD THESE REQUIRED FIELDS:
                UserFirstNameField = signerFirstNames,
                UserLastNameField = signerLastName,
                LatitudeField = string.Empty,
                LongitudeField = string.Empty,
                SignerPasswordField = string.Empty,
                PhotoAtSigningField = 0,
                SignatureTypeField = 0,

                WorkflowUserFieldsField = signerFields
            };

            workflowRequest.WorkflowUsersListField = new List<WorkflowUserInfo> { signer };

            // ---------------- EXECUTE ----------------
            Console.WriteLine("Workflow Request:");
            Console.WriteLine(JsonSerializer.Serialize(workflowRequest, new JsonSerializerOptions { WriteIndented = true }));
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