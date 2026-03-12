using System.Security.Cryptography;
using System.Text;
using ERP.Api.Application.Contracts;
using Microsoft.Extensions.Options;

namespace ERP.Api.Application.Security;

public sealed class WebhookSignatureService(IOptions<WebhookOptions> options)
{
    private readonly WebhookOptions _options = options.Value;

    public string SignatureHeaderName => string.IsNullOrWhiteSpace(_options.SignatureHeaderName)
        ? WebhookOptions.DefaultSignatureHeaderName
        : _options.SignatureHeaderName;

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.SharedSecret);
    }

    public string GenerateSignature(ProcessarWebhookRequest request)
    {
        var secret = _options.SharedSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Webhook shared secret nao configurado.");
        }

        var payload = BuildPayload(request);
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(bytes)).ToLowerInvariant();
    }

    public bool IsValid(string? signature, ProcessarWebhookRequest request)
    {
        if (!IsConfigured() || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var expected = GenerateSignature(request);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static string BuildPayload(ProcessarWebhookRequest request)
    {
        return $"{request.EventoId}:{request.Origem}:{request.Payload}";
    }
}
