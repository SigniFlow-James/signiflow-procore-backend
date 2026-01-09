// ============================================================
// FILE: Models/ProcoreSession.cs
// ============================================================
public class ProcoreSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
    public long? CompanyId { get; set; }
    public long? UserId { get; set; }
}

// ============================================================
// END FILE: Models/ProcoreSession.cs
// ============================================================