// ============================================================
// FILE: Services/ProcoreService.cs
// ============================================================
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
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
            );
            }
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

    public async Task<(CommitmentBase?, string? error)> GetCommitmentAsync(
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
                $"companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}?view=extended",
                companyId
                );

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            var type = data.GetProperty("type");
            Console.WriteLine(data);
            Console.WriteLine(type);

            if (type.ToString() == ProcoreEnums.ProcoreCommitmentType.WorkOrder)
            {
                var commitment = JsonSerializer.Deserialize<WorkOrderCommitment>(data) ?? throw new InvalidCastException("WorkOrderCommitment is null");
                return (commitment, null);
            }
            else if (type.ToString() == ProcoreEnums.ProcoreCommitmentType.PurchaseOrder)
            {
                var commitment = JsonSerializer.Deserialize<PurchaseOrderCommitment>(data) ?? throw new InvalidCastException("PurchaseOrderCommitment is null");
                return (commitment, null);
            }
            else
            {
                return (null, "Commitment type can't be determined");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return (null, ex.Message);
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

    public async Task<List<FileItem>> GetDocumentFilesAsync(
    string companyId,
    string projectId
    )
    {
        var response = await _procoreClient.SendAsync(
            HttpMethod.Get,
            "2.0",
            _oauthSession.Procore.AccessToken,
            $"projects/{projectId}/documents?sort=document_type&filters%5Bdocument_type%5D=file",
            companyId
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var files = JsonSerializer.Deserialize<FileRoot>(json);
        if (files is null) return [];
        return files.Data;
    }

    public List<FileItem> FindFilesFromProstoreIds(
    List<FileItem> files,
    List<string> ids)
    {
        List<FileItem> filteredFiles = [];
        foreach (var file in files)
        {
            try
            {
                var storeId = file.FileInfo.CurrentVersion.ProstoreFile.Id;
                if (ids.Contains(storeId))
                {
                    filteredFiles.Add(file);
                }
            }
            catch
            {
                continue;
            }
        }
        return filteredFiles;
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
    CommitmentPatchBase patch)
    {
        await HandleCommitmentRequestAsync(context, HttpMethod.Patch, patch, true);
    }

    private async Task HandleCommitmentRequestAsync(
    ProcoreContext context,
    HttpMethod method,
    CommitmentPatchBase? patch,
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