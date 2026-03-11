namespace ERP.Api.Application.Contracts;

public sealed record ProcessarWebhookRequest(string EventoId, string Origem, string Payload);
public sealed record ResultadoWebhookResponse(string Status, string Mensagem);
public sealed record ConsultarWebhooksRequest(string? Origem, string? Status, string? EventoId, int Page = 1, int PageSize = 20);
public sealed record WebhookProcessadoResponse(string EventoId, string Origem, string Status, string Mensagem, DateTimeOffset ProcessadoEm);
