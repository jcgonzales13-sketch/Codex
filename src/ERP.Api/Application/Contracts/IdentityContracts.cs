namespace ERP.Api.Application.Contracts;

public sealed record CreateUsuarioRequest(Guid EmpresaId, string Email, string Nome);
public sealed record BloquearUsuarioRequest(string Motivo);
public sealed record ConcederPermissaoRequest(string Permissao);
public sealed record DefinirSenhaUsuarioRequest(string Senha, bool AtivarUsuario = true);
public sealed record LoginRequest(Guid EmpresaId, string Email, string Senha);
public sealed record LogoutRequest(string Token);
public sealed record ConsultarSessaoRequest(string Token);
public sealed record ConsultarUsuariosRequest(Guid? EmpresaId, string? Status, string? Termo, int Page = 1, int PageSize = 20);
public sealed record PermissaoResponse(string Codigo);
public sealed record UsuarioResponse(Guid Id, Guid EmpresaId, string Email, string Nome, string Status, DateTimeOffset? UltimoBloqueioEm, bool PossuiSenhaConfigurada, IReadOnlyCollection<string> Permissoes);
public sealed record SessaoAutenticacaoResponse(string Token, DateTimeOffset ExpiresAt, UsuarioResponse Usuario);
