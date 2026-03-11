namespace ERP.Api.Application.Contracts;

public sealed record CreateEmpresaRequest(string Documento, string NomeFantasia, string RazaoSocial);
public sealed record AtualizarEmpresaRequest(string NomeFantasia, string RazaoSocial);
public sealed record ConsultarEmpresasRequest(string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record EmpresaResponse(Guid Id, string Documento, string NomeFantasia, string RazaoSocial, string Status);
