// ============================================================
// FILE: Services/AuthService.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Http;

public class AuthService
{
    private readonly OAuthSession _oauthSession;

    public AuthService(OAuthSession oauthSession)
    {
        _oauthSession = oauthSession;
    }


    // ------------------------------------------------------------
    // Check Procore authentication
    // ------------------------------------------------------------

    public bool IsProcoreAuthenticated()
    {
        return _oauthSession.Procore.AccessToken != null &&
               _oauthSession.Procore.ExpiresAt != null &&
               DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < _oauthSession.Procore.ExpiresAt;
    }


    // ------------------------------------------------------------
    // Check Signiflow authentication
    // ------------------------------------------------------------

    public bool IsSigniflowAuthenticated()
    {
        return _oauthSession.Signiflow.AccessToken != null &&
               _oauthSession.Signiflow.ExpiresAt != null &&
               DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < _oauthSession.Signiflow.ExpiresAt;
    }


    // ------------------------------------------------------------
    // Async Check authentication
    // ------------------------------------------------------------

    public async Task<bool> CheckAuthAsync(HttpResponse response)
    {
        if (IsProcoreAuthenticated() == false)
        {
            response.StatusCode = StatusCodes.Status401Unauthorized;
            await response.WriteAsJsonAsync(new
            {
                error = "Not authenticated with Procore"
            });
            return false;
        }

        // Implement when Signiflow integration is added
        // vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv

        // else if (IsSigniflowAuthenticated() == false)
        // {
        //     response.StatusCode = StatusCodes.Status401Unauthorized;
        //     await response.WriteAsJsonAsync(new
        //     {
        //         error = "Not authenticated with Signiflow"
        //     });
        //     return false;
        // }

        return true;
    }


    // ------------------------------------------------------------
    // Aquire Procore tokens
    // ------------------------------------------------------------

    public async Task<(bool success, string? error)> GetProcoreTokenAsync(string code)
    {
        try
        {
            using var httpClient = new HttpClient();

            var payload = new
            {
                grant_type = "authorization_code",
                client_id = AppConfig.ClientId,
                client_secret = AppConfig.ClientSecret,
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
                return (false, "Token exchange failed");
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

            _oauthSession.Procore.AccessToken =
                tokenData.GetProperty("access_token").GetString();

            _oauthSession.Procore.RefreshToken =
                tokenData.GetProperty("refresh_token").GetString();

            _oauthSession.Procore.ExpiresAt =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                tokenData.GetProperty("expires_in").GetInt32() * 1000;

            Console.WriteLine("OAuth tokens stored");
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return (false, ex.Message);
        }
    }


    // ------------------------------------------------------------
    // Refresh Procore token
    // ------------------------------------------------------------
    
    public async Task<(bool refreshed, bool loginRequired)> RefreshTokenAsync()
    {
        if (_oauthSession.Procore.RefreshToken == null)
        {
            return (false, true);
        }

        try
        {
            using var httpClient = new HttpClient();

            var payload = new
            {
                grant_type = "refresh_token",
                client_id = AppConfig.ClientId,
                client_secret = AppConfig.ClientSecret,
                refresh_token = _oauthSession.Procore.RefreshToken
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
                return (false, true);
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

            _oauthSession.Procore.AccessToken =
                tokenData.GetProperty("access_token").GetString();

            if (tokenData.TryGetProperty("refresh_token", out var newRefresh))
            {
                _oauthSession.Procore.RefreshToken = newRefresh.GetString();
            }

            _oauthSession.Procore.ExpiresAt =
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
                tokenData.GetProperty("expires_in").GetInt32() * 1000;

            Console.WriteLine("🔁 Procore token refreshed");
            return (true, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Refresh error");
            Console.WriteLine(ex);
            return (false, false);
        }
    }
}

// ============================================================
// END FILE: Services/AuthService.cs
// ============================================================