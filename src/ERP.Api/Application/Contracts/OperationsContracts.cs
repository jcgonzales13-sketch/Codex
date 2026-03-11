namespace ERP.Api.Application.Contracts;

public sealed record StorageStatusResponse(
    string Provider,
    string? FilePath,
    int Produtos,
    int Clientes,
    int Usuarios,
    int Pedidos,
    int NotasFiscais,
    int Saldos,
    int ChavesImportadas,
    int EventosWebhook);
