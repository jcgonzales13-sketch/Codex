using ERP.Modules.Catalogo;
using ERP.Modules.Compras;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Integracoes;
using ERP.Modules.Vendas;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    application = "ERP.Api",
    status = "online",
    modules = new[]
    {
        nameof(Produto),
        nameof(ImportacaoNotaEntradaService),
        nameof(SaldoEstoque),
        nameof(PedidoVenda),
        nameof(NotaFiscal),
        nameof(Usuario),
        nameof(ProcessamentoWebhookService)
    }
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
}));

app.MapGet("/modules", () => Results.Ok(new[]
{
    new { Name = "Catalogo", Capability = "Cadastro de produtos, variacoes e auditoria fiscal" },
    new { Name = "Compras", Capability = "Importacao de nota de entrada com conciliacao" },
    new { Name = "Estoque", Capability = "Ajustes, reservas, baixas e transferencias" },
    new { Name = "Vendas", Capability = "Aprovacao e reserva de pedidos" },
    new { Name = "Fiscal", Capability = "Autorizacao, rejeicao e cancelamento de notas" },
    new { Name = "Identity", Capability = "Cadastro de usuarios, ativacao e permissoes" },
    new { Name = "Integracoes", Capability = "Processamento idempotente de webhooks" }
}));

app.Run();
