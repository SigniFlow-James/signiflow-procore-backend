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
    public long parent_id { get; set; }
}

public class DocumentFolder
{
    public long id { get; set; }
    public string name { get; set; } = default!;
    public long? parent_id { get; set; }
}

public sealed class CommitmentContractRequest
{
    [JsonPropertyName("number")]
    public string? Number { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("executed")]
    public bool? Executed { get; init; }

    [JsonPropertyName("vendor_id")]
    public int? VendorId { get; init; }

    [JsonPropertyName("assignee_id")]
    public int? AssigneeId { get; init; }

    [JsonPropertyName("signature_required")]
    public bool? SignatureRequired { get; init; }

    [JsonPropertyName("billing_schedule_of_values_status")]
    public string? BillingScheduleOfValuesStatus { get; init; }

    [JsonPropertyName("inclusions")]
    public string? Inclusions { get; init; }

    [JsonPropertyName("exclusions")]
    public string? Exclusions { get; init; }

    [JsonPropertyName("bill_to_address")]
    public string? BillToAddress { get; init; }

    [JsonPropertyName("ship_to_address")]
    public string? ShipToAddress { get; init; }

    [JsonPropertyName("ship_via")]
    public string? ShipVia { get; init; }

    [JsonPropertyName("payment_terms")]
    public string? PaymentTerms { get; init; }

    // Procore expects retainage as string
    [JsonPropertyName("retainage_percent")]
    public string? RetainagePercent { get; init; }

    [JsonPropertyName("accounting_method")]
    public string? AccountingMethod { get; init; }

    [JsonPropertyName("allow_comments")]
    public bool? AllowComments { get; init; }

    [JsonPropertyName("allow_markups")]
    public bool? AllowMarkups { get; init; }

    [JsonPropertyName("change_order_level_of_detail")]
    public string? ChangeOrderLevelOfDetail { get; init; }

    [JsonPropertyName("enable_ssov")]
    public bool? EnableSsov { get; init; }

    [JsonPropertyName("allow_payment_applications")]
    public bool? AllowPaymentApplications { get; init; }

    [JsonPropertyName("allow_payments")]
    public bool? AllowPayments { get; init; }

    [JsonPropertyName("display_materials_retainage")]
    public bool? DisplayMaterialsRetainage { get; init; }

    [JsonPropertyName("display_work_retainage")]
    public bool? DisplayWorkRetainage { get; init; }

    [JsonPropertyName("show_cost_code_on_pdf")]
    public bool? ShowCostCodeOnPdf { get; init; }

    [JsonPropertyName("ssr_enabled")]
    public bool? SsrEnabled { get; init; }

    [JsonPropertyName("private")]
    public bool? Private { get; init; }

    [JsonPropertyName("show_line_items_to_non_admins")]
    public bool? ShowLineItemsToNonAdmins { get; init; }

    [JsonPropertyName("bill_recipient_ids")]
    public List<int>? BillRecipientIds { get; init; }

    [JsonPropertyName("accessor_ids")]
    public List<int>? AccessorIds { get; init; }

    // ---- Dates (date-only, not datetime)
    [JsonPropertyName("actual_completion_date")]
    public DateOnly? ActualCompletionDate { get; init; }

    [JsonPropertyName("approval_letter_date")]
    public DateOnly? ApprovalLetterDate { get; init; }

    [JsonPropertyName("contract_date")]
    public DateOnly? ContractDate { get; init; }

    [JsonPropertyName("contract_estimated_completion_date")]
    public DateOnly? ContractEstimatedCompletionDate { get; init; }

    [JsonPropertyName("contract_start_date")]
    public DateOnly? ContractStartDate { get; init; }

    [JsonPropertyName("delivery_date")]
    public DateOnly? DeliveryDate { get; init; }

    [JsonPropertyName("execution_date")]
    public DateOnly? ExecutionDate { get; init; }

    [JsonPropertyName("issued_on_date")]
    public DateOnly? IssuedOnDate { get; init; }

    [JsonPropertyName("letter_of_intent_date")]
    public DateOnly? LetterOfIntentDate { get; init; }

    [JsonPropertyName("returned_date")]
    public DateOnly? ReturnedDate { get; init; }

    [JsonPropertyName("signed_contract_received_date")]
    public DateOnly? SignedContractReceivedDate { get; init; }

    // ---- Currency
    [JsonPropertyName("currency_exchange_rate")]
    public string? CurrencyExchangeRate { get; init; }

    [JsonPropertyName("currency_iso_code")]
    public string? CurrencyIsoCode { get; init; }

    // ---- Attachments
    [JsonPropertyName("change_event_attachment_ids")]
    public List<int>? ChangeEventAttachmentIds { get; init; }

    [JsonPropertyName("attachment_ids")]
    public List<string>? AttachmentIds { get; init; }

    [JsonPropertyName("drawing_revision_ids")]
    public List<string>? DrawingRevisionIds { get; init; }

    [JsonPropertyName("file_version_ids")]
    public List<string>? FileVersionIds { get; init; }

    [JsonPropertyName("form_ids")]
    public List<string>? FormIds { get; init; }

    [JsonPropertyName("image_ids")]
    public List<string>? ImageIds { get; init; }

    [JsonPropertyName("upload_ids")]
    public List<string>? UploadIds { get; init; }

    // ---- Custom fields (dynamic)
    [JsonExtensionData]
    public Dictionary<string, object>? CustomFields { get; init; }
}


// Root
public sealed class WorkOrderContractResponse
{
    [JsonPropertyName("data")]
    public WorkOrderContractData Data { get; init; } = default!;
}

// Data payload
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