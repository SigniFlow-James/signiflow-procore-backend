// ============================================================
// FILE: Services/ProcoreApiClient.cs
// ============================================================
using Microsoft.AspNetCore.Http;
using Procore.APIClasses;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Procore.APIClasses;

public class ProcoreApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        
    };

    public ProcoreApiClient(HttpClient http)
    {
        _http = http;
    }

    // ------------------------------------------------------------
    // Aquire Procore tokens
    // ------------------------------------------------------------

    public async Task<(ProcoreSession? session, string? error)> GetProcoreTokenAsync(string code)
    {
        try
        {
            using var httpClient = new HttpClient();

            var payload = new
            {
                grant_type = "authorization_code",
                client_id = AppConfig.ProcoreClientId,
                client_secret = AppConfig.ProcoreClientSecret,
                redirect_uri = AppConfig.RedirectUri,
                code
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
}

// ============================================================
// END FILE: Services/ProcoreApiClient.cs
// ============================================================