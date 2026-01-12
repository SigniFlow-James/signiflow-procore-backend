// ============================================================
// FILE: Models/ProcoreModels.cs
// ============================================================
namespace Procore.APIClasses;

public class ProcoreSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
    public long? CompanyId { get; set; }
    public long? UserId { get; set; }
}

public class CreateUploadRequest
{
    public string response_filename { get; set; } = default!;
    public string response_content_type { get; set; } = default!;
    public bool attachment_content_disposition { get; set; } = true;
    public long size { get; set; }
    public List<UploadSegment> segments { get; set; } = new();
}

public class UploadSegment
{
    public long size { get; set; }
    public string sha256 { get; set; } = default!;
    public string md5 { get; set; } = default!;
    public string etag { get; set; } = default!;
}

public class CreateUploadResponse
{
    public string uuid { get; set; } = default!;
    public string url { get; set; } = default!;
    public Dictionary<string, string> fields { get; set; } = new();
}

public class DocumentPayload
{
    public string name { get; set; } = default!;
    public string upload_uuid { get; set; } = default!;
    public long parent_id { get; set; }
}

public class DocumentFolder
{
    public long id { get; set; }
    public string name { get; set; } = default!;
    public long? parent_id { get; set; }
}


// ============================================================
// END FILE: Models/ProcoreModels.cs
// ============================================================