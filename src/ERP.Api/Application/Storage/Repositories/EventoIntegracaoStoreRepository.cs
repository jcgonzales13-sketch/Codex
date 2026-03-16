using ERP.Modules.Integracoes;

namespace ERP.Api.Application.Storage.Repositories;

public sealed class EventoIntegracaoStoreRepository(IErpStore store) : IEventoIntegracaoRepository
{
    public bool EventoJaProcessado(string eventoId) => store.EventosWebhook.Contains(eventoId);

    public void Registrar(string eventoId, string origem) => store.EventosWebhook.Add(eventoId);
}
