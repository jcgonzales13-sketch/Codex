using ERP.Modules.Catalogo;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class ProdutoStoreRepository(IErpStore store) : IProdutoRepository
{
    public bool SkuJaExiste(Guid empresaId, string sku) =>
        store.Produtos.Values.Any(produto =>
            produto.EmpresaId == empresaId &&
            string.Equals(produto.Sku, sku, StringComparison.OrdinalIgnoreCase));

    public void Add(Produto produto) => store.Produtos[produto.Id] = produto;
}
