// ============================================================
// FILE: Models/OAuthModels.cs
// ============================================================
public class OAuthSession
{
    public ProcoreSession Procore { get; set; } = new();
    public SigniflowSession Signiflow { get; set; } = new();
}

// OAuth Refresh Response
public class OAuthRefreshResponse
{
    public bool Refreshed { get; set; }
    public bool LoginRequired { get; set; }
    public object Auth { get; set; } = null!;
}

// OAuth Status Response
public class OAuthStatusResponse
{
    public bool ProcoreAuthenticated { get; set; }
    public long? ProcoreExpiresAt { get; set; }
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
}

// ============================================================
// END FILE: Models/OAuthModels.cs
// ============================================================