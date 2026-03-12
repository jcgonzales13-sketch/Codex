using System.Text.Json.Serialization;

namespace ERP.Api.IntegrationTests;

public sealed record ApiEnvelope<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("error")] ApiErrorEnvelope? Error);

public sealed record ApiErrorEnvelope(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);
