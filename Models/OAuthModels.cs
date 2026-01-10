// ============================================================
// FILE: Models/OAuthModels.cs
// ============================================================
public class OAuthSession
{
    public ProcoreSession Procore { get; set; } = new();
    public SigniflowSession Signiflow { get; set; } = new();
}

// OAuth Auth Info (for successful refresh)
public class OAuthInfo
{
    public bool Authenticated { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class OAuthFullInfo
{
    public OAuthInfo? Procore { get; set; }
    public OAuthInfo? Signiflow { get; set; }
    public bool? Authenticated { get; set; }
    public DateTime? NextExpiresAt { get; set; }
    public string? Error { get; set; }
}

// ============================================================
// END FILE: Models/OAuthModels.cs
// ============================================================