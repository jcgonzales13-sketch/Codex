using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Api.IntegrationTests;

public sealed class HealthAndIdentityIntegrationTests : IClassFixture<ErpApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ErpApiFactory _factory;

    public HealthAndIdentityIntegrationTests(ErpApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Deve_responder_healthz()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.Equal("healthy", envelope.Data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deve_expor_metricas_operacionais()
    {
        using var client = _factory.CreateClient();

        _ = await client.GetAsync("/healthz");
        _ = await client.GetAsync("/modules");

        var response = await client.GetAsync("/system/metrics");

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Data.GetProperty("totalHttpRequests").GetInt64() >= 2);
        Assert.True(envelope.Data.GetProperty("endpoints").GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Deve_expor_diagnostico_de_storage_com_campos_operacionais()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/system/storage");

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("InMemory", envelope!.Data.GetProperty("provider").GetString());
        Assert.True(envelope.Data.GetProperty("persistLegacyStateSnapshot").GetBoolean());
        Assert.Equal(0, envelope.Data.GetProperty("pendingMigrations").GetInt32());
        Assert.Equal(0, envelope.Data.GetProperty("legacyStateRows").GetInt32());
        Assert.Equal(0, envelope.Data.GetProperty("dedicatedTablesWithData").GetInt32());
    }

    [Fact]
    public async Task Deve_expor_metricas_e_modulos_na_superficie_versionada_v1()
    {
        using var client = _factory.CreateClient();

        var modulesResponse = await client.GetAsync("/api/v1/modules");
        var metricsResponse = await client.GetAsync("/api/v1/system/metrics");

        modulesResponse.EnsureSuccessStatusCode();
        metricsResponse.EnsureSuccessStatusCode();

        var modulesEnvelope = await modulesResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        var metricsEnvelope = await metricsResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        Assert.NotNull(modulesEnvelope);
        Assert.NotNull(metricsEnvelope);
        Assert.True(modulesEnvelope!.Data.GetArrayLength() >= 1);
        Assert.True(metricsEnvelope!.Data.GetProperty("totalHttpRequests").GetInt64() >= 2);
    }

    [Fact]
    public async Task Deve_expor_headers_de_paginacao_na_consulta_de_clientes()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        _ = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        var response = await client.GetAsync($"/clientes?empresaId={seeded.EmpresaId}&page=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        Assert.Equal("1", response.Headers.GetValues("X-Page").Single());
        Assert.Equal("10", response.Headers.GetValues("X-Page-Size").Single());
        Assert.True(int.Parse(response.Headers.GetValues("X-Total-Count").Single()) >= 1);
        Assert.True(int.Parse(response.Headers.GetValues("X-Total-Pages").Single()) >= 1);
    }

    [Fact]
    public async Task Deve_atualizar_cliente_via_put_restful()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var clienteId = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/clientes/{clienteId}")
        {
            Content = JsonContent.Create(new
            {
                nome = "Cliente Rest Atualizado",
                email = "cliente.rest@empresa.com"
            })
        };
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Cliente Rest Atualizado", envelope!.Data.GetProperty("nome").GetString());
        Assert.Equal("cliente.rest@empresa.com", envelope.Data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Deve_consultar_empresa_e_cliente_por_id_em_rotas_restful()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var clienteId = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        var empresaResponse = await client.GetAsync($"/empresas/{seeded.EmpresaId}");
        var clienteResponse = await client.GetAsync($"/clientes/{clienteId}");

        empresaResponse.EnsureSuccessStatusCode();
        clienteResponse.EnsureSuccessStatusCode();

        var empresaEnvelope = await empresaResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        var clienteEnvelope = await clienteResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);

        Assert.NotNull(empresaEnvelope);
        Assert.NotNull(clienteEnvelope);
        Assert.Equal(seeded.EmpresaId, empresaEnvelope!.Data.GetProperty("id").GetGuid());
        Assert.Equal(clienteId, clienteEnvelope!.Data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Deve_consultar_cliente_pela_superficie_versionada_v1()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var clienteId = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        var response = await client.GetAsync($"/api/v1/clientes/{clienteId}");

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal(clienteId, envelope!.Data.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Deve_consultar_usuario_autenticado_via_identity_me()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/identity/me");
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal(seeded.Email, envelope!.Data.GetProperty("usuario").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Deve_atualizar_fornecedor_via_put_restful()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var fornecedorId = await CriarFornecedor(client, seeded.EmpresaId, sessionToken);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/fornecedores/{fornecedorId}")
        {
            Content = JsonContent.Create(new
            {
                nome = "Fornecedor Rest Atualizado",
                email = "fornecedor.rest@empresa.com"
            })
        };
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Fornecedor Rest Atualizado", envelope!.Data.GetProperty("nome").GetString());
        Assert.Equal("fornecedor.rest@empresa.com", envelope.Data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Deve_atualizar_cliente_via_patch_restful_preservando_campos_omitidos()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var clienteId = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/clientes/{clienteId}")
        {
            Content = JsonContent.Create(new
            {
                nome = "Cliente Patch"
            })
        };
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Cliente Patch", envelope!.Data.GetProperty("nome").GetString());
        Assert.Equal("cliente.rest.original@empresa.com", envelope.Data.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Deve_atualizar_perfil_via_patch_restful_preservando_permissoes_quando_omitidas()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var perfilId = await CriarPerfil(client, seeded.EmpresaId, sessionToken);

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/identity/perfis/{perfilId}")
        {
            Content = JsonContent.Create(new
            {
                nome = "Perfil Patch"
            })
        };
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Perfil Patch", envelope!.Data.GetProperty("nome").GetString());
        Assert.True(envelope.Data.GetProperty("permissoes").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Deve_inativar_cliente_via_delete_restful()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();
        var sessionToken = await RealizarLogin(client, seeded);
        var clienteId = await CriarCliente(client, seeded.EmpresaId, sessionToken);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/clientes/{clienteId}");
        request.Headers.Add("X-Session-Token", sessionToken);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Inativo", envelope!.Data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deve_realizar_fluxo_de_login_token_e_consulta_de_sessao()
    {
        using var client = _factory.CreateClient();
        var seeded = SeedCompanyAndUser();

        var loginResponse = await client.PostAsJsonAsync("/identity/auth/login", new
        {
            empresaId = seeded.EmpresaId,
            email = seeded.Email,
            senha = seeded.Password
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginEnvelope = await loginResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(loginEnvelope);
        var sessionToken = loginEnvelope!.Data.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(sessionToken));

        var tokenResponse = await client.PostAsJsonAsync("/identity/oauth/token", new
        {
            empresaId = seeded.EmpresaId,
            email = seeded.Email,
            senha = seeded.Password
        });

        tokenResponse.EnsureSuccessStatusCode();
        var tokenEnvelope = await tokenResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(tokenEnvelope);
        var accessToken = tokenEnvelope!.Data.GetProperty("accessToken").GetString();
        var currentSessionToken = tokenEnvelope.Data.GetProperty("sessao").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(currentSessionToken));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/identity/auth/sessao")
        {
            Content = JsonContent.Create(new { token = currentSessionToken })
        };
        request.Headers.Authorization = new("Bearer", accessToken);

        var sessionResponse = await client.SendAsync(request);

        sessionResponse.EnsureSuccessStatusCode();
        var sessionEnvelope = await sessionResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(sessionEnvelope);
        Assert.Equal(seeded.Email, sessionEnvelope!.Data.GetProperty("usuario").GetProperty("email").GetString());
    }

    [Fact]
    public async Task Deve_processar_webhook_assinado_sem_sessao()
    {
        using var client = _factory.CreateClient();
        var payload = new ProcessarWebhookRequest("evt-int-1", "marketplace", "{\"pedido\":\"123\"}");
        var signature = CreateWebhookSignature(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/integracoes/webhooks")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(WebhookOptions.DefaultSignatureHeaderName, signature);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("Processado", envelope!.Data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Deve_rejeitar_webhook_sem_assinatura_e_sem_sessao()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/integracoes/webhooks", new
        {
            eventoId = "evt-int-2",
            origem = "marketplace",
            payload = "{\"pedido\":\"123\"}"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal("unauthorized", envelope.Error!.Code);
    }

    private SeededIdentity SeedCompanyAndUser()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ErpApplicationService>();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest(GenerateNumericDocument(), $"Empresa Integracao {suffix}", $"Empresa Integracao {suffix} LTDA"));
        var email = $"integracao.{suffix}@empresa.com";
        var usuario = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, email, $"Usuario Integracao {suffix}"));
        service.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123"));
        return new SeededIdentity(empresa.Id, email, "Senha@123");
    }

    private static async Task<string> RealizarLogin(HttpClient client, SeededIdentity seeded)
    {
        var loginResponse = await client.PostAsJsonAsync("/identity/auth/login", new
        {
            empresaId = seeded.EmpresaId,
            email = seeded.Email,
            senha = seeded.Password
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginEnvelope = await loginResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        return loginEnvelope!.Data.GetProperty("token").GetString()!;
    }

    private static async Task<Guid> CriarCliente(HttpClient client, Guid empresaId, string sessionToken)
    {
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/clientes")
        {
            Content = JsonContent.Create(new
            {
                empresaId,
                documento = GenerateNumericDocument(),
                nome = "Cliente Rest",
                email = "cliente.rest.original@empresa.com"
            })
        };
        createRequest.Headers.Add("X-Session-Token", sessionToken);

        var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        return createEnvelope!.Data.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CriarFornecedor(HttpClient client, Guid empresaId, string sessionToken)
    {
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/fornecedores")
        {
            Content = JsonContent.Create(new
            {
                empresaId,
                documento = GenerateNumericDocument(),
                nome = "Fornecedor Rest",
                email = "fornecedor.rest.original@empresa.com"
            })
        };
        createRequest.Headers.Add("X-Session-Token", sessionToken);

        var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        return createEnvelope!.Data.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CriarPerfil(HttpClient client, Guid empresaId, string sessionToken)
    {
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/identity/perfis")
        {
            Content = JsonContent.Create(new
            {
                empresaId,
                nome = $"Perfil {Guid.NewGuid():N}"[..18],
                permissoes = new[]
                {
                    IdentityPermissions.ClientesManage
                }
            })
        };
        createRequest.Headers.Add("X-Session-Token", sessionToken);

        var createResponse = await client.SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createEnvelope = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<JsonElement>>(JsonOptions);
        return createEnvelope!.Data.GetProperty("id").GetGuid();
    }

    private static string GenerateNumericDocument()
    {
        var digits = DateTime.UtcNow.Ticks.ToString()[^10..];
        return $"1234{digits}";
    }

    private string CreateWebhookSignature(ProcessarWebhookRequest request)
    {
        using var scope = _factory.Services.CreateScope();
        var signatureService = scope.ServiceProvider.GetRequiredService<WebhookSignatureService>();
        return signatureService.GenerateSignature(request);
    }

    private sealed record SeededIdentity(Guid EmpresaId, string Email, string Password);
}
