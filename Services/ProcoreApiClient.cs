// ============================================================
// FILE: Services/ProcoreApiClient.cs
// ============================================================
using Microsoft.AspNetCore.Http;
using Procore.APIClasses;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Procore.APIClasses;

public class ProcoreApiClient
{
    private readonly HttpClient _http;
    private readonly string _clientKey;
    private readonly string _secretKey;
    private readonly string _redirect;

    public ProcoreApiClient(HttpClient http)
    {
        _http = http;

        _clientKey = AppConfig.ProcoreClientId
            ?? throw new InvalidOperationException("SIGNIFLOW_USERNAME environment variable not configured");

        _secretKey = AppConfig.ProcoreClientSecret
            ?? throw new InvalidOperationException("SIGNIFLOW_PASSWORD environment variable not configured");
        
        _redirect = AppConfig.RedirectUri;
    }

    // ------------------------------------------------------------
    // Aquire Procore tokens
    // ------------------------------------------------------------

    public async Task<(ProcoreSession? session, string? error)> GetProcoreTokenAsync(string code)
    {
        try
        {

            var payload = new
            {
                grant_type = "authorization_code",
                client_id = _clientKey,
                client_secret = _secretKey,
                redirect_uri = _redirect,
                code
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var tokenRes = await _http.PostAsync(
                "https://sandbox.procore.com/oauth/token",
                content
            );

            var tokenJson = await tokenRes.Content.ReadAsStringAsync();

            if (!tokenRes.IsSuccessStatusCode)
            {
                Console.WriteLine("Token exchange failed: " + tokenJson);
                return (null, "Token exchange failed");
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

            var token = new ProcoreSession
            {
                AccessToken = tokenData.GetProperty("access_token").GetString(),
                RefreshToken = tokenData.GetProperty("refresh_token").GetString(),
                ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                            tokenData.GetProperty("expires_in").GetInt32() * 1000
            };

            Console.WriteLine("OAuth tokens stored");
            return (token, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return (null, ex.Message);
        }
    }


    // ------------------------------------------------------------
    // Refresh Procore token
    // ------------------------------------------------------------

    public async Task<(ProcoreSession? session, string? error)> RefreshProcoreTokenAsync(string refreshToken)
    {
        try
        {
            using var httpClient = new HttpClient();

            var payload = new
            {
                grant_type = "refresh_token",
                client_id = AppConfig.ProcoreClientId,
                client_secret = AppConfig.ProcoreClientSecret,
                refresh_token = refreshToken
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var tokenRes = await httpClient.PostAsync(
                "https://sandbox.procore.com/oauth/token",
                content
            );

            var tokenJson = await tokenRes.Content.ReadAsStringAsync();

            if (!tokenRes.IsSuccessStatusCode)
            {
                Console.WriteLine("❌ Refresh failed: " + tokenJson);
                return (null, "Refresh failed, see logs");
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

            var token = new ProcoreSession
            {
                AccessToken = tokenData.GetProperty("access_token").GetString(),
                RefreshToken = tokenData.GetProperty("refresh_token").GetString(),
                ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                            tokenData.GetProperty("expires_in").GetInt32() * 1000
            };

            Console.WriteLine("🔁 Procore token refreshed");
            return (token, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Refresh error");
            Console.WriteLine(ex);
            return (null, ex.Message);
        }
    }

    // ------------------------------------------------------------
    // Core POST helper
    // ------------------------------------------------------------
    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string targetVersion,
        string? accessToken,
        string endpoint,
        string? companyId = null,
        object? body = null
        )
    {
        endpoint = $"{_http.BaseAddress}/rest/v{targetVersion}/{endpoint}";
        // Start export
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        if (companyId != null)
        {
            request.Headers.Add("Procore-Company-Id", companyId);
        }
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<T> ApiV1<T>(
        HttpMethod method,
        string accessToken,
        string endpoint,
        string companyId,
        string projectId,
        object? body = null
        )
    {
        endpoint = $"{_http.BaseAddress}/rest/v2.0/companies/{companyId}/projects/{projectId}/{endpoint}";
        // Start export
        var request = new HttpRequestMessage(method, endpoint);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Procore-Company-Id", companyId);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔑 response: {responseJson}");
        return JsonSerializer.Deserialize<T>(responseJson)!;
    }
}

// ============================================================
// END FILE: Services/ProcoreApiClient.cs
// ============================================================