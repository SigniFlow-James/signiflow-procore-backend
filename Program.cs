using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting();

var app = builder.Build();

app.UseCors("DefaultCorsPolicy");
app.UseHttpsRedirection();

// ------------------
// Environment variables
// ------------------

string? CLIENT_ID = Environment.GetEnvironmentVariable("PROCORE_CLIENT_ID");
string? CLIENT_SECRET = Environment.GetEnvironmentVariable("PROCORE_CLIENT_SECRET");
const string PROCORE_API_BASE = "https://sandbox.procore.com";
const string REDIRECT_URI = "https://signiflow-backend-test.onrender.com/oauth/callback";
const int RETRY_LIMIT = 5;

// ------------------
// In-memory token store (singleton-style)
// ------------------

var oauthSession = new OAuthSession();

bool RequireAuth(HttpResponse response)
{
    if (oauthSession.Procore.AccessToken == null ||
        oauthSession.Procore.ExpiresAt == null ||
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= oauthSession.Procore.ExpiresAt)
    {
        response.StatusCode = StatusCodes.Status401Unauthorized;
        response.ContentType = "application/json";
        response.WriteAsync(JsonSerializer.Serialize(new
        {
            error = "Not authenticated with Procore"
        }));
        return false;
    }

    return true;
}

// ------------------
// Health check
// ------------------

app.MapGet("/", () => Results.Ok("OK"));

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
