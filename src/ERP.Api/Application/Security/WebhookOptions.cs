namespace ERP.Api.Application.Security;

public sealed class WebhookOptions
{
    public const string SectionName = "WebhookSecurity";
    public const string DefaultSignatureHeaderName = "X-Webhook-Signature";

    public string SharedSecret { get; set; } = string.Empty;
    public string SignatureHeaderName { get; set; } = DefaultSignatureHeaderName;
}
