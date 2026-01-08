using System.Text.Json;

namespace SigniflowBackend.Helpers;

public class ProcoreHelpers
{
    public OAuthSession _oauthSession;
    public string _procoreApiBase;
    public int _retryLimit;

    public ProcoreHelpers(OAuthSession oauthSession, string procoreApiBase, int retryLimit)
    {
        _oauthSession = oauthSession;
        _procoreApiBase = procoreApiBase;
        _retryLimit = retryLimit;
    }

    public async Task<(bool success, JsonElement body, string? error)> ParseRequestBody(HttpRequest request)
    {
        try
        {
            var body = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);
            return (true, body, null);
        }
        catch
        {
            return (false, default, "Invalid JSON body");
        }
    }

    public (bool isValid, JsonElement form, JsonElement context, string? error) ValidateRequestStructure(JsonElement body)
    {
        if (!body.TryGetProperty("form", out var form) ||
            !body.TryGetProperty("context", out var context))
        {
            Console.WriteLine("❌ Missing form or context");
            return (false, default, default, "Missing form or context");
        }

        return (true, form, context, null);
    }

    public (bool isValid, string? companyId, string? projectId, string? commitmentId, string? view, string? error) ExtractProcoreContext(JsonElement context)
    {
        if (!context.TryGetProperty("company_id", out var companyIdProp) ||
            !context.TryGetProperty("project_id", out var projectIdProp) ||
            !context.TryGetProperty("object_id", out var commitmentIdProp))
        {
            Console.WriteLine("❌ Invalid Procore context");
            return (false, null, null, null, null, "Invalid Procore context");
        }

        var companyId = companyIdProp.GetString();
        var projectId = projectIdProp.GetString();
        var commitmentId = commitmentIdProp.GetString();
        var view = context.TryGetProperty("view", out var viewProp)
            ? viewProp.GetString()
            : null;

        return (true, companyId, projectId, commitmentId, view, null);
    }

    public async Task<bool> StartPdfExport(HttpClient httpClient, string exportUrl, string companyId)
    {
        var postReq = new HttpRequestMessage(HttpMethod.Post, exportUrl);
        postReq.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                _oauthSession.Procore.AccessToken
            );
        postReq.Headers.Add("Procore-Company-Id", companyId);

        var exportPost = await httpClient.SendAsync(postReq);
        Console.WriteLine("Export response status: " + (int)exportPost.StatusCode);

        return exportPost.IsSuccessStatusCode;
    }

    public async Task<byte[]?> PollForPdf(HttpClient httpClient, string exportUrl, string companyId)
    {
        int retries = _retryLimit;

        while (retries > 0)
        {
            var getReq = new HttpRequestMessage(HttpMethod.Get, exportUrl);
            getReq.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _oauthSession.Procore.AccessToken
                );
            getReq.Headers.Add("Procore-Company-Id", companyId);

            var exportResponse = await httpClient.SendAsync(getReq);

            if (exportResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var pdfBytes = await exportResponse.Content.ReadAsByteArrayAsync();
                Console.WriteLine("✅ PDF exported successfully, size: " + pdfBytes.Length);
                return pdfBytes;
            }

            if (exportResponse.StatusCode == System.Net.HttpStatusCode.Accepted ||
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
                return null;
            }
        }

        Console.WriteLine("❌ PDF export timed out after retries");
        return null;
    }

    public string BuildExportUrl(string companyId, string projectId, string commitmentId)
    {
        return $"{_procoreApiBase}/rest/v2.0/companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}/pdf";
    }
}