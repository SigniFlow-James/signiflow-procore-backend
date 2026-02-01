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
}

/// <summary>
/// Procore User Model
/// </summary>

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(WorkOrderCommitment), "WorkOrderContract")]
[JsonDerivedType(typeof(PurchaseOrderCommitment), "PurchaseOrderContract")]
public abstract class CommitmentBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;
}

public class WorkOrderCommitment : CommitmentBase
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }
}

public class PurchaseOrderCommitment : CommitmentBase
{
    [JsonPropertyName("status")]
    public required string Status { get; set; }
}

public class ProcoreUser
{
    [JsonPropertyName("address")]
    public required string Address { get; set; }

    [JsonPropertyName("avatar")]
    public required string Avatar { get; set; }

    [JsonPropertyName("business_id")]
    public required string BusinessId { get; set; }

    [JsonPropertyName("business_phone")]
    public required string BusinessPhone { get; set; }

    [JsonPropertyName("business_phone_extension")]
    public required string BusinessPhoneExtension { get; set; }

    [JsonPropertyName("city")]
    public required string City { get; set; }

    [JsonPropertyName("country_code")]
    public required string CountryCode { get; set; }

    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; set; }

    [JsonPropertyName("email_signature")]
    public required string EmailSignature { get; set; }

    [JsonPropertyName("employee_id")]
    public required string EmployeeId { get; set; }

    [JsonPropertyName("erp_integrated_accountant")]
    public bool ErpIntegratedAccountant { get; set; }

    [JsonPropertyName("fax_number")]
    public required string FaxNumber { get; set; }

    [JsonPropertyName("first_name")]
    public required string FirstNames { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("initials")]
    public required string Initials { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("is_employee")]
    public bool IsEmployee { get; set; }

    [JsonPropertyName("job_title")]
    public required string JobTitle { get; set; }

    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [JsonPropertyName("last_name")]
    public required string LastName { get; set; }

    [JsonPropertyName("mobile_phone")]
    public required string MobilePhone { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("notes")]
    public required string Notes { get; set; }

    [JsonPropertyName("state_code")]
    public required string StateCode { get; set; }

    [JsonPropertyName("welcome_email_sent_at")]
    public DateTime? WelcomeEmailSentAt { get; set; }

    [JsonPropertyName("zip")]
    public required string Zip { get; set; }

    [JsonPropertyName("origin_id")]
    public required string OriginId { get; set; }

    [JsonPropertyName("origin_data")]
    public required string OriginData { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("vendor")]
    public required VendorFromUser Vendor { get; set; }

    [JsonPropertyName("contact_id")]
    public int? ContactId { get; set; }

    [JsonPropertyName("work_classification_id")]
    public int? WorkClassificationId { get; set; }

    [JsonPropertyName("permission_template")]
    public required PermissionTemplate PermissionTemplate { get; set; }

    [JsonPropertyName("company_permission_template")]
    public required PermissionTemplate CompanyPermissionTemplate { get; set; }
}

public class VendorFromUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("is_connected")]
    public bool IsConnected { get; set; }

    [JsonPropertyName("origin_id")]
    public string OriginId { get; set; } = default!;

    [JsonPropertyName("business_register_id")]
    public string BusinessRegisterId { get; set; } = default!;
}

public class PermissionTemplate
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("project_specific")]
    public bool ProjectSpecific { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

public class ProcoreRecipient
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("employee_id")]
    public required string EmployeeId { get; set; }

    [JsonPropertyName("job_title")]
    public required string JobTitle { get; set; }

    [JsonPropertyName("first_name")]
    public required string FirstNames { get; set; }
    
    [JsonPropertyName("last_name")]
    public required string LastName { get; set; }

    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; set; }
}

/// <summary>
/// Procore Vendor Model
/// </summary>

public class RawProcoreCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("logo_url")]
    public required string LogoUrl { get; set; }

    [JsonPropertyName("pcn_business_experience")]
    public bool? PcnBusinessExperience { get; set; }

    [JsonPropertyName("my_company")]
    public bool MyCompany { get; set; }
}

