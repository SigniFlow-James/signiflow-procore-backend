// ============================================================
// FILE: Services/SigniflowService.cs
// ============================================================
using System.Text;
using System.Text.Json;
using static Signiflow.APIClasses.SigniflowEnums;

namespace Signiflow.APIClasses;

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
    Procore.APIClasses.ProcoreContext metaData,
    string documentName,
    BasicUserInfo signerOne,
    BasicUserInfo signerTwo,
    List<ViewerItem> viewers,
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
                // DueDateField = DateTime.UtcNow.AddDays(7),
                AutoRemindField = (int)AutoRemind.No,
                AutoExpireField = (int)AutoExpire.No,
                PriorityField = (int)Priority.Normal,
                SendFirstEmailField = true,
                SendWorkflowEmailsField = true,
                SLAField = 0,
                UseAutoTagsField = false,
                CustomMessageField = customMessage,

                AdditionalDataField = JsonSerializer.Serialize(metaData),

                PortfolioInformationField = new PortfolioInfo
                {
                    CreatePortfolioField = true,
                    PortfolioNameField = "Procore Commitments",
                    LinkToPortfolioField = false,
                    PortfolioIDField = 0
                }
            };

            var signerOneFullInfo = GenerateWorkflowSignerInfo(
                signerOne,
                "ProcoreGeneralContractorSignHere",
                "ProcoreGeneralContractorSignedDate"
                );
            var signerTwoFullInfo = GenerateWorkflowSignerInfo(
                signerTwo,
                "ProcoreSubcontractorSignHere",
                "ProcoreSubcontractorSignedDate"
                );

            // Get viewers
            List<WorkflowUserInfo> workflowViewers = [];
            foreach (ViewerItem viewer in viewers)
            {
                var workflowViewer = GenerateWorkflowViewerInfo(viewer.Recipient!);
                if (workflowViewer == null) continue;
                workflowViewers.Add(workflowViewer);
            }

            workflowRequest.WorkflowUsersListField = [signerTwoFullInfo, signerOneFullInfo, .. workflowViewers];

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

    public WorkflowUserInfo GenerateWorkflowSignerInfo(
        BasicUserInfo info,
        string signTag,
        string dateTag)
    {
        var first = info.FirstNames.Trim();
        var last = info.LastName.Trim();
        var email = info.Email.Trim();
        // ---------------- WORKFLOW SIGNER FIELDS ----------------
        var signerFields = new List<WorkflowUserFieldInformation>
        {
            new WorkflowUserFieldInformation
            {
                FieldTypeField = (int)FieldType.Signature,
                PageNumberField = 1,
                TagNameField = signTag,
                WidthField = "133",
                HeightField = "40",
                XOffsetField = 0,
                YOffsetField = -35,
                IsInvisibleField = false
            },
            new WorkflowUserFieldInformation
            {
                FieldTypeField = (int)FieldType.DateText,
                PageNumberField = 1,
                TagNameField = dateTag,
                WidthField = "200",
                HeightField = "25",
                XOffsetField = 0,
                YOffsetField = -20,
                IsInvisibleField = false
            }
        };

        // ---------------- WORKFLOW USER INFO ----------------
        var signer = new WorkflowUserInfo
        {
            ActionField = (int)ActionRequired.SignDocument,
            AllowProxyField = (int)ProxyAllowed.Yes,
            AutoSignField = false,
            EmailAddressField = email,
            LanguageCodeField = "en",
            MobileNumberField = "",
            SendCompletedEmailField = (int)SendCompletedEmail.Yes,
            SignReasonField = "I Accept this document.",
            UserFullNameField = first + " " + last,
            UserFirstNameField = first,
            UserLastNameField = last,
            LatitudeField = "",
            LongitudeField = "",
            SignerPasswordField = "",
            // PhotoAtSigningField = 0,
            SignatureTypeField = 0,

            WorkflowUserFieldsField = signerFields
        };

        return signer;
    }

    public WorkflowUserInfo? GenerateWorkflowViewerInfo(
        Recipient info
        )
    {
        var first = info.FirstNames ?? "";
        var last = info.LastName ?? "";
        var email = info.Email ?? "";

        if (first == "" || last == "" || email == "")
        {
            return null;
        }

        // ---------------- WORKFLOW USER INFO ----------------
        var viewer = new WorkflowUserInfo
        {
            ActionField = (int)ActionRequired.ViewDocument,
            AllowProxyField = (int)ProxyAllowed.No,
            AutoSignField = false,
            EmailAddressField = email,
            LanguageCodeField = "en",
            MobileNumberField = "",
            SendCompletedEmailField = (int)SendCompletedEmail.Yes,
            UserFullNameField = first + " " + last,
            UserFirstNameField = first,
            UserLastNameField = last,
            LatitudeField = "",
            LongitudeField = ""
        };

        return viewer;
    }

    public async Task<List<string>> AddSupportingDocsToWorkflowAsync(
        List<SupportingWorkflowDocument> supportingWorkflowDocuments,
        int portfolioId
    )
    {
        foreach (var doc in supportingWorkflowDocuments)
        {

        }
        return [];
    }

    public async Task<DownloadResponse> DownloadAsync(string documentId)
    {
        var body = new DownloadRequest
        {
            DocIDField = documentId,
            TokenField = _oauthSession.Signiflow.TokenField
        };

        var response = await _client.DownloadDocumentAsync(body);
        return response;
    }

    public async Task<PortfolioResponse?> Test()
    {
        var token = _oauthSession.Signiflow.TokenField;
        if (token == null)
        {
            return null;
        }
        var request = new{
                DocIDField = 60085,
                DocumentNameField = "Signed Commitment_ 116891",
                PortfolioIDField = 13514,
                TokenField = token
            };
        var response = await _client.PostAsync<PortfolioResponse>(
            endpoint: "LinkToPortfolio",
            body: request,
            false);
        return response;
    }
}

// ============================================================
// END FILE: Services/SigniflowService.cs
// ============================================================