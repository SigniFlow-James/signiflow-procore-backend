// ============================================================
// FILE: Configuration/AppConfig.cs (UPDATE - Add SigniFlow config)
// ============================================================
public static class AppConfig
{
    // Procore Configuration
    public static string? ProcoreClientId => Environment.GetEnvironmentVariable("PROCORE_CLIENT_ID");
    public static string? ProcoreClientSecret => Environment.GetEnvironmentVariable("PROCORE_CLIENT_SECRET");
    public const string ProcoreApiBase = "https://sandbox.procore.com";
    public const string RedirectUri = "https://signiflow-procore-backend-net.onrender.com/oauth/callback";
    public const int RetryLimit = 7;

    // SigniFlow Configuration
    public static string? SigniflowUsername => Environment.GetEnvironmentVariable("SIGNIFLOW_USERNAME");
    public static string? SigniflowPassword => Environment.GetEnvironmentVariable("SIGNIFLOW_PASSWORD");
    public const string SigniflowApiBase = "https://server.signiflow.com/API/Home/"; // Update with actual base URL
}

// ============================================================
// END FILE: Configuration/AppConfig.cs
// ============================================================