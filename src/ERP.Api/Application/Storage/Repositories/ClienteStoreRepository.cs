using ERP.Modules.Clientes;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class ClienteStoreRepository(IErpStore store) : IClienteRepository
{
    public bool DocumentoJaExiste(Guid empresaId, string documento)
    {
        return store.Clientes.Values.Any(item =>
            item.EmpresaId == empresaId &&
            string.Equals(item.Documento, documento.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void Add(Cliente cliente)
    {
        store.Clientes[cliente.Id] = cliente;
    }
}