public class ProcoreCompany
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

// Projects
public class RawProcoreProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("address")]
    public required ProjectAddress Address { get; set; }

    [JsonPropertyName("stage_name")]
    public required string StageName { get; set; }

    [JsonPropertyName("status_name")]
    public required string StatusName { get; set; }

    [JsonPropertyName("type_name")]
    public required string TypeName { get; set; }

    [JsonPropertyName("open_items")]
    public required List<OpenItem> OpenItems { get; set; }

    [JsonPropertyName("created_by")]
    public required ProjectCreatedBy CreatedBy { get; set; }
}

// Sub classes for nested objects
public class ProjectAddress
{
    [JsonPropertyName("street")]
    public required string Street { get; set; }

    [JsonPropertyName("city")]
    public required string City { get; set; }

    [JsonPropertyName("state_code")]
    public required string StateCode { get; set; }

    [JsonPropertyName("zip")]
    public required string Zip { get; set; }

    [JsonPropertyName("country_code")]
    public required string CountryCode { get; set; }
}

public class OpenItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("details")]
    public required string Details { get; set; }

    [JsonPropertyName("host")]
    public required string Host { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

public class ProjectCreatedBy
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("email")]
    public required string Email { get; set; }
}

public class ProcoreProject
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

/// <summary>
/// Commitment Metadata Model
/// </summary>

public class ProcoreContext
{
    [JsonPropertyName("company_id")]
    public string CompanyId { get; set; } = "";

    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = "";

    [JsonPropertyName("object_id")]
    public string CommitmentId { get; set; } = "";

    [JsonPropertyName("route")]
    public string CommitmentType { get; set; } = "";
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
    public string? etag { get; set; } = null;
}

public class CreateUploadResponse
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("segments")]
    public List<UploadSegmentResponse> Segments { get; set; } = new();
}

public class UploadSegmentResponse
{
    [JsonPropertyName("etag")]
    public string Etag { get; set; } = default!;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = default!;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("headers")]
    public UploadSegmentHeaders Headers { get; set; } = default!;
}


public class UploadSegmentHeaders
{
    [JsonPropertyName("x-amz-content-sha256")]
    public string XAmzContentSha256 { get; set; } = default!;

    [JsonPropertyName("content-length")]
    public string ContentLength { get; set; } = default!;

    [JsonPropertyName("content-md5")]
    public string ContentMd5 { get; set; } = default!;
}



public class DocumentPayload
{
    [JsonPropertyName("parent_id")]
    public long ParentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_tracked")]
    public bool IsTracked { get; set; }

    [JsonPropertyName("explicit_permissions")]
    public bool ExplicitPermissions { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("unique_name")]
    public bool UniqueName { get; set; }

    [JsonPropertyName("upload_uuid")]
    public string UploadUuid { get; set; } = string.Empty;

    /// <summary>
    /// Dynamic Procore custom fields:
    /// key = "custom_field_%{custom_field_definition_id}"
    /// value = string | number | bool
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? CustomFields { get; set; }
}


public class DocumentFolderPayload
{
    public string? name;
    public string? parent_id;
}

public class DocumentFolder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("is_tracked")]
    public bool IsTracked { get; set; }

    [JsonPropertyName("tracked_folder")]
    public object? TrackedFolder { get; set; }

    [JsonPropertyName("name_with_path")]
    public string? NameWithPath { get; set; }

    [JsonPropertyName("folders")]
    public List<DocumentFolder>? Folders { get; set; }

    [JsonPropertyName("files")]
    public List<FileItem>? Files { get; set; }

    [JsonPropertyName("read_only")]
    public bool ReadOnly { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("is_recycle_bin")]
    public bool IsRecycleBin { get; set; }

    [JsonPropertyName("has_children")]
    public bool HasChildren { get; set; }

    [JsonPropertyName("has_children_files")]
    public bool HasChildrenFiles { get; set; }

    [JsonPropertyName("has_children_folders")]
    public bool HasChildrenFolders { get; set; }

    [JsonPropertyName("custom_fields")]
    public Dictionary<string, CustomField>? CustomFields { get; set; }
}

