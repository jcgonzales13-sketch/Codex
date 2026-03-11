namespace ERP.Api.Application.Contracts;

public sealed record StorageStatusResponse(
    string Provider,
    string? FilePath,
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
