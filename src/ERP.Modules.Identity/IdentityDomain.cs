using ERP.BuildingBlocks;
using System.Security.Cryptography;
using System.Text;

namespace ERP.Modules.Identity;

public enum StatusUsuario
{
    PendenteAtivacao,
    Ativo,
    Bloqueado
}

public sealed class Usuario
{
    private readonly HashSet<string> _permissoes = [];

    public Usuario(Guid empresaId, string email, string nome)
    {
        if (empresaId == Guid.Empty)
        {
            throw new DomainException("Usuario deve estar vinculado a uma empresa valida.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new DomainException("Email do usuario e invalido.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do usuario e obrigatorio.");
        }

        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        Email = email.Trim().ToLowerInvariant();
        Nome = nome.Trim();
    }

    public Guid Id { get; }
    public Guid EmpresaId { get; }
    public string Email { get; }
    public string Nome { get; }
    public StatusUsuario Status { get; private set; } = StatusUsuario.PendenteAtivacao;
    public DateTimeOffset? UltimoBloqueioEm { get; private set; }
    public IReadOnlyCollection<string> Permissoes => _permissoes;
    public bool PossuiSenhaConfigurada => !string.IsNullOrWhiteSpace(PasswordHash);
    public string? PasswordHash { get; private set; }

    public void Ativar()
    {
        if (Status == StatusUsuario.Bloqueado)
        {
            throw new DomainException("Usuario bloqueado nao pode ser ativado sem desbloqueio.");
        }

        Status = StatusUsuario.Ativo;
    }

    public void Bloquear(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("Motivo do bloqueio e obrigatorio.");
        }

        Status = StatusUsuario.Bloqueado;
        UltimoBloqueioEm = DateTimeOffset.UtcNow;
    }

    public void ConcederPermissao(string permissao)
    {
        if (Status != StatusUsuario.Ativo)
        {
            throw new DomainException("Somente usuario ativo pode receber permissao.");
        }

        if (string.IsNullOrWhiteSpace(permissao))
        {
            throw new DomainException("Permissao informada e invalida.");
        }

        _permissoes.Add(permissao.Trim().ToUpperInvariant());
    }

    public void DefinirSenha(string senha, bool ativarUsuario)
    {
        if (string.IsNullOrWhiteSpace(senha) || senha.Trim().Length < 8)
        {
            throw new DomainException("Senha deve possuir ao menos 8 caracteres.");
        }

        PasswordHash = HashPassword(senha.Trim());
        if (ativarUsuario && Status == StatusUsuario.PendenteAtivacao)
        {
            Status = StatusUsuario.Ativo;
        }
    }

    public bool ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(PasswordHash) || string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        return VerifyPassword(senha.Trim(), PasswordHash);
    }

    private static string HashPassword(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string senha, string passwordHash)
    {
        var parts = passwordHash.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), salt, 100_000, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}

public interface IUsuarioRepository
{
    bool EmailJaExiste(Guid empresaId, string email);
    void Add(Usuario usuario);
}

public sealed class CadastroUsuarioService(IUsuarioRepository repository)
{
    public Usuario Cadastrar(Guid empresaId, string email, string nome)
    {
        if (repository.EmailJaExiste(empresaId, email))
        {
            throw new DomainException("Ja existe usuario cadastrado com este email na empresa.");
        }

        var usuario = new Usuario(empresaId, email, nome);
        repository.Add(usuario);
        return usuario;
    }
}
