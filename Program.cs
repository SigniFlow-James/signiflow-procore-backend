// ============================================================
// FILE: Program.cs
// ============================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Signiflow.APIClasses;
using Procore.APIClasses;

var builder = WebApplication.CreateBuilder(args);
var FRONTEND_URL = AppConfig.FrontendUrl ?? throw new InvalidOperationException("FRONTEND_URL environment variable not configured");

Console.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Data exists: {Directory.Exists("/app/data")}");
Console.WriteLine($"Files: {string.Join(", ", Directory.GetFiles("/app/data"))}");


// ------------------------------------------------------------
// Configure services
// ------------------------------------------------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(
                FRONTEND_URL,
                "https://sandbox.procore.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting();


// ------------------------------------------------------------
// Register services
// ------------------------------------------------------------

builder.Services.AddSingleton<OAuthSession>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ISigniflowWebhookQueue, SigniflowWebhookQueue>();
builder.Services.AddSingleton<AdminService>();
builder.Services.AddScoped<SigniflowWebhookProcessor>();
builder.Services.AddHostedService<SigniflowWebhookWorker>();


// Register Procore Services
builder.Services.AddHttpClient<ProcoreApiClient>(client =>
{
    client.BaseAddress = new Uri(AppConfig.ProcoreApiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ProcoreService>();


// Register SigniFlow services
builder.Services.AddHttpClient<SigniflowApiClient>(client =>
{
    client.BaseAddress = new Uri(AppConfig.SigniflowApiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
    );
});
builder.Services.AddScoped<SigniflowService>();

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


// ------------------------------------------------------------
// Map endpoints
// ------------------------------------------------------------

app.MapHealthEndpoints();
app.MapOAuthEndpoints();
app.MapApiEndpoints();
app.MapWebhookEndpoints();
app.MapAdminEndpoints();
app.Run();

// ============================================================
// END FILE: Program.cs
// ============================================================