// ============================================================
// FILE: Configuration/AppConfig.cs
// ============================================================
public static class AppConfig
{
    public static string? ClientId => Environment.GetEnvironmentVariable("PROCORE_CLIENT_ID");
    public static string? ClientSecret => Environment.GetEnvironmentVariable("PROCORE_CLIENT_SECRET");
    public const string ProcoreApiBase = "https://sandbox.procore.com";
    public const string RedirectUri = "https://signiflow-procore-backend-net.onrender.com/oauth/callback";
    public const int RetryLimit = 7;
}

// ============================================================
// END FILE: Configuration/AppConfig.cs
// ============================================================