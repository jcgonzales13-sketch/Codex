using ERP.Modules.Vendas;

namespace ERP.Api.Application.Storage.Repositories;

public interface IPedidoVendaStoreRepository
{
    IReadOnlyCollection<PedidoVenda> List();
    PedidoVenda? Find(Guid pedidoId);
    void Add(PedidoVenda pedido);
}

public sealed class PedidoVendaStoreRepository(IErpStore store) : IPedidoVendaStoreRepository
{
    public IReadOnlyCollection<PedidoVenda> List() => store.Pedidos.Values.ToArray();

    public PedidoVenda? Find(Guid pedidoId) =>
        store.Pedidos.TryGetValue(pedidoId, out var pedido) ? pedido : null;

    public void Add(PedidoVenda pedido) => store.Pedidos[pedido.Id] = pedido;
}
