// ============================================================
// FILE: Services/SigniflowApiClient.cs
// ============================================================
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FullWorkflowRestAPI.APIClasses;

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
    // Login (credentials resolved internally)
    // ------------------------------------------------------------
    public async Task<LoginResponse> LoginAsync()
    {
        var request = new LoginRequest
        {
            UserNameField = _username,
            PasswordField = _password
        };

        return await PostAsync<LoginResponse>("Login", request);
    }

    // ------------------------------------------------------------
    // Full Workflow
    // ------------------------------------------------------------
    public async Task<FullWorkflowResponse> FullWorkflowAsync(FullWorkflowRequest request)
    {
        return await PostAsync<FullWorkflowResponse>(
            "FullWorkflow",
            request,
            useMicrosoftDateFormat: true);
    }

    // ------------------------------------------------------------
    // Core POST helper
    // ------------------------------------------------------------
    private async Task<T> PostAsync<T>(
        string endpoint,
        object body,
        bool useMicrosoftDateFormat = false)
    {
        var options = useMicrosoftDateFormat
            ? new JsonSerializerOptions(JsonOptions)
            {
                Converters = { new MicrosoftDateTimeConverter() }
            }
            : JsonOptions;

        Console.WriteLine("🔑", body);
        var json = JsonSerializer.Serialize(body, options);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
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
        var millis = long.Parse(value![6..^2]);
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var millis = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        writer.WriteStringValue($"\\/Date({millis})\\/");
    }
}

// ============================================================
// END FILE: Services/SigniflowApiClient.cs
// ============================================================