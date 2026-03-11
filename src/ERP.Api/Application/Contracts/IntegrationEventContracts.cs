namespace ERP.Api.Application.Contracts;

public sealed record IntegrationEventResponse(
    Guid Id,
    string Type,
    string SourceModule,
    string AggregateId,
    string Description,
    DateTimeOffset OccurredAt);

public sealed record ConsultarEventosIntegracaoRequest(string? Type, string? SourceModule, int Page = 1, int PageSize = 20);
