namespace ERP.Api.Application.Integration;

public sealed record IntegrationEvent(
    Guid Id,
    string Type,
    string SourceModule,
    string AggregateId,
    string Description,
    DateTimeOffset OccurredAt);
