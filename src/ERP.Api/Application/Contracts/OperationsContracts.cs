namespace ERP.Api.Application.Contracts;

public sealed record StorageStatusResponse(
    string Provider,
    string? FilePath,
    bool PersistLegacyStateSnapshot,
    string? LastAppliedMigrationId,
    int PendingMigrations,
    int LegacyStateRows,
    int DedicatedTablesWithData,
    int Empresas,
    int Fornecedores,
    int Produtos,
    int Clientes,
    int Depositos,
    int Usuarios,
    int Pedidos,
    int NotasFiscais,
    int Saldos,
    int ChavesImportadas,
    int EventosWebhook);

public sealed record ObservabilityMetricsResponse(
    long TotalHttpRequests,
    long TotalHttpFailures,
    long TotalDomainOperations,
    long TotalExceptions,
    DateTimeOffset? LastRequestAt,
    DateTimeOffset? LastDomainOperationAt,
    IReadOnlyCollection<EndpointMetricSnapshot> Endpoints,
    IReadOnlyCollection<OperationMetricSnapshot> Operations);

public sealed record EndpointMetricSnapshot(
    string Method,
    string Path,
    long TotalRequests,
    long TotalFailures,
    int LastStatusCode,
    double AverageDurationMs);

public sealed record OperationMetricSnapshot(
    string Action,
    long Total);
