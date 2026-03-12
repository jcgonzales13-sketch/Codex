namespace ERP.Api.Application.Contracts;

public sealed record CreateFornecedorRequest(Guid EmpresaId, string Documento, string Nome, string? Email);
public sealed record AtualizarFornecedorRequest(string Nome, string? Email);
public sealed record AtualizarFornecedorParcialRequest(string? Nome, string? Email);
public sealed record BloquearFornecedorRequest(string Motivo);
public sealed record ConsultarFornecedoresRequest(Guid? EmpresaId, string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record FornecedorResponse(Guid Id, Guid EmpresaId, string Documento, string Nome, string? Email, string Status, DateTimeOffset? UltimoBloqueioEm);
