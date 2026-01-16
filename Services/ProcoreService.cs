// ============================================================
// FILE: Services/ProcoreService.cs
// ============================================================
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Procore.APIClasses;

public class ProcoreService
{
    private readonly OAuthSession _oauthSession;
    private readonly ProcoreApiClient _procoreClient;

    public ProcoreService(OAuthSession oauthSession, ProcoreApiClient procoreClient)
    {
        _oauthSession = oauthSession;
        _procoreClient = procoreClient;
    }

    private static readonly JsonSerializerOptions NullJsonOptions =
    new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };


    // ------------------------------------------------------------
    // Export commitment PDF from Procore
    // ------------------------------------------------------------

    public async Task<(byte[]? pdfBytes, string? error)> ExportCommitmentPdfAsync(
        string companyId,
        string projectId,
        string commitmentId)
    {
        try
        {
            var exportUrl =
                $"companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}/pdf";

            // Start export (POST)
            await _procoreClient.SendAsync(
                HttpMethod.Post,
                "2.0",
                _oauthSession.Procore.AccessToken,
                exportUrl,
                companyId
                );

            // Poll for PDF (GET)
            int retries = AppConfig.RetryLimit;
            byte[]? pdfBytes = null;

            while (retries > 0)
            {
                var getReq = new HttpRequestMessage(HttpMethod.Get, exportUrl);
                getReq.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
                getReq.Headers.Add("Procore-Company-Id", companyId);

                var exportResponse = await _procoreClient.SendAsync(
                    HttpMethod.Get,
                    "2.0",
                    _oauthSession.Procore.AccessToken,
                    exportUrl,
                    companyId
                    );

                if (exportResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    pdfBytes = await exportResponse.Content.ReadAsByteArrayAsync();
                    Console.WriteLine("✅ PDF exported successfully, size: " + pdfBytes.Length);
                    break;
                }
                else if (exportResponse.StatusCode == System.Net.HttpStatusCode.Accepted ||
                         exportResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    retries--;
                    Console.WriteLine($"PDF not ready yet, retries left: {retries}");
                    await Task.Delay(2000);
                }
                else
                {
                    var errorText = await exportResponse.Content.ReadAsStringAsync();
                    Console.WriteLine("❌ PDF export failed: " + errorText);
                    return (null, "PDF export failed");
                }
            }

            if (pdfBytes == null)
            {
                Console.WriteLine("❌ PDF export timed out after retries");
                return (null, "PDF export timed out");
            }

            return (pdfBytes, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error exporting PDF:");
            Console.WriteLine(ex);
            return (null, "Error exporting PDF");
        }
    }


    // ------------------------------------------------------------
    // Create upload in Procore
    // ------------------------------------------------------------

    private async Task<CreateUploadResponse> CreateUploadAsync(
    string projectId,
    string fileName,
    byte[] fileBytes,
    string contentType = "application/pdf")
    {
        var payload = new CreateUploadRequest
        {
            response_filename = fileName,
            response_content_type = contentType,
            attachment_content_disposition = true,
            size = fileBytes.Length,
            segments = new()
        {
            new UploadSegment
            {
                size = fileBytes.Length,
                sha256 = Convert.ToHexString(
                    SHA256.HashData(fileBytes)
                ).ToLowerInvariant(),
                md5 = Convert.ToHexString(
                    MD5.HashData(fileBytes)
                ).ToLowerInvariant(),
                etag = Guid.NewGuid().ToString("N")
            }
        }
        };

        var response = await _procoreClient.SendAsync(
            HttpMethod.Post,
            "1.1",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/uploads",
            null,
            payload
        );

        // response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateUploadResponse>()
               ?? throw new InvalidOperationException("Upload creation failed");
    }


    // ------------------------------------------------------------
    // Upload file to S3 using provided upload info
    // ------------------------------------------------------------

    private async Task UploadFileToS3Async(
    CreateUploadResponse upload,
    string fileName,
    byte[] fileBytes,
    string contentType = "application/pdf")
    {
        using var s3Client = new HttpClient();

        using var form = new MultipartFormDataContent();

        // VERY IMPORTANT: fields must be added EXACTLY as provided
        foreach (var field in upload.fields)
        {
            form.Add(new StringContent(field.Value), field.Key);
        }

        // File field MUST be named "file"
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        Console.WriteLine($"Posting to {upload.url}");
        var response = await s3Client.PostAsync(upload.url, form);
        response.EnsureSuccessStatusCode();
    }


    // ------------------------------------------------------------
    // Get document folders in Procore project
    // ------------------------------------------------------------

    private async Task<DocumentFolder> GetDocumentFoldersAsync(
    string projectId)
    {
        var response = await _procoreClient.SendAsync(
            HttpMethod.Get,
            "1.0",
            _oauthSession.Procore.AccessToken,
            $"folders?project_id={projectId}"
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DocumentFolder>(json)!;
    }

    private DocumentFolder? FindFolder(
    DocumentFolder root,
    string? folderName = null,
    int? parentId = null)
    {
        // Case 1: both null → root
        if (folderName == null && parentId == null)
            return root;

        return FindFolderRecursive(root, folderName, parentId);
    }

    private DocumentFolder? FindFolderRecursive(
        DocumentFolder current,
        string? folderName,
        int? parentId)
    {
        // Case 2: name only
        if (folderName != null && parentId == null &&
            current.Name?.Equals(folderName, StringComparison.OrdinalIgnoreCase) == true)
        {
            return current;
        }

        // Case 3: name + parent
        if (folderName != null && parentId != null &&
            current.Name?.Equals(folderName, StringComparison.OrdinalIgnoreCase) == true &&
            current.ParentId == parentId)
        {
            return current;
        }

        // Recurse into children
        if (current.Folders != null)
        {
            foreach (var child in current.Folders)
            {
                var found = FindFolderRecursive(child, folderName, parentId);
                if (found != null)
                    return found;
            }
        }

        return null;
    }



    // ------------------------------------------------------------
    // Create document folder in Procore
    // ------------------------------------------------------------

    private async Task<DocumentFolder> CreateDocumentFolderAsync(
    string projectId,
    string folderName,
    int? parentFolderId = null)
    {
        var payload = new
        {
            folder = new
            {
                name = folderName,
                parent_id = parentFolderId
            }
        };
        var json = JsonSerializer.Serialize(payload, options: NullJsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _procoreClient.SendAsync
        (
            HttpMethod.Post,
            "1.0",
            _oauthSession.Procore.AccessToken,
            $"folders?project_id={projectId}",
            null,
            content
        );

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DocumentFolder>(responseJson)!;
    }


    // ------------------------------------------------------------
    // Create document record in Procore
    // ------------------------------------------------------------

    private async Task CreateDocumentAsync(
    string projectId,
    long folderId,
    string fileName,
    string uploadUuid)
    {
        var payload = new
        {
            document = new DocumentPayload
            {
                Name = fileName,
                UploadUuid = uploadUuid,
                ParentId = folderId
            }
        };

        var response = await _procoreClient.SendAsync(
            HttpMethod.Post,
            "1.0",
            _oauthSession.Procore.AccessToken,
            $"files?project_id={projectId}",
            null,
            payload
        );

        response.EnsureSuccessStatusCode();

    }


    // ------------------------------------------------------------
    // Full contract upload flow
    // ------------------------------------------------------------

    public async Task<string> FullUploadDocumentAsync(
        string projectId,
        string fileName,
        byte[] fileBytes
    )
    {
        // find folder ID for association
        // Console.WriteLine("Finding folder");
        // var rootFolder = await GetDocumentFoldersAsync(projectId);
        // var targetFolder = FindFolder(rootFolder, "Signed_Contracts");
        // if (targetFolder is null)
        // {
        //     // can't find folder, create new
        //     // var rootFolder = FindFolder(folder) ?? throw new FileNotFoundException("Cannot establish root folder in procore project.");
        //     targetFolder = await CreateDocumentFolderAsync(projectId, "Signed_Contracts"); //, rootFolder.Id);
        // }

        // Generate upload ID and url
        Console.WriteLine("creating placeholder object");
        var targetUpload = await CreateUploadAsync(projectId, fileName, fileBytes);
        Console.WriteLine($"Placeholder created: {targetUpload.url}");
        // upload document to url with ID

        Console.WriteLine("Attempting post to AWS");
        await UploadFileToS3Async(targetUpload, fileName, fileBytes);
        Console.WriteLine("Post success");

        // create document object and associate with upload ID
        // await CreateDocumentAsync(projectId, targetFolder.Id, fileName, targetUpload.uuid);

        return targetUpload.uuid;
    }



    // ------------------------------------------------------------
    // Update commitment in Procore
    // ------------------------------------------------------------

    public async Task PatchCommitmentAsync(
    string commitmentId,
    string projectId,
    string companyId,
    CommitmentContractPatch patch)
    {
        await HandleCommitmentRequestAsync(commitmentId, projectId, companyId, HttpMethod.Patch, patch, true);
    }

    // public async Task GetCommitmentAsync(
    // string commitmentId,
    // string projectId,
    // string companyId)
    // {
    //     return await HandleCommitmentRequestAsync(commitmentId, projectId, companyId, HttpMethod.Get, null, false);
    // }

    private async Task HandleCommitmentRequestAsync(
    string commitmentId,
    string projectId,
    string companyId,
    HttpMethod method,
    CommitmentContractPatch? patch,
    bool useJsonOptions)
    {
        try
        {
            var endpoint = $"companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}";
            var response = await _procoreClient.SendAsync(
                method,
                "2.0",
                _oauthSession.Procore.AccessToken,
                endpoint,
                companyId,
                patch,
                useJsonOptions
                );

            Console.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Procore commitment {commitmentId}, {ex}");
            throw;
        }
    }
}



// ============================================================
// END FILE: Services/ProcoreService.cs
// ============================================================