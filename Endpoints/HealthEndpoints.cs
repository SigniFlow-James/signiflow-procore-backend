// ============================================================
// FILE: Endpoints/HealthEndpoints.cs
// ============================================================
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Ok("OK"));
    }
}

// ============================================================
// END FILE: Endpoints/HealthEndpoints.cs
// ============================================================