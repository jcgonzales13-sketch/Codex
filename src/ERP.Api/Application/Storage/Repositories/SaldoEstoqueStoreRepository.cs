using ERP.Modules.Estoque;

namespace ERP.Api.Application.Storage.Repositories;

public interface ISaldoEstoqueStoreRepository
{
    IReadOnlyCollection<SaldoEstoque> List();
    SaldoEstoque? Find(Guid produtoId, Guid depositoId);
    bool Exists(Guid produtoId, Guid depositoId);
    void Save(SaldoEstoque saldo);
}

public sealed class SaldoEstoqueStoreRepository(IErpStore store) : ISaldoEstoqueStoreRepository
{
    public IReadOnlyCollection<SaldoEstoque> List() => store.Saldos.Values.ToArray();

    public SaldoEstoque? Find(Guid produtoId, Guid depositoId) =>
        store.Saldos.TryGetValue((produtoId, depositoId), out var saldo) ? saldo : null;

    public bool Exists(Guid produtoId, Guid depositoId) => store.Saldos.ContainsKey((produtoId, depositoId));

    public void Save(SaldoEstoque saldo) => store.Saldos[(saldo.ProdutoId, saldo.DepositoId)] = saldo;
}
