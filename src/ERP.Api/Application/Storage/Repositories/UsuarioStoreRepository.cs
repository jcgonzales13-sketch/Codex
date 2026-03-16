using ERP.Modules.Identity;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class UsuarioStoreRepository(IErpStore store) : IUsuarioRepository
{
    public bool EmailJaExiste(Guid empresaId, string email) =>
        store.Usuarios.Values.Any(usuario =>
            usuario.EmpresaId == empresaId &&
            string.Equals(usuario.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

    public void Add(Usuario usuario) => store.Usuarios[usuario.Id] = usuario;
}
