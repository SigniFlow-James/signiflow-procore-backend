// ============================================================
// FILE: Services/ProcoreService.cs
// ============================================================
using System.Net.Http.Headers;

public class ProcoreService
{
    private readonly OAuthSession _oauthSession;

    public ProcoreService(OAuthSession oauthSession)
    {
        _oauthSession = oauthSession;
    }

    public async Task<(byte[]? pdfBytes, string? error)> ExportCommitmentPdf(
        string companyId,
        string projectId,
        string commitmentId)
    {
        try
        {
            using var httpClient = new HttpClient();

            var exportUrl =
                $"{AppConfig.ProcoreApiBase}/rest/v2.0/companies/{companyId}/projects/{projectId}/commitment_contracts/{commitmentId}/pdf";

            // Start export (POST)
            var postReq = new HttpRequestMessage(HttpMethod.Post, exportUrl);
            postReq.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
            postReq.Headers.Add("Procore-Company-Id", companyId);

            var exportPost = await httpClient.SendAsync(postReq);
            Console.WriteLine("Export response status: " + (int)exportPost.StatusCode);

            // Poll for PDF (GET)
            int retries = AppConfig.RetryLimit;
            byte[]? pdfBytes = null;

            while (retries > 0)
            {
                var getReq = new HttpRequestMessage(HttpMethod.Get, exportUrl);
                getReq.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _oauthSession.Procore.AccessToken);
                getReq.Headers.Add("Procore-Company-Id", companyId);

                var exportResponse = await httpClient.SendAsync(getReq);

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
}

// ============================================================
// END FILE: Services/ProcoreService.cs
// ============================================================