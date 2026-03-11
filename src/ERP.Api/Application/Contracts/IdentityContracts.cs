namespace ERP.Api.Application.Contracts;

public sealed record CreateUsuarioRequest(Guid EmpresaId, string Email, string Nome);
public sealed record BloquearUsuarioRequest(string Motivo);
public sealed record ConcederPermissaoRequest(string Permissao);
public sealed record ConsultarUsuariosRequest(Guid? EmpresaId, string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record UsuarioResponse(Guid Id, Guid EmpresaId, string Email, string Nome, string Status, DateTimeOffset? UltimoBloqueioEm, IReadOnlyCollection<string> Permissoes);
