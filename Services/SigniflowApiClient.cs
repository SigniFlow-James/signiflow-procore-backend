// ============================================================
// FILE: Services/SigniflowApiClient.cs
// ============================================================
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signiflow.APIClasses;

public class SigniflowApiClient
{
    private readonly HttpClient _http;
    private readonly string _username;
    private readonly string _password;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SigniflowApiClient(HttpClient http)
    {
        _http = http;

        _username = AppConfig.SigniflowUsername
            ?? throw new InvalidOperationException("SIGNIFLOW_USERNAME environment variable not configured");

        _password = AppConfig.SigniflowPassword
            ?? throw new InvalidOperationException("SIGNIFLOW_PASSWORD environment variable not configured");
    }

    // ------------------------------------------------------------
    // Login
    // ------------------------------------------------------------
    public async Task<LoginResponse> LoginAsync()
    {
        var request = new LoginRequest
        {
            UserNameField = _username,
            PasswordField = _password
        };
        Console.WriteLine("🔑 Signing in to SigniFlow as " + _username);
        return await PostAsync<LoginResponse>("Login", request);
    }

    // ------------------------------------------------------------
    // Full Workflow
    // ------------------------------------------------------------
    public async Task<FullWorkflowResponse> FullWorkflowAsync(FullWorkflowRequest request)
    {
        return await PostAsync<FullWorkflowResponse>(
            "FullWorkflow",
            request);
    }

    // ------------------------------------------------------------
    // Download Document
    // ------------------------------------------------------------
    public async Task<DownloadResponse> DownloadDocumentAsync(DownloadRequest request)
    {
        return await PostAsync<DownloadResponse>(
            "GetDoc",
            request);
    }

    // ------------------------------------------------------------
    // Core POST helper
    // ------------------------------------------------------------
    public async Task<T> PostAsync<T>(
        string endpoint,
        object body,
        bool useMicrosoftDateFormat = true)
    {
        var options = useMicrosoftDateFormat
            ? new JsonSerializerOptions(JsonOptions)
            {
                Converters = { new MicrosoftDateTimeConverter() },
                WriteIndented = true
            }
            : JsonOptions;

        var json = JsonSerializer.Serialize(body, options);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        Console.WriteLine($"📍 Posting to: {_http.BaseAddress}{endpoint}");
        var response = await _http.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔑 response: {responseJson}");
        return JsonSerializer.Deserialize<T>(responseJson, options)!;
    }
}

// ------------------------------------------------------------
// Microsoft JSON date format: \/Date(123456789)\/
// ------------------------------------------------------------
internal sealed class MicrosoftDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return DateTime.MinValue;
        }

        // Format: \/Date(1768049260000+0000)\/
        // Extract the timestamp between /Date( and +/- or )
        var startIndex = value.IndexOf('(') + 1;
        var endIndex = value.IndexOfAny(['+', '-', ')'], startIndex);

        if (startIndex < 1 || endIndex < 0)
        {
            throw new JsonException($"Invalid date format: {value}");
        }

        var millisString = value.Substring(startIndex, endIndex - startIndex);
        var millis = long.Parse(millisString);

        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var dto = new DateTimeOffset(value);
        var millis = dto.ToUnixTimeMilliseconds();
        var offset = dto.ToString("zzz").Replace(":", "");
        writer.WriteStringValue($"/Date({millis}{offset})/");
    }
}

// ============================================================
// END FILE: Services/SigniflowApiClient.cs
// ============================================================