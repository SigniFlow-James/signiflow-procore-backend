// ============================================================
// FILE: Configuration/AppConfig.cs
// ============================================================
public static class AppConfig
{
    // Frontend Configuration
    public static string? FrontendUrl => Environment.GetEnvironmentVariable("FRONTEND_URL");

    // Procore Configuration
    public static string? ProcoreClientId => Environment.GetEnvironmentVariable("PROCORE_CLIENT_ID");
    public static string? ProcoreClientSecret => Environment.GetEnvironmentVariable("PROCORE_CLIENT_SECRET");
    public const string ProcoreApiBase = "https://sandbox.procore.com";
    public const string RedirectUri = "https://signiflow-procore-backend-net.onrender.com/oauth/callback";
    public const int RetryLimit = 7; // Number of times to retry document download polling

    // SigniFlow Configuration
    public static string? SigniflowUsername => Environment.GetEnvironmentVariable("SIGNIFLOW_USERNAME");
    public static string? SigniflowPassword => Environment.GetEnvironmentVariable("SIGNIFLOW_PASSWORD");
    public const string SigniflowApiBase = "https://au.signiflow.com/API/SignFlowAPIServiceRest.svc/";
}

// ============================================================
// END FILE: Configuration/AppConfig.cs
// ============================================================