// ============================================================
// FILE: Endpoints/HealthEndpoints.cs
// ============================================================
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