public class FileItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parent_id")]
    public int ParentId { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("checked_out_until")]
    public DateTime? CheckedOutUntil { get; set; }

    [JsonPropertyName("name_with_path")]
    public string? NameWithPath { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("is_tracked")]
    public bool IsTracked { get; set; }

    [JsonPropertyName("tracked_folder")]
    public object? TrackedFolder { get; set; }

    [JsonPropertyName("checked_out_by")]
    public User? CheckedOutBy { get; set; }

    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [JsonPropertyName("file_versions")]
    public List<FileVersion>? FileVersions { get; set; }

    [JsonPropertyName("legacy_id")]
    public int LegacyId { get; set; }

    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("custom_fields")]
    public Dictionary<string, CustomField>? CustomFields { get; set; }
}

public class FileVersion
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("created_by")]
    public User? CreatedBy { get; set; }

    [JsonPropertyName("prostore_file")]
    public ProstoreFile? ProstoreFile { get; set; }

    [JsonPropertyName("file_id")]
    public int FileId { get; set; }
}

public class ProstoreFile
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }
}

public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class CustomField
{
    [JsonPropertyName("data_type")]
    public string? DataType { get; set; }

    // Can be string, number, bool, object, or array
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

public abstract class CommitmentPatchBase
{
    // Core
    public string? Number { get; set; }
    public string? Status { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? Executed { get; set; }
    public bool? Private { get; set; }

    public string? VendorId { get; set; }
    public string? AccountingMethod { get; set; }
    public decimal? RetainagePercent { get; set; }

    // Dates (shared)
    public DateOnly? ApprovalLetterDate { get; set; }
    public DateOnly? ContractDate { get; set; }
    public DateOnly? ExecutionDate { get; set; }
    public DateOnly? IssuedOnDate { get; set; }
    public DateOnly? LetterOfIntentDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }

    // Integration
    public string? OriginCode { get; set; }
    public string? OriginData { get; set; }
    public long? OriginId { get; set; }

    // Billing
    public List<int>? InvoiceContactUserIds { get; set; }

    // Currency
    public decimal? CurrencyExchangeRate { get; set; }
    public string? CurrencyIsoCode { get; set; }

    // Attachments
    public List<string>? Attachments { get; set; }
    public List<int>? DrawingRevisionIds { get; set; }
    public List<int>? FileVersionIds { get; set; }
    public List<int>? FormIds { get; set; }
    public List<int>? ImageIds { get; set; }
    public List<string>? UploadIds { get; set; }

    // Custom fields
    public Dictionary<string, string?>? CustomFields { get; set; }
}

public sealed class WorkOrderPatch : CommitmentPatchBase
{
    public DateOnly? ActualCompletionDate { get; set; }
    public DateOnly? ContractStartDate { get; set; }
    public DateOnly? ContractEstimatedCompletionDate { get; set; }
    public DateOnly? SignedContractReceivedDate { get; set; }

    public string? Inclusions { get; set; }
    public string? Exclusions { get; set; }
}

public sealed class PurchaseOrderPatch : CommitmentPatchBase
{
    public string? AssigneeId { get; set; }

    public DateOnly? DeliveryDate { get; set; }
    public DateOnly? SignedPurchaseOrderReceivedDate { get; set; }

    public string? BillToAddress { get; set; }
    public string? ShipToAddress { get; set; }
    public string? ShipVia { get; set; }
    public string? PaymentTerms { get; set; }
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
    public VendorID Vendor { get; init; } = default!;
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

public sealed class VendorID
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;
}


// ============================================================
// END FILE: Models/ProcoreModels.cs
// ============================================================