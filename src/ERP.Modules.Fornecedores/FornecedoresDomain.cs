using ERP.BuildingBlocks;

namespace ERP.Modules.Fornecedores;

public enum StatusFornecedor
{
    Ativo,
    Inativo,
    Bloqueado
}

public sealed class Fornecedor
{
    public Fornecedor(Guid empresaId, string documento, string nome, string? email)
    {
        if (empresaId == Guid.Empty)
        {
            throw new DomainException("Fornecedor deve estar vinculado a uma empresa valida.");
        }

        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new DomainException("Documento do fornecedor e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do fornecedor e obrigatorio.");
        }

        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        Documento = documento.Trim().ToUpperInvariant();
        Nome = nome.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public Guid Id { get; }
    public Guid EmpresaId { get; }
    public string Documento { get; }
    public string Nome { get; private set; }
    public string? Email { get; private set; }
    public StatusFornecedor Status { get; private set; } = StatusFornecedor.Ativo;
    public DateTimeOffset? UltimoBloqueioEm { get; private set; }

    public void AtualizarCadastro(string nome, string? email)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do fornecedor e obrigatorio.");
        }

        Nome = nome.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void Inativar()
    {
        Status = StatusFornecedor.Inativo;
    }

    public void Ativar()
    {
        Status = StatusFornecedor.Ativo;
    }

    public void Bloquear(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("Motivo do bloqueio e obrigatorio.");
        }

        Status = StatusFornecedor.Bloqueado;
        UltimoBloqueioEm = DateTimeOffset.UtcNow;
    }

    public void GarantirQuePodeFornecer()
    {
        if (Status != StatusFornecedor.Ativo)
        {
            throw new DomainException("Fornecedor inativo ou bloqueado nao pode operar.");
        }
    }
}

public interface IFornecedorRepository
{
    bool DocumentoJaExiste(Guid empresaId, string documento);
    void Add(Fornecedor fornecedor);
}

public sealed class CadastroFornecedorService(IFornecedorRepository repository)
{
    public Fornecedor Cadastrar(Guid empresaId, string documento, string nome, string? email)
    {
        if (repository.DocumentoJaExiste(empresaId, documento))
        {
            throw new DomainException("Ja existe fornecedor cadastrado com este documento na empresa.");
        }

        var fornecedor = new Fornecedor(empresaId, documento, nome, email);
        repository.Add(fornecedor);
        return fornecedor;
    }
}
