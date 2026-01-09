// ============================================================
// FILE: Models/OAuthSession.cs
// ============================================================
public class OAuthSession
{
    public ProcoreSession Procore { get; set; } = new();
    public SigniflowSession Signiflow { get; set; } = new();
}

// ============================================================
// END FILE: Models/OAuthSession.cs
// ============================================================