using ERP.BuildingBlocks;
using ERP.Modules.Integracoes;

namespace ERP.Modules.Integracoes.UnitTests;

public sealed class ProcessamentoWebhookServiceTests
{
    [Fact]
    public void Deve_ignorar_evento_duplicado()
    {
        var repository = new FakeEventoIntegracaoRepository(eventoJaProcessado: true);
        var service = new ProcessamentoWebhookService(repository);

        var resultado = service.Processar(new WebhookRecebido("evt-1", "marketplace", "{}"));

        Assert.Equal(StatusWebhook.IgnoradoDuplicado, resultado.Status);
        Assert.Equal("Evento ignorado por duplicidade.", resultado.Mensagem);
    }

    [Fact]
    public void Deve_exigir_identificador_do_evento()
    {
        var repository = new FakeEventoIntegracaoRepository(eventoJaProcessado: false);
        var service = new ProcessamentoWebhookService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Processar(new WebhookRecebido(string.Empty, "marketplace", "{}")));

        Assert.Equal("Evento de integracao deve possuir identificador.", exception.Message);
    }

    [Fact]
    public void Deve_registrar_evento_novo()
    {
        var repository = new FakeEventoIntegracaoRepository(eventoJaProcessado: false);
        var service = new ProcessamentoWebhookService(repository);

        var resultado = service.Processar(new WebhookRecebido("evt-2", "marketplace", "{}"));

        Assert.Equal(StatusWebhook.Processado, resultado.Status);
        Assert.True(repository.Registrou);
    }

    private sealed class FakeEventoIntegracaoRepository(bool eventoJaProcessado) : IEventoIntegracaoRepository
    {
        public bool Registrou { get; private set; }

        public bool EventoJaProcessado(string eventoId) => eventoJaProcessado;

        public void Registrar(string eventoId, string origem)
        {
            Registrou = true;
        }
    }
}
