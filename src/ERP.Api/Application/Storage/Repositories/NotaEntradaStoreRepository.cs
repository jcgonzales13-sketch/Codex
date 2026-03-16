using ERP.Modules.Compras;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class NotaEntradaStoreRepository(IErpStore store) : INotaEntradaRepository
{
    public bool ChaveJaImportada(Guid empresaId, string chaveAcesso) =>
        store.ChavesImportadas.Contains((empresaId, chaveAcesso));

    public void RegistrarImportacao(Guid empresaId, string chaveAcesso) =>
        store.ChavesImportadas.Add((empresaId, chaveAcesso));
}
