// ============================================================
// FILE: Endpoints/OAuthEndpoints.cs
// ============================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class OAuthEndpoints
{
    public static void MapOAuthEndpoints(this WebApplication app)
    {
        // Launch page
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

        // Start OAuth flow
        app.MapGet("/oauth/start", (HttpResponse response) =>
        {
            var stateBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            var state = Convert.ToHexString(stateBytes).ToLower();

            var authUrl =
                "https://login-sandbox.procore.com/oauth/authorize" +
                "?response_type=code" +
                $"&client_id={AppConfig.ClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(AppConfig.RedirectUri)}" +
                $"&state={state}";

            response.Redirect(authUrl);
        });

        // OAuth callback
        app.MapGet("/oauth/callback", async (HttpRequest request, AuthService authService) =>
        {
            var code = request.Query["code"].ToString();

            if (string.IsNullOrEmpty(code))
            {
                return Results.BadRequest("Missing code");
            }

            Console.WriteLine("OAuth callback received");
            Console.WriteLine($"code: {code}");

            var (success, error) = await authService.ExchangeCodeForToken(code);

            if (!success)
            {
                return Results.StatusCode(500);
            }

            return Results.Content(@"
            <h2>OAuth success</h2>
            <p>You can close this window and return to Procore.</p>
            ", "text/html");
        });
    }
}

// ============================================================
// END FILE: Endpoints/OAuthEndpoints.cs
// ============================================================