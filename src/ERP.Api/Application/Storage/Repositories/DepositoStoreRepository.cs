using ERP.Modules.Depositos;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class DepositoStoreRepository(IErpStore store) : IDepositoRepository
{
    public bool CodigoJaExiste(Guid empresaId, string codigo) =>
        store.Depositos.Values.Any(deposito =>
            deposito.EmpresaId == empresaId &&
            string.Equals(deposito.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Add(Deposito deposito) => store.Depositos[deposito.Id] = deposito;
}
