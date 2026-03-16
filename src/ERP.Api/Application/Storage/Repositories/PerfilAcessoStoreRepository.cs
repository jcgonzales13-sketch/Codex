using ERP.Modules.Identity;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class PerfilAcessoStoreRepository(IErpStore store) : IPerfilAcessoRepository
{
    public bool NomeJaExiste(Guid empresaId, string nome) =>
        store.PerfisAcesso.Values.Any(perfil =>
            perfil.EmpresaId == empresaId &&
            string.Equals(perfil.Nome, nome.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Add(PerfilAcesso perfilAcesso) => store.PerfisAcesso[perfilAcesso.Id] = perfilAcesso;
}
