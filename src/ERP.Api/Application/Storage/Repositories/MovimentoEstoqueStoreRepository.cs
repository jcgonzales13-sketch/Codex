using ERP.Modules.Estoque;

namespace ERP.Api.Application.Storage.Repositories;

public interface IMovimentoEstoqueStoreRepository
{
    IReadOnlyCollection<MovimentoEstoque> List();
    void Add(MovimentoEstoque movimento);
}

public sealed class MovimentoEstoqueStoreRepository(IErpStore store) : IMovimentoEstoqueStoreRepository
{
    public IReadOnlyCollection<MovimentoEstoque> List() => store.MovimentosEstoque.ToArray();

    public void Add(MovimentoEstoque movimento) => store.MovimentosEstoque.Add(movimento);
}
