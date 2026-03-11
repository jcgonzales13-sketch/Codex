namespace ERP.Api.Application.Storage;

public sealed record ImportacaoNotaEntradaRegistro(
    Guid EmpresaId,
    Guid FornecedorId,
    Guid DepositoId,
    string ChaveAcesso,
    bool ImportadaComSucesso,
    int ItensExternos,
    int ItensPendentesConciliacao,
    int MovimentosGerados,
    DateTimeOffset ProcessadaEm);

public sealed record WebhookProcessadoRegistro(
    string EventoId,
    string Origem,
    string Status,
    string Mensagem,
    DateTimeOffset ProcessadoEm);
