using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SigniflowBackend.Helpers;

var builder = WebApplication.CreateBuilder(args);


// ------------------
// Services
// ------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "https://signiflow-james.github.io",
                "https://sandbox.procore.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting();

var app = builder.Build();


app.UseCors("DefaultCorsPolicy");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 204;
        return;
    }
    await next();
});

// app.UseHttpsRedirection();


// ------------------
// Environment variables
// ------------------

string? CLIENT_ID = Environment.GetEnvironmentVariable("PROCORE_CLIENT_ID");
string? CLIENT_SECRET = Environment.GetEnvironmentVariable("PROCORE_CLIENT_SECRET");
const string PROCORE_API_BASE = "https://sandbox.procore.com";
const string REDIRECT_URI = "https://signiflow-procore-backend-net.onrender.com/oauth/callback";
const int RETRY_LIMIT = 7;
var procoreHelper = new ProcoreHelpers(oauthSession, PROCORE_API_BASE, RETRY_LIMIT);


// ------------------
// In-memory token store
// ------------------

var oauthSession = new OAuthSession();

async Task<bool> RequireAuth(HttpResponse response)
{
    if (oauthSession.Procore.AccessToken == null ||
        oauthSession.Procore.ExpiresAt == null ||
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= oauthSession.Procore.ExpiresAt)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        await response.WriteAsJsonAsync(new
        {
            error = "Not authenticated with Procore"
        });
        return false;
    }

    return true;
}


// ------------------
// Health check
// ------------------

app.MapGet("/", () => Results.Ok("OK"));


// ------------------
// Manual OAuth routes 
// ------------------

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


// ------------------
// Start OAuth
// ------------------

app.MapGet("/oauth/start", (HttpResponse response) =>
{
    var stateBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
    var state = Convert.ToHexString(stateBytes).ToLower();

    var authUrl =
        "https://login-sandbox.procore.com/oauth/authorize" +
        "?response_type=code" +
        $"&client_id={CLIENT_ID}" +
        $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
        $"&state={state}";

    response.Redirect(authUrl);
});


// ------------------
// OAuth callback
// ------------------

