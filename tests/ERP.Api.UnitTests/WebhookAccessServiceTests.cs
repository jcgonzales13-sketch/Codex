using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Security;
using ERP.Api.Application.Storage;
using ERP.Modules.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ERP.Api.UnitTests;

public sealed class WebhookAccessServiceTests
{
    [Fact]
    public void Deve_permitir_webhook_assinado_sem_sessao()
    {
        var request = new ProcessarWebhookRequest("evt-100", "marketplace", "{\"pedido\":\"1\"}");
        var signatureService = CreateSignatureService("segredo-webhook");
        var accessService = new WebhookAccessService(signatureService, CreateJwtTokenService());
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[signatureService.SignatureHeaderName] = signatureService.GenerateSignature(request);

        var exception = Record.Exception(() => accessService.ValidateOrThrow(httpContext, request, new ErpApplicationService(new InMemoryErpStore())));

        Assert.Null(exception);
    }

    [Fact]
    public void Nao_deve_permitir_webhook_sem_assinatura_quando_nao_ha_sessao()
    {
        var request = new ProcessarWebhookRequest("evt-101", "marketplace", "{\"pedido\":\"1\"}");
        var accessService = new WebhookAccessService(CreateSignatureService("segredo-webhook"), CreateJwtTokenService());

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            accessService.ValidateOrThrow(new DefaultHttpContext(), request, new ErpApplicationService(new InMemoryErpStore())));

        Assert.Equal("Header X-Webhook-Signature e obrigatorio para webhooks externos.", exception.Message);
    }

    [Fact]
    public void Deve_permitir_webhook_com_sessao_autenticada_sem_assinatura()
    {
        var store = new InMemoryErpStore();
        var applicationService = new ErpApplicationService(store);
        var empresa = applicationService.CadastrarEmpresa(new CreateEmpresaRequest("12345678000999", "Empresa Webhook", "Empresa Webhook LTDA"));
        var usuario = applicationService.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "webhook@empresa.com", "Webhook Admin"));
        applicationService.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123"));
        var sessao = applicationService.Login(new LoginRequest(empresa.Id, "webhook@empresa.com", "Senha@123"));

        var request = new ProcessarWebhookRequest("evt-102", "marketplace", "{\"pedido\":\"1\"}");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Session-Token"] = sessao.Token;
        var accessService = new WebhookAccessService(CreateSignatureService(string.Empty), CreateJwtTokenService());

        var exception = Record.Exception(() => accessService.ValidateOrThrow(httpContext, request, applicationService));

        Assert.Null(exception);
    }

    private static WebhookSignatureService CreateSignatureService(string sharedSecret)
    {
        return new WebhookSignatureService(Options.Create(new WebhookOptions
        {
            SharedSecret = sharedSecret,
            SignatureHeaderName = WebhookOptions.DefaultSignatureHeaderName
        }));
    }

    private static JwtTokenService CreateJwtTokenService()
    {
        return new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "CodexERP.Tests",
            Audience = "CodexERP.Tests.Clients",
            SigningKey = "CHANGE_ME_DEVELOPMENT_ONLY_SIGNING_KEY_123456789",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 30
        }));
    }
}
