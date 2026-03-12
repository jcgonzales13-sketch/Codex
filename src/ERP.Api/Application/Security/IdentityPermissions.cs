namespace ERP.Api.Application.Security;

public static class IdentityPermissions
{
    public sealed record DefaultProfile(string Nome, IReadOnlyCollection<string> Permissoes);

    public const string Admin = "ADMIN";
    public const string EmpresasManage = "EMPRESAS_MANAGE";
    public const string CatalogoManage = "CATALOGO_MANAGE";
    public const string ClientesManage = "CLIENTES_MANAGE";
    public const string FornecedoresManage = "FORNECEDORES_MANAGE";
    public const string DepositosManage = "DEPOSITOS_MANAGE";
    public const string IdentityManage = "IDENTITY_MANAGE";
    public const string EstoqueManage = "ESTOQUE_MANAGE";
    public const string VendasManage = "VENDAS_MANAGE";
    public const string ComprasManage = "COMPRAS_MANAGE";
    public const string FiscalManage = "FISCAL_MANAGE";
    public const string IntegracoesManage = "INTEGRACOES_MANAGE";

    public static IReadOnlyCollection<string> All { get; } =
    [
        Admin,
        EmpresasManage,
        CatalogoManage,
        ClientesManage,
        FornecedoresManage,
        DepositosManage,
        IdentityManage,
        EstoqueManage,
        VendasManage,
        ComprasManage,
        FiscalManage,
        IntegracoesManage
    ];

    public static IReadOnlyCollection<DefaultProfile> DefaultProfiles { get; } =
    [
        new("Administrador", [Admin]),
        new("Operador de Estoque", [EstoqueManage]),
        new("Compras", [ComprasManage, FornecedoresManage]),
        new("Vendas", [VendasManage, ClientesManage]),
        new("Fiscal", [FiscalManage]),
        new("Catalogo", [CatalogoManage]),
        new("Identity", [IdentityManage]),
        new("Integracoes", [IntegracoesManage])
    ];

    public static string Normalize(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        return permission.Trim().ToUpperInvariant();
    }

    public static bool IsKnown(string permission) => All.Contains(Normalize(permission), StringComparer.Ordinal);
}
