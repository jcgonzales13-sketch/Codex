using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Health;
using ERP.Api.Application.Logging;
using ERP.Api.Application.Observability;
using ERP.Api.Application.Storage;
using ERP.Api.Application.Storage.Repositories;
using ERP.Api.Application.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Codex ERP API",
        Version = "v1",
        Description = "API modular de ERP com fluxos de empresas, catalogo, compras, estoque, vendas, fiscal, identity e integracoes."
    });

    options.AddSecurityDefinition("SessionToken", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Session-Token",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Token de sessao retornado pelo endpoint de login."
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT retornado pelo endpoint de token."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "SessionToken"
                }
            },
            Array.Empty<string>()
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection(WebhookOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddHealthChecks().AddCheck<StorageHealthCheck>("storage");
builder.Services.AddSingleton<IErpStore>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
    return options.Provider.ToUpperInvariant() switch
    {
        "JSONFILE" => new JsonFileErpStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>()),
        "SQLSERVER" => new SqlServerErpStore(serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>()),
        _ => new InMemoryErpStore()
    };
});
builder.Services.AddSingleton<InMemoryObservabilityCollector>();
builder.Services.AddSingleton<ERP.Modules.Empresas.IEmpresaRepository, EmpresaStoreRepository>();
builder.Services.AddSingleton<ERP.Modules.Clientes.IClienteRepository, ClienteStoreRepository>();
builder.Services.AddSingleton<ERP.Modules.Fornecedores.IFornecedorRepository, FornecedorStoreRepository>();
builder.Services.AddSingleton<ErpApplicationService>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<WebhookSignatureService>();
builder.Services.AddSingleton<WebhookAccessService>();

var app = builder.Build();

app.UseErpExceptionHandling();
app.UseStructuredRequestLogging();
app.UseForwardedHeaders();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Codex ERP API v1");
    options.RoutePrefix = "swagger";
});
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(ApiResponses.Ok(new
{
    application = "ERP.Api",
    status = "online",
    storage = builder.Configuration.GetSection(StorageOptions.SectionName).GetValue<string>("Provider") ?? "InMemory",
    modules = new[] { "Empresas", "Fornecedores", "Catalogo", "Clientes", "Depositos", "Compras", "Estoque", "Vendas", "Fiscal", "Identity", "Integracoes" }
})))
.WithSummary("Estado geral da aplicacao.")
.WithDescription("Retorna status online, provider de storage atual e os modulos carregados pela API. Use este endpoint para confirmar bootstrap, ambiente ativo e disponibilidade basica da aplicacao.")
.Produces(StatusCodes.Status200OK);

app.MapGet("/health", () => Results.Ok(ApiResponses.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
})))
.WithSummary("Health check simples.")
.WithDescription("Endpoint leve para verificar se a API esta respondendo. Nao depende de autenticacao nem de leitura detalhada do storage.")
.Produces(StatusCodes.Status200OK);

app.MapGet("/healthz", () => Results.Ok(ApiResponses.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
})))
.WithSummary("Health check alternativo.")
.WithDescription("Alias de health check usado em monitoramento e deploy. Ideal para probes simples de liveness.")
.Produces(StatusCodes.Status200OK);

app.MapPost("/healthz", () => Results.Ok(ApiResponses.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
})))
.WithSummary("Health check manual via POST.")
.WithDescription("Alias em POST para testes em ferramentas como Postman ou plataformas que validam um endpoint ativo via requisicao manual.")
.Produces(StatusCodes.Status200OK);

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponses.Ok(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        }));
    }
})
.WithSummary("Health check de prontidao.")
.WithDescription("Executa as verificacoes registradas de health check e retorna o detalhamento por dependencia. Use este endpoint para readiness em deploy e monitoramento operacional.");

app.MapGet("/modules", () => Results.Ok(ApiResponses.Ok(new[]
{
    new { Name = "Empresas", Capability = "Cadastro de empresas e validacao do contexto operacional" },
    new { Name = "Fornecedores", Capability = "Cadastro de fornecedores e vinculo operacional com compras" },
    new { Name = "Catalogo", Capability = "Cadastro de produtos, variacoes e auditoria fiscal" },
    new { Name = "Clientes", Capability = "Cadastro de clientes, bloqueio e consulta operacional" },
    new { Name = "Depositos", Capability = "Cadastro de depositos e validacao operacional de armazenagem" },
    new { Name = "Compras", Capability = "Importacao de nota de entrada com conciliacao" },
    new { Name = "Estoque", Capability = "Ajustes, reservas, baixas e transferencias" },
    new { Name = "Vendas", Capability = "Aprovacao e reserva de pedidos" },
    new { Name = "Fiscal", Capability = "Autorizacao, rejeicao e cancelamento de notas" },
    new { Name = "Identity", Capability = "Cadastro de usuarios, perfis de acesso, ativacao e permissoes" },
    new { Name = "Integracoes", Capability = "Processamento idempotente de webhooks" }
})))
.WithSummary("Lista os modulos da aplicacao.")
.WithDescription("Retorna os modulos disponiveis e a capacidade principal de cada um. Serve como inventario funcional rapido da API.")
.Produces(StatusCodes.Status200OK);

app.MapGet("/system/storage", (IErpStore store, Microsoft.Extensions.Options.IOptions<StorageOptions> options) =>
    Results.Ok(ApiResponses.Ok(new StorageStatusResponse(
        options.Value.Provider,
        options.Value.FilePath,
        store.Empresas.Count,
        store.Fornecedores.Count,
        store.Produtos.Count,
        store.Clientes.Count,
        store.Depositos.Count,
        store.Usuarios.Count,
        store.Pedidos.Count,
        store.NotasFiscais.Count,
        store.Saldos.Count,
        store.ChavesImportadas.Count,
        store.EventosWebhook.Count))))
    .WithSummary("Inspeciona o estado do storage.")
    .WithDescription("Retorna o provider configurado e contagens atuais das entidades persistidas. E util para diagnostico rapido de volume e confirmacao do backend de armazenamento em uso.")
    .Produces(StatusCodes.Status200OK);

app.MapGet("/system/events", (string? type, string? sourceModule, int? page, int? pageSize, ErpApplicationService service) =>
    Results.Ok(ApiResponses.Ok(service.ConsultarEventosIntegracao(new ConsultarEventosIntegracaoRequest(type, sourceModule, page ?? 1, pageSize ?? 20)))))
    .WithSummary("Consulta eventos internos de integracao.")
    .WithDescription("Lista eventos gerados pelos fluxos integrados entre modulos, com filtros por tipo e modulo de origem. Use para auditoria funcional, suporte e rastreabilidade de operacoes.")
    .Produces(StatusCodes.Status200OK);

app.MapGet("/system/metrics", (InMemoryObservabilityCollector observabilityCollector) =>
    Results.Ok(ApiResponses.Ok(observabilityCollector.Snapshot())))
    .WithSummary("Consulta metricas e sinais internos da API.")
    .WithDescription("Retorna metricas operacionais em memoria sobre requests HTTP, falhas, operacoes de dominio e excecoes, servindo como base inicial de observabilidade e suporte.")
    .Produces(StatusCodes.Status200OK);

app.MapErpEndpoints();

app.Run();

public partial class Program;
