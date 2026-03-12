using ERP.Modules.Fornecedores;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class FornecedorStoreRepository(IErpStore store) : IFornecedorRepository
{
    public bool DocumentoJaExiste(Guid empresaId, string documento)
    {
        return store.Fornecedores.Values.Any(item =>
            item.EmpresaId == empresaId &&
            string.Equals(item.Documento, documento.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Fornecedor fornecedor)
    {
        store.Fornecedores[fornecedor.Id] = fornecedor;
    }
}
