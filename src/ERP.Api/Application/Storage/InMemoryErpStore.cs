using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Depositos;
using ERP.Modules.Empresas;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Vendas;
using ERP.Api.Application.Integration;

namespace ERP.Api.Application.Storage;

public sealed class InMemoryErpStore : IErpStore
{
    public object SyncRoot { get; } = new();
    public Dictionary<Guid, Empresa> Empresas { get; } = [];
    public Dictionary<Guid, Produto> Produtos { get; } = [];
    public Dictionary<Guid, Cliente> Clientes { get; } = [];
    public Dictionary<Guid, Deposito> Depositos { get; } = [];
    public Dictionary<Guid, Usuario> Usuarios { get; } = [];
    public Dictionary<Guid, PedidoVenda> Pedidos { get; } = [];
    public Dictionary<Guid, NotaFiscal> NotasFiscais { get; } = [];
    public Dictionary<(Guid ProdutoId, Guid DepositoId), SaldoEstoque> Saldos { get; } = [];
    public List<MovimentoEstoque> MovimentosEstoque { get; } = [];
    public HashSet<(Guid EmpresaId, string ChaveAcesso)> ChavesImportadas { get; } = [];
    public HashSet<string> EventosWebhook { get; } = [];
    public List<ImportacaoNotaEntradaRegistro> ImportacoesNotaEntrada { get; } = [];
    public List<WebhookProcessadoRegistro> WebhooksProcessados { get; } = [];
    public List<IntegrationEvent> IntegrationEvents { get; } = [];

    public void Persist()
    {
    }
}
