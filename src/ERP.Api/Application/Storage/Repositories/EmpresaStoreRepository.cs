using ERP.Modules.Empresas;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class EmpresaStoreRepository(IErpStore store) : IEmpresaRepository
{
    public bool DocumentoJaExiste(string documento)
    {
        return store.Empresas.Values.Any(item => string.Equals(item.Documento, documento.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Empresa empresa)
    {
        store.Empresas[empresa.Id] = empresa;
    }
}