app.MapGet("/oauth/callback", async (HttpRequest request) =>
{
    var code = request.Query["code"].ToString();

    if (string.IsNullOrEmpty(code))
    {
        return Results.BadRequest("Missing code");
    }

    Console.WriteLine("OAuth callback received");
    Console.WriteLine($"code: {code}");

    try
    {
        using var httpClient = new HttpClient();

        var payload = new
        {
            grant_type = "authorization_code",
            client_id = CLIENT_ID,
            client_secret = CLIENT_SECRET,
            redirect_uri = REDIRECT_URI,
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
            return Results.StatusCode(500);
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

        oauthSession.Procore.AccessToken =
            tokenData.GetProperty("access_token").GetString();

        oauthSession.Procore.RefreshToken =
            tokenData.GetProperty("refresh_token").GetString();

        oauthSession.Procore.ExpiresAt =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
            tokenData.GetProperty("expires_in").GetInt32() * 1000;

        Console.WriteLine("OAuth tokens stored");

        return Results.Content(@"
            <h2>OAuth success</h2>
            <p>You can close this window and return to Procore.</p>
            ", "text/html");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.StatusCode(500);
    }
});


// ------------------
// Refresh token
// ------------------

app.MapPost("/api/auth/refresh", async (HttpResponse response) =>
{
    // No refresh token → must re-auth
    if (oauthSession.Procore.RefreshToken == null)
    {
        response.StatusCode = 200;
        await response.WriteAsJsonAsync(new
        {
            refreshed = false,
            loginRequired = true,
            auth = oauthSession.Procore
        });
        return;
    }

    try
    {
        using var httpClient = new HttpClient();

        var payload = new
        {
            grant_type = "refresh_token",
            client_id = CLIENT_ID,
            client_secret = CLIENT_SECRET,
            refresh_token = oauthSession.Procore.RefreshToken
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

            response.StatusCode = 200;
            await response.WriteAsJsonAsync(new
            {
                refreshed = false,
                loginRequired = true,
                auth = oauthSession.Procore
            });
            return;
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson)!;

        oauthSession.Procore.AccessToken =
            tokenData.GetProperty("access_token").GetString();

        if (tokenData.TryGetProperty("refresh_token", out var newRefresh))
        {
            oauthSession.Procore.RefreshToken = newRefresh.GetString();
        }

        oauthSession.Procore.ExpiresAt =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() +
            tokenData.GetProperty("expires_in").GetInt32() * 1000;

        Console.WriteLine("🔁 Procore token refreshed");
        // SaveTokensToDisk();

        await response.WriteAsJsonAsync(new
        {
            refreshed = true,
            loginRequired = false,
            auth = new
            {
                authenticated = true,
                expiresAt = oauthSession.Procore.ExpiresAt
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Refresh error");
        Console.WriteLine(ex);

        response.StatusCode = 500;
        await response.WriteAsJsonAsync(new
        {
            refreshed = false,
            loginRequired = false,
            auth = oauthSession.Procore
        });
    }
});


// ------------------
// OAuth status
// ------------------

app.MapGet("/api/auth/status", () =>
{
    var isAuthenticated =
        oauthSession.Procore.AccessToken != null &&
        oauthSession.Procore.ExpiresAt != null &&
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < oauthSession.Procore.ExpiresAt;

    return Results.Json(new
    {
        authenticated = isAuthenticated,
        expiresAt = oauthSession.Procore.ExpiresAt
    });
});


// ------------------
// Send Procore PDF to SigniFlow
// ------------------

app.MapPost("/api/send", async (HttpRequest request, HttpResponse response) =>
{
    // Auth guard
    if (!await RequireAuth(response))
        return;

    // Parse body
    var (parseSuccess, body, parseError) = await procoreHelper.ParseRequestBody(request);
    if (!parseSuccess)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { error = parseError });
        return;
    }

    // Validate structure
    var (structureValid, form, context, structureError) = procoreHelper.ValidateRequestStructure(body);
    if (!structureValid)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { error = structureError });
        return;
    }

    // Extract Procore context
    var (contextValid, companyId, projectId, commitmentId, view, contextError) = 
        procoreHelper.ExtractProcoreContext(context);
    
    if (!contextValid)
    {
        response.StatusCode = 400;
        await response.WriteAsJsonAsync(new { error = contextError });
        return;
    }

    Console.WriteLine("📥 /api/send received");
    Console.WriteLine($"Company: {companyId}, Project: {projectId}, Commitment: {commitmentId}");


    try
    {
        using var httpClient = new HttpClient();
        var exportUrl = procoreHelper.BuildExportUrl(companyId!, projectId!, commitmentId!);

        // Start PDF export
        var exportStarted = await procoreHelper.StartPdfExport(httpClient, exportUrl, companyId!);
        if (!exportStarted)
        {
            response.StatusCode = 500;
            await response.WriteAsJsonAsync(new { error = "Failed to start PDF export" });
            return;
        }

        // Poll for PDF completion
        var pdfBytes = await procoreHelper.PollForPdf(httpClient, exportUrl, companyId!);
        if (pdfBytes == null)
        {
            response.StatusCode = 500;
            await response.WriteAsJsonAsync(new { error = "PDF export failed or timed out" });
            return;
        }

        // Convert to base64
        var pdfBase64 = Convert.ToBase64String(pdfBytes);
        Console.WriteLine("📤 Sending PDF to SigniFlow...");

        // (SigniFlow integration goes here)

        response.StatusCode = 200;
        await response.WriteAsJsonAsync(new
        {
            success = true,
            pdfSize = pdfBytes.Length
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Error exporting PDF:");
        Console.WriteLine(ex);

        response.StatusCode = 500;
        await response.WriteAsJsonAsync(new { error = "Error exporting PDF" });
    }
});

app.Run();

// ------------------
// Models
// ------------------

class OAuthSession
{
    public ProcoreSession Procore { get; set; } = new();
    public SigniflowSession Signiflow { get; set; } = new();
}

class ProcoreSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
    public long? CompanyId { get; set; }
    public long? UserId { get; set; }
}

class SigniflowSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
}
