using ERP.Api.Application.Contracts;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace ERP.Api.Application.Security;

public sealed class WebhookAccessService(
    WebhookSignatureService webhookSignatureService,
    JwtTokenService jwtTokenService)
{
    public void ValidateOrThrow(HttpContext httpContext, ProcessarWebhookRequest request, ErpApplicationService service)
    {
        if (TryResolveAuthenticatedSession(httpContext, out var sessionToken))
        {
            service.ValidarAcesso(sessionToken, IdentityPermissions.IntegracoesManage, null);
            return;
        }

        if (!webhookSignatureService.IsConfigured())
        {
            throw new UnauthorizedAccessException("Webhook externo exige assinatura configurada no servidor.");
        }

        if (!httpContext.Request.Headers.TryGetValue(webhookSignatureService.SignatureHeaderName, out var signature))
        {
            throw new UnauthorizedAccessException($"Header {webhookSignatureService.SignatureHeaderName} e obrigatorio para webhooks externos.");
        }

        if (!webhookSignatureService.IsValid(signature.ToString(), request))
        {
            throw new UnauthorizedAccessException("Assinatura do webhook invalida.");
        }
    }

    private bool TryResolveAuthenticatedSession(HttpContext httpContext, out string sessionToken)
    {
        if (TryResolveBearerToken(httpContext.Request.Headers, out sessionToken))
        {
            return true;
        }

        if (httpContext.Request.Headers.TryGetValue("X-Session-Token", out var sessionHeader) &&
            !StringValues.IsNullOrEmpty(sessionHeader))
        {
            sessionToken = sessionHeader.ToString();
            return true;
        }

        sessionToken = string.Empty;
        return false;
    }

    private bool TryResolveBearerToken(IHeaderDictionary headers, out string sessionToken)
    {
        if (headers.TryGetValue(HeaderNames.Authorization, out var authHeaderValues))
        {
            var bearer = authHeaderValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(bearer) && bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                sessionToken = jwtTokenService.ExtractSessionToken(bearer["Bearer ".Length..].Trim());
                return true;
            }
        }

        sessionToken = string.Empty;
        return false;
    }
}
