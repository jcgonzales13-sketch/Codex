namespace ERP.Api.Application.Contracts;

public sealed record CreateDepositoRequest(Guid EmpresaId, string Codigo, string Nome);
public sealed record AtualizarDepositoRequest(string Nome);
public sealed record ConsultarDepositosRequest(Guid? EmpresaId, string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record DepositoResponse(Guid Id, Guid EmpresaId, string Codigo, string Nome, string Status);
