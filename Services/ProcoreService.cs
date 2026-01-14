// ============================================================
// FILE: Services/ProcoreService.cs
// ============================================================
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            var postReq = new HttpRequestMessage(HttpMethod.Post, exportUrl);
            postReq.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
            postReq.Headers.Add("Procore-Company-Id", companyId);

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

    public async Task<CreateUploadResponse> CreateUploadAsync(
    long projectId,
    string fileName,
    string contentType,
    byte[] fileBytes)
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

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateUploadResponse>()
               ?? throw new InvalidOperationException("Upload creation failed");
    }


    // ------------------------------------------------------------
    // Upload file to S3 using provided upload info
    // ------------------------------------------------------------

    public async Task UploadFileToS3Async(
    CreateUploadResponse upload,
    byte[] fileBytes,
    string fileName,
    string contentType)
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

        var response = await s3Client.PostAsync(upload.url, form);
        response.EnsureSuccessStatusCode();
    }


    // ------------------------------------------------------------
    // Get document folders in Procore project
    // ------------------------------------------------------------

    public async Task<List<DocumentFolder>> GetDocumentFoldersAsync(
    long projectId)
    {
        var response = await _procoreClient.SendAsync(
            HttpMethod.Get,
            "1.1",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/document_folders"
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<DocumentFolder>>(json)!;
    }

    public long? FindFolderId(
    IEnumerable<DocumentFolder> folders,
    string folderName,
    long? parentId = null)
    {
        return folders.FirstOrDefault(f =>
            f.name.Equals(folderName, StringComparison.OrdinalIgnoreCase) &&
            f.parent_id == parentId
        )?.id;
    }


    // ------------------------------------------------------------
    // Create document folder in Procore
    // ------------------------------------------------------------

    public async Task<DocumentFolder> CreateDocumentFolderAsync(
    long projectId,
    string folderName,
    long? parentFolderId)
    {
        var payload = new
        {
            document_folder = new DocumentFolder
            {
                name = folderName,
                parent_id = parentFolderId
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _procoreClient.SendAsync
        (
            HttpMethod.Post,
            "1.0",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/document_folders",
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

    public async Task CreateDocumentAsync(
    long projectId,
    long folderId,
    string fileName,
    string uploadUuid)
    {
        var payload = new
        {
            document = new DocumentPayload
            {
                name = fileName,
                upload_uuid = uploadUuid,
                parent_id = folderId
            }
        };

        var response = await _procoreClient.SendAsync(
            HttpMethod.Post,
            "1.0",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/documents",
            null,
            payload
        );

        response.EnsureSuccessStatusCode();

    }

    public async Task UpdateCommitmentStatusAsync(
        string commitmentId,
        string projectId,
        string companyId,
        string statusValue,
        DateTime? completedDateTime //,
                                    // string workflowUrl,
                                    // string[]? uploadIds
        )
    {
        Console.WriteLine("2");
        try
        {

            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Patch,
                RequestUri = new Uri($"https://api.procore.com/rest/v2.0/companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}"),
                Content = new StringContent("{\"status\":\"statusValue\"}".Replace("statusValue", statusValue))
                {
                    Headers =
                        {
                            ContentType = new MediaTypeHeaderValue("application/json")
                        }
                }
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
            Console.WriteLine("1");

            var response = await client.SendAsync(request);
            
            Console.WriteLine("2");
            Console.WriteLine(await response.Content.ReadAsStringAsync());

            response.EnsureSuccessStatusCode();
            Results.Ok("OK");


            // var endpoint = $"companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}";
            // var token = _oauthSession.Procore;
            // if (token?.AccessToken == null)
            // {
            //     throw new InvalidOperationException("Procore token is null");
            // }
            // Console.WriteLine("Grabbing existing commitment");
            // var response = await _procoreClient.SendAsync(
            //     HttpMethod.Get,
            //     "2.0",
            //     token.AccessToken,
            //     endpoint,
            //     companyId.ToString(),
            //     null
            //     );
            // response.EnsureSuccessStatusCode();
            // var responseJson = await response.Content.ReadAsStringAsync();
            // var commitmentData = JsonSerializer.Deserialize<WorkOrderContractResponse>(responseJson);
            // Console.WriteLine(commitmentData?.Data);

            // var patchRequest = CommitmentContractMapper.ToPatchRequest(commitmentData);

            // patchRequest.Status = ProcoreEnums.WorkflowStatus.Approved;

            // Console.WriteLine($"4, {patchRequest}");



            // Console.WriteLine("5");

            // response = await _procoreClient.SendAsync(
            //     HttpMethod.Patch,
            //     "2.0",
            //     token.AccessToken,
            //     endpoint,
            //     companyId.ToString(),
            //     updatePayload
            //     );

            // Console.WriteLine("6");

            // response.EnsureSuccessStatusCode();

            // Console.WriteLine($"Updated Procore commitment {commitmentId} status");
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