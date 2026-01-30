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
    // Get Procore Users
    // ------------------------------------------------------------

    public async Task<List<ProcoreRecipient>> GetProcoreUsersAsync(
        string companyId,
        string? projectId = null
    )
    {
        try
        {
            HttpResponseMessage response;
            if (string.IsNullOrEmpty(projectId))
            {
                response = await _procoreClient.SendAsync(
                HttpMethod.Get,
                "1.3",
                _oauthSession.Procore.AccessToken,
                $"companies/{companyId}/users",
                companyId
            );}
            else
            {
                response = await _procoreClient.SendAsync(
                HttpMethod.Get,
                "1.0",
                _oauthSession.Procore.AccessToken,
                $"projects/{projectId}/users",
                companyId
            );
            }
            
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Procore Users JSON: {json}");
            var users = JsonSerializer.Deserialize<List<ProcoreUser>>(json)
                           ?? [];
            
            var recipients = users.Select(user => new ProcoreRecipient
            {
                Id = user.Id.ToString(),
                EmployeeId = user.EmployeeId,
                EmailAddress = user.EmailAddress,
                FirstNames = user.FirstNames,
                LastName = user.LastName,
                JobTitle = user.JobTitle
            }).ToList();

            return recipients;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error fetching Procore managers:");
            Console.WriteLine(ex);
            return [];
        }
    }

    // ------------------------------------------------------------
    // Get Procore Companies
    // ------------------------------------------------------------

    public async Task<List<ProcoreCompany>> GetCompaniesAsync(
    )
    {
        try
        {
            var response = await _procoreClient.SendAsync(
                HttpMethod.Get,
                "1.0",
                _oauthSession.Procore.AccessToken,
                $"companies"
            );

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var res = JsonSerializer.Deserialize<List<RawProcoreCompany>>(json)
                           ?? [];
            var companies = res.Select(user => new ProcoreCompany
            {
                Id = user.Id,
                Name = user.Name
            }).ToList();

            return companies;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error fetching Procore companies:");
            Console.WriteLine(ex);
            return [];
        }
    }

    // ------------------------------------------------------------
    // Get Procore Projects
    // ------------------------------------------------------------

    public async Task<List<ProcoreProject>> GetProjectsAsync(
        string companyId
    )
    {
        try
        {
            var response = await _procoreClient.SendAsync(
                HttpMethod.Get,
                "1.0",
                _oauthSession.Procore.AccessToken,
                $"companies/{companyId}/projects"
            );

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var res = JsonSerializer.Deserialize<List<RawProcoreProject>>(json)
                           ?? [];
            var projects = res.Select(user => new ProcoreProject
            {
                Id = user.Id,
                Name = user.Name
            }).ToList();

            return projects;
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error fetching Procore projects:");
            Console.WriteLine(ex);
            return [];
        }
    }

    public async Task<(bool, string? error)> CheckCommitmentAsync(
        string companyId,
        string projectId,
        string commitmentId)
    {
        try
        {
            var response = await _procoreClient.SendAsync(
                HttpMethod.Get,
                "2.0",
                _oauthSession.Procore.AccessToken,
                $"companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}",
                companyId
                );
            
            response.EnsureSuccessStatusCode();

            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error checking commitment:");
            Console.WriteLine(ex);
            return (false, "Error checking commitment");
        }
    }

    // ------------------------------------------------------------
    // Export commitment PDF from Procore
    // ------------------------------------------------------------

    public async Task<(byte[]? pdfBytes, string? error)> ExportCommitmentPdfAsync(ProcoreContext context)
    {
        try
        {
            var exportUrl =
                $"companies/{context.CompanyId}/projects/{context.ProjectId}/commitment_contracts/{context.CommitmentId}/pdf";

            // Start export and pdg generation on procore side (POST)
            await _procoreClient.SendAsync(
                HttpMethod.Post,
                "2.0",
                _oauthSession.Procore.AccessToken,
                exportUrl,
                context.CompanyId
                );

            // Poll and wait for PDF to finish generation (GET)
            int retries = AppConfig.RetryLimit;
            byte[]? pdfBytes = null;

            while (retries > 0)
            {
                var getReq = new HttpRequestMessage(HttpMethod.Get, exportUrl);
                getReq.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
                getReq.Headers.Add("Procore-Company-Id", context.CompanyId);

                var exportResponse = await _procoreClient.SendAsync(
                    HttpMethod.Get,
                    "2.0",
                    _oauthSession.Procore.AccessToken,
                    exportUrl,
                    context.CompanyId
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

    private async Task<CreateUploadResponse> DocumentUploadAsync(
    string projectId,
    string fileName,
    byte[] fileBytes,
    HttpMethod method,
    List<string>? eTags = null,
    string? uuid = null,
    string contentType = "application/pdf")
    {
        // Method Usage options:
        // Create new upload: 
        //      method = HttpMethod.Post, eTags = null, uuid = null
        // Complete upload: 
        //      method = HttpMethod.Patch, eTags = AWS S3 response tags, uuid = upload ID from POST

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
                etag = eTags?.FirstOrDefault()
            }
        }
        };

        string? extension = method == HttpMethod.Post ? "" : "/" + uuid;
        var response = await _procoreClient.SendAsync(
            method,
            "1.1",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/uploads{extension}",
            null,
            payload,
            true
        );

        Console.WriteLine(await response.Content.ReadAsStringAsync());
        // response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateUploadResponse>()
               ?? throw new InvalidOperationException("Upload creation failed");
    }


    // ------------------------------------------------------------
    // Upload file to S3 using provided upload info
    // ------------------------------------------------------------

    private async Task<List<string>> UploadFileToS3Async(
    CreateUploadResponse upload,
    byte[] fileBytes)
    {
        using var http = new HttpClient();

        var offset = 0;
        var uploadedETags = new List<string>();

        foreach (var segment in upload.Segments)
        {
            var segmentBytes = fileBytes
                .Skip(offset)
                .Take((int)segment.Size)
                .ToArray();

            offset += (int)segment.Size;

            using var request = new HttpRequestMessage(HttpMethod.Put, segment.Url);
            request.Headers.ExpectContinue = false;

            request.Headers.Add(
                "x-amz-content-sha256",
                segment.Headers.XAmzContentSha256);

            request.Content = new ByteArrayContent(segmentBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            request.Content.Headers.ContentLength = segment.Size;
            request.Content.Headers.Add(
                "Content-MD5",
                segment.Headers.ContentMd5);

            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception(
                    $"S3 upload failed: {(int)response.StatusCode} {response.StatusCode}\n{body}");
            }

            // Extract ETag from response headers
            if (response.Headers.TryGetValues("ETag", out var etags))
            {
                var etag = etags.FirstOrDefault()?.Trim('"');
                if (etag != null)
                {
                    uploadedETags.Add(etag);
                }
                else
                {
                    throw new Exception("S3 did not return an ETag header");
                }
            }
            else
            {
                throw new Exception("S3 did not return an ETag header");
            }
        }

        return uploadedETags;
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
        // Generate upload ID and url
        Console.WriteLine("creating placeholder object");
        var targetUpload = await DocumentUploadAsync(projectId, fileName, fileBytes, HttpMethod.Post);
        Console.WriteLine($"Placeholder created: {targetUpload.Uuid}");

        // upload document to url with ID
        Console.WriteLine("Attempting post to AWS");
        var etags = await UploadFileToS3Async(targetUpload, fileBytes);
        Console.WriteLine("Post success");

        // Finalise upload object in procore
        Console.WriteLine("Attempting etag patch to procore upload");
        await DocumentUploadAsync(projectId, fileName, fileBytes, HttpMethod.Patch, etags, targetUpload.Uuid);
        Console.WriteLine("Patch complete, file uploaded.");
        return targetUpload.Uuid;
    }



    // ------------------------------------------------------------
    // Update commitment in Procore
    // ------------------------------------------------------------

    public async Task PatchCommitmentAsync(
    ProcoreContext context,
    CommitmentContractPatch patch)
    {
        await HandleCommitmentRequestAsync(context, HttpMethod.Patch, patch, true);
    }

    private async Task HandleCommitmentRequestAsync(
    ProcoreContext context,
    HttpMethod method,
    CommitmentContractPatch? patch,
    bool useJsonOptions)
    {
        try
        {
            var endpoint = $"companies/{context.CompanyId}/projects/{context.ProjectId}/commitment_contracts/{context.CommitmentId}";
            var response = await _procoreClient.SendAsync(
                method,
                "2.0",
                _oauthSession.Procore.AccessToken,
                endpoint,
                context.CompanyId,
                patch,
                useJsonOptions
                );

            // Console.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating Procore commitment {context.CommitmentId}, {ex}");
            throw;
        }
    }
}



// ============================================================
// END FILE: Services/ProcoreService.cs
// ============================================================