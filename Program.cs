// ============================================================
// FILE: Program.cs
// ============================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FullWorkflowRestAPI.APIClasses;

var builder = WebApplication.CreateBuilder(args);


// ------------------------------------------------------------
// Configure services
// ------------------------------------------------------------

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


// ------------------------------------------------------------
// Register singleton services
// ------------------------------------------------------------

builder.Services.AddSingleton<OAuthSession>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ProcoreService>();

// Register SigniFlow services
builder.Services.AddHttpClient<SigniflowApiClient>(client =>
{
    client.BaseAddress = new Uri(AppConfig.SigniflowApiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
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

app.Run();

// ============================================================
// END FILE: Program.cs
// ============================================================