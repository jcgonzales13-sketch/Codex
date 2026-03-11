using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Health;
using ERP.Api.Application.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
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
builder.Services.AddSingleton<ErpApplicationService>();

var app = builder.Build();

app.UseErpExceptionHandling();
app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(ApiResponses.Ok(new
{
    application = "ERP.Api",
    status = "online",
    storage = builder.Configuration.GetSection(StorageOptions.SectionName).GetValue<string>("Provider") ?? "InMemory",
    modules = new[] { "Empresas", "Fornecedores", "Catalogo", "Clientes", "Depositos", "Compras", "Estoque", "Vendas", "Fiscal", "Identity", "Integracoes" }
})));

app.MapGet("/health", () => Results.Ok(ApiResponses.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
})));

app.MapGet("/healthz", () => Results.Ok(ApiResponses.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
})));

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
});

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
    new { Name = "Identity", Capability = "Cadastro de usuarios, ativacao e permissoes" },
    new { Name = "Integracoes", Capability = "Processamento idempotente de webhooks" }
})));

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
        store.EventosWebhook.Count))));

app.MapGet("/system/events", (string? type, string? sourceModule, int? page, int? pageSize, ErpApplicationService service) =>
    Results.Ok(ApiResponses.Ok(service.ConsultarEventosIntegracao(new ConsultarEventosIntegracaoRequest(type, sourceModule, page ?? 1, pageSize ?? 20)))));

app.MapErpEndpoints();

app.Run();
