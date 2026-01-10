// ============================================================
// FILE: Endpoints/OAuthEndpoints.cs
// ============================================================
public static class OAuthEndpoints
{
    private static OAuthFullInfo GenerateOAuthFullInfo(
        AuthService authService,
        OAuthSession oauthSession,
        string? error = null
    )
    {
        var isProcoreAuthenticated = authService.IsProcoreAuthenticated();
        var isSigniflowAuthenticated = authService.IsSigniflowAuthenticated();
        var procoreExpiryDateTime = null as DateTime?;
        var soonerDateTime = null as DateTime?;

        if (oauthSession.Procore.ExpiresAt.HasValue)
        {
            procoreExpiryDateTime = DateTime.UnixEpoch.AddMilliseconds(oauthSession.Procore.ExpiresAt.Value);
        }
        if (procoreExpiryDateTime < oauthSession.Signiflow.TokenField?.TokenExpiryField)
        {
            soonerDateTime = procoreExpiryDateTime;
        }
        else
        {
            soonerDateTime = oauthSession.Signiflow.TokenField?.TokenExpiryField;
        }

        return new OAuthFullInfo
        {
            Procore = new OAuthInfo { Authenticated = isProcoreAuthenticated, ExpiresAt = procoreExpiryDateTime },
            Signiflow = new OAuthInfo { Authenticated = isSigniflowAuthenticated, ExpiresAt = oauthSession.Signiflow.TokenField?.TokenExpiryField },
            Authenticated = isProcoreAuthenticated && isSigniflowAuthenticated,
            NextExpiresAt = soonerDateTime,
            Error = error
        };
    }

    public static void MapOAuthEndpoints(this WebApplication app)
    {

        // ------------------------------------------------------------
        // Launch page
        // ------------------------------------------------------------

        app.MapGet("/launch", () =>
            Results.Content(@"
                <html>
                <body>
                    <h3>Signiflow</h3>
                    <button onclick=""connect()"">Connect to Procore</button>

                    <script>
                    function connect() {
                        window.open('/oauth/start', '_blank');
                    }
                    </script>
                </body>
                </html>
                ", "text/html")
        );


        // ------------------------------------------------------------
        // Start OAuth flow
        // ------------------------------------------------------------

        app.MapGet("/oauth/start", (HttpResponse response) =>
        {
            var stateBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            var state = Convert.ToHexString(stateBytes).ToLower();

            var authUrl =
                "https://login-sandbox.procore.com/oauth/authorize" +
                "?response_type=code" +
                $"&client_id={AppConfig.ProcoreClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(AppConfig.RedirectUri)}" +
                $"&state={state}";

            response.Redirect(authUrl);
        });


        // ------------------------------------------------------------
        // OAuth callback
        // ------------------------------------------------------------

        app.MapGet("/oauth/callback", async (HttpRequest request, AuthService authService) =>
        {
            var code = request.Query["code"].ToString();

            if (string.IsNullOrEmpty(code))
            {
                return Results.BadRequest("Missing code");
            }

            Console.WriteLine("OAuth callback received");
            Console.WriteLine($"code: {code}");

            var (success, error) = await authService.GetProcoreTokenAsync(code);
            if (!success)
            {
                Console.WriteLine("❌ Procore token exchange failed: " + error);
                return Results.Content(@"
                    <h2>OAuth Failure</h2>
                    <p>Procore was unable to authenticate you.</p>
                    ", "text/html");
            }

            (success, error) = await authService.SigniflowLoginAsync();
            if (!success)
            {
                Console.WriteLine("❌ Signiflow authentication failed: " + error);
                return Results.Content(@"
                    <h2>OAuth Failure</h2>
                    <p>Signiflow was unable to authenticate you.</p>
                    ", "text/html");
            }

            return Results.Content(@"
                <h2>OAuth success</h2>
                <p>You can close this window and return to Procore.</p>
                ", "text/html");
        });

        // Refresh token
        _ = app.MapPost("/api/oauth/refresh", async (
            HttpResponse response,
            AuthService authService,
            OAuthSession oauthSession
        ) =>
        {
            var procoreRefreshed = false;
            var procoreExpiryDateTime = null as DateTime?;
            var signiflowRefreshed = false;
            var loginRequired = false;
            var error = null as string;
            if (!authService.IsProcoreAuthenticated())
            {
                (procoreRefreshed, loginRequired) = await authService.RefreshProcoreTokenAsync();
            }
            if (!authService.IsSigniflowAuthenticated())
            {
                (signiflowRefreshed, error) = await authService.SigniflowLoginAsync();
            }

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(GenerateOAuthFullInfo(
                authService,
                oauthSession,
                error
            ));
        });

        // Auth status
        app.MapGet("/api/oauth/status", (AuthService authService, OAuthSession oauthSession) =>
        {
            return Results.Json(GenerateOAuthFullInfo(
                authService,
                oauthSession
            ));
        });
    }
}

// ============================================================
// END FILE: Endpoints/OAuthEndpoints.cs
// ============================================================