using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Vendas;
using ERP.Api.Application.Integration;

namespace ERP.Api.Application.Storage;

public interface IErpStore
{
    object SyncRoot { get; }
    Dictionary<Guid, Produto> Produtos { get; }
    Dictionary<Guid, Cliente> Clientes { get; }
    Dictionary<Guid, Usuario> Usuarios { get; }
    Dictionary<Guid, PedidoVenda> Pedidos { get; }
    Dictionary<Guid, NotaFiscal> NotasFiscais { get; }
    Dictionary<(Guid ProdutoId, Guid DepositoId), SaldoEstoque> Saldos { get; }
    List<MovimentoEstoque> MovimentosEstoque { get; }
    HashSet<(Guid EmpresaId, string ChaveAcesso)> ChavesImportadas { get; }
    HashSet<string> EventosWebhook { get; }
    List<ImportacaoNotaEntradaRegistro> ImportacoesNotaEntrada { get; }
    List<WebhookProcessadoRegistro> WebhooksProcessados { get; }
    List<IntegrationEvent> IntegrationEvents { get; }
    void Persist();
}
