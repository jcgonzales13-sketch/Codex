using ERP.Modules.Fiscal;

namespace ERP.Api.Application.Storage.Repositories;

public interface INotaFiscalStoreRepository
{
    IReadOnlyCollection<NotaFiscal> List();
    NotaFiscal? Find(Guid notaFiscalId);
    void Add(NotaFiscal notaFiscal);
    bool HasActiveForPedido(Guid pedidoId);
}

public sealed class NotaFiscalStoreRepository(IErpStore store) : INotaFiscalStoreRepository
{
    public IReadOnlyCollection<NotaFiscal> List() => store.NotasFiscais.Values.ToArray();

    public NotaFiscal? Find(Guid notaFiscalId) =>
        store.NotasFiscais.TryGetValue(notaFiscalId, out var notaFiscal) ? notaFiscal : null;

    public void Add(NotaFiscal notaFiscal) => store.NotasFiscais[notaFiscal.Id] = notaFiscal;

    public bool HasActiveForPedido(Guid pedidoId) =>
        store.NotasFiscais.Values.Any(item =>
            item.PedidoVendaId == pedidoId &&
            item.Status != StatusNotaFiscal.Cancelada);
}
