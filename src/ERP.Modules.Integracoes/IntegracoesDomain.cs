using ERP.BuildingBlocks;

namespace ERP.Modules.Integracoes;

public enum StatusWebhook
{
    Recebido,
    Processado,
    IgnoradoDuplicado,
    Falhou
}

public sealed record WebhookRecebido(string EventoId, string Origem, string Payload);

public sealed record ResultadoProcessamentoWebhook(StatusWebhook Status, string Mensagem);

public interface IEventoIntegracaoRepository
{
    bool EventoJaProcessado(string eventoId);
    void Registrar(string eventoId, string origem);
}

public sealed class ProcessamentoWebhookService(IEventoIntegracaoRepository repository)
{
    public ResultadoProcessamentoWebhook Processar(WebhookRecebido webhook)
    {
        if (string.IsNullOrWhiteSpace(webhook.EventoId))
        {
            throw new DomainException("Evento de integracao deve possuir identificador.");
        }

        if (string.IsNullOrWhiteSpace(webhook.Origem))
        {
            throw new DomainException("Origem do webhook e obrigatoria.");
        }

        if (repository.EventoJaProcessado(webhook.EventoId))
        {
            return new ResultadoProcessamentoWebhook(StatusWebhook.IgnoradoDuplicado, "Evento ignorado por duplicidade.");
        }

        repository.Registrar(webhook.EventoId, webhook.Origem);
        return new ResultadoProcessamentoWebhook(StatusWebhook.Processado, "Evento processado com sucesso.");
    }
}
