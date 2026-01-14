// ============================================================
// FILE: Models/ProcoreModels.cs
// ============================================================
using System.Text.Json.Serialization;

namespace Procore.APIClasses;

public class ProcoreSession
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public long? ExpiresAt { get; set; }
    // public long? CompanyId { get; set; }
    // public long? UserId { get; set; }
}

public class CommitmentMetadata
{
    public string CompanyId { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string CommitmentId { get; set; } = "";
    public string IntegrationType { get; set; } = "";
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
    public string? parent_id { get; set; }
}

public class DocumentFolder
{
    public string id { get; set; } = default!;
    public string name { get; set; } = default!;
    public string? parent_id { get; set; }
}

public class CommitmentContractPatch
{
    public string? Number { get; set; }
    public string? Status { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? Executed { get; set; }

    public string? VendorId { get; set; }
    public string? AssigneeId { get; set; }

    public bool? SignatureRequired { get; set; }
    public string? BillingScheduleOfValuesStatus { get; set; }

    public string? Inclusions { get; set; }
    public string? Exclusions { get; set; }

    public string? BillToAddress { get; set; }
    public string? ShipToAddress { get; set; }
    public string? ShipVia { get; set; }
    public string? PaymentTerms { get; set; }

    public decimal? RetainagePercent { get; set; }
    public string? AccountingMethod { get; set; }

    public bool? AllowComments { get; set; }
    public bool? AllowMarkups { get; set; }

    public string? ChangeOrderLevelOfDetail { get; set; }

    public bool? EnableSsov { get; set; }
    public bool? AllowPaymentApplications { get; set; }
    public bool? AllowPayments { get; set; }

    public bool? DisplayMaterialsRetainage { get; set; }
    public bool? DisplayWorkRetainage { get; set; }
    public bool? ShowCostCodeOnPdf { get; set; }

    public bool? SsrEnabled { get; set; }
    public bool? Private { get; set; }
    public bool? ShowLineItemsToNonAdmins { get; set; }

    public List<string>? BillRecipientIds { get; set; }
    public List<string>? AccessorIds { get; set; }

    public DateOnly? ActualCompletionDate { get; set; }
    public DateOnly? ApprovalLetterDate { get; set; }
    public DateOnly? ContractDate { get; set; }
    public DateOnly? ContractEstimatedCompletionDate { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public DateOnly? ExecutionDate { get; set; }
    public DateOnly? IssuedOnDate { get; set; }
    public DateOnly? LetterOfIntentDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }
    public DateOnly? SignedContractReceivedDate { get; set; }

    public decimal? CurrencyExchangeRate { get; set; }
    public string? CurrencyIsoCode { get; set; }

    public List<int>? ChangeEventAttachmentIds { get; set; }
    public List<string>? AttachmentIds { get; set; }
    public List<string>? DrawingRevisionIds { get; set; }
    public List<string>? FileVersionIds { get; set; }
    public List<string>? FormIds { get; set; }
    public List<string>? ImageIds { get; set; }
    public List<string>? UploadIds { get; set; }
}



// Root
public sealed class WorkOrderContractResponse
{
    [JsonPropertyName("data")]
    public WorkOrderContractData Data { get; init; } = default!;
}

public sealed class WorkOrderContractData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; init; } = "WorkOrderContract";

    [JsonPropertyName("number")]
    public string Number { get; init; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; init; } = default!;

    [JsonPropertyName("executed")]
    public bool Executed { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = default!;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; init; }

    // Procore returns money as strings
    [JsonPropertyName("grand_total")]
    public string GrandTotal { get; init; } = default!;

    [JsonPropertyName("private")]
    public bool Private { get; init; }

    [JsonPropertyName("currency_configuration")]
    public CurrencyConfiguration CurrencyConfiguration { get; init; } = default!;

    [JsonPropertyName("created_by")]
    public CreatedBy CreatedBy { get; init; } = default!;

    [JsonPropertyName("accounting_method")]
    public string AccountingMethod { get; init; } = default!;

    [JsonPropertyName("allow_comments")]
    public bool AllowComments { get; init; }

    [JsonPropertyName("allow_markups")]
    public bool AllowMarkups { get; init; }

    [JsonPropertyName("change_order_level_of_detail")]
    public string ChangeOrderLevelOfDetail { get; init; } = default!;

    [JsonPropertyName("enable_ssov")]
    public bool EnableSsov { get; init; }

    [JsonPropertyName("allow_payment_applications")]
    public bool AllowPaymentApplications { get; init; }

    [JsonPropertyName("allow_payments")]
    public bool AllowPayments { get; init; }

    [JsonPropertyName("retainage_percent")]
    public int RetainagePercent { get; init; }

    [JsonPropertyName("display_materials_retainage")]
    public bool DisplayMaterialsRetainage { get; init; }

    [JsonPropertyName("display_work_retainage")]
    public bool DisplayWorkRetainage { get; init; }

    [JsonPropertyName("show_cost_code_on_pdf")]
    public bool ShowCostCodeOnPdf { get; init; }

    [JsonPropertyName("ssr_enabled")]
    public bool SsrEnabled { get; init; }

    [JsonPropertyName("show_line_items_to_non_admins")]
    public bool ShowLineItemsToNonAdmins { get; init; }

    [JsonPropertyName("accessor_ids")]
    public List<int> AccessorIds { get; init; } = new();

    [JsonPropertyName("bill_recipient_ids")]
    public List<int> BillRecipientIds { get; init; } = new();

    [JsonPropertyName("prostore_file_ids")]
    public List<int> ProstoreFileIds { get; init; } = new();

    [JsonPropertyName("vendor")]
    public Vendor Vendor { get; init; } = default!;
}

public sealed class CurrencyConfiguration
{
    [JsonPropertyName("base_currency_iso_code")]
    public string BaseCurrencyIsoCode { get; init; } = default!;

    [JsonPropertyName("currency_iso_code")]
    public string CurrencyIsoCode { get; init; } = default!;

    [JsonPropertyName("currency_exchange_rate")]
    public string CurrencyExchangeRate { get; init; } = default!;
}

public sealed class CreatedBy
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; init; } = default!;
}

public sealed class Vendor
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;
}


// ============================================================
// END FILE: Models/ProcoreModels.cs
// ============================================================