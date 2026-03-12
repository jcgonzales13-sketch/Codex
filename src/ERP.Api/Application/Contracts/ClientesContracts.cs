namespace ERP.Api.Application.Contracts;

public sealed record CreateClienteRequest(Guid EmpresaId, string Documento, string Nome, string? Email);
public sealed record AtualizarClienteRequest(string Nome, string? Email);
public sealed record AtualizarClienteParcialRequest(string? Nome, string? Email);
public sealed record BloquearClienteRequest(string Motivo);
public sealed record ConsultarClientesRequest(Guid? EmpresaId, string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record ClienteResponse(Guid Id, Guid EmpresaId, string Documento, string Nome, string? Email, string Status, DateTimeOffset? UltimoBloqueioEm);
