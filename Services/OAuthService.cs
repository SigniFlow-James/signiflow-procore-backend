// ============================================================
// FILE: Services/OAuthService.cs
// ============================================================
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Signiflow.APIClasses;
using Procore.APIClasses;

public class AuthService
{
    private readonly OAuthSession _oauthSession;
    private readonly SigniflowApiClient _signiflowClient;

    private readonly ProcoreApiClient _procoreClient;

    public AuthService(OAuthSession oauthSession, SigniflowApiClient sfClient, ProcoreApiClient pcClient)
    {
        _oauthSession = oauthSession;
        _signiflowClient = sfClient;
        _procoreClient = pcClient;
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
        return _oauthSession.Signiflow.TokenField != null &&
               DateTimeOffset.UtcNow < _oauthSession.Signiflow.TokenField.TokenExpiryField;
    }

    public async Task<string?> CheckRefreshAuthAsync()
    {
        var procoreError = null as string;
        var signiflowError = null as string;
        if (IsProcoreAuthenticated())
        {
            (_, procoreError) = await RefreshProcoreTokenAsync();
        }
        if (IsSigniflowAuthenticated())
        {
            (_, signiflowError) = await SigniflowLoginAsync();
        }
        var error = procoreError ?? $"Procore Auth Error: {procoreError}, " + signiflowError ?? $"Signiflow Auth Error: {signiflowError}";

        if (error != null) { Console.WriteLine(error); }
        return error;
    }

    // ------------------------------------------------------------
    // Async Check authentication
    // ------------------------------------------------------------

    public async Task<bool> CheckAuthResponseAsync(HttpResponse response)
    {
        if (IsProcoreAuthenticated() == false)
        {
            response.StatusCode = 401;
            await response.WriteAsJsonAsync(new
            {
                error = "Not authenticated with Procore"
            });
            return false;
        }

        else if (IsSigniflowAuthenticated() == false)
        {
            response.StatusCode = 401;
            await response.WriteAsJsonAsync(new
            {
                error = "Not authenticated with Signiflow"
            });
            return false;
        }

        return true;
    }


    // ------------------------------------------------------------
    // Procore Authentication
    // ------------------------------------------------------------

    public async Task<(bool success, string? error)> GetProcoreTokenAsync(string code)
    {
        var (token, error) = await _procoreClient.GetProcoreTokenAsync(code);
        if (token == null)
        {
            return (false, error);
        }
        _oauthSession.Procore = token;
        return (true, error);
    }

    public async Task<(bool success, string? error)> RefreshProcoreTokenAsync()
    {
        if (_oauthSession.Procore.RefreshToken == null)
        {
            return (false, "No refresh token available");
        }
        var (token, error) = await _procoreClient.RefreshProcoreTokenAsync(_oauthSession.Procore.RefreshToken);
        if (token == null)
        {
            return (false, error);
        }
        _oauthSession.Procore = token;
        return (true, error);
    }


    // ------------------------------------------------------------
    // Signiflow Authentication
    // ------------------------------------------------------------

    public async Task<(bool success, string? error)> SigniflowLoginAsync()
    {
        try
        {
            var loginRes = await _signiflowClient.LoginAsync();
            Console.WriteLine("🔑 Signiflow login response: " + loginRes.TokenField);
            _oauthSession.Signiflow.TokenField = loginRes.TokenField;
            Console.WriteLine("🔁 Signiflow logged in");
            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Signiflow login error");
            Console.WriteLine(ex);
            return (false, ex.Message);
        }
    }
}

// ============================================================
// END FILE: Services/OAuthService.cs
// ============================================================