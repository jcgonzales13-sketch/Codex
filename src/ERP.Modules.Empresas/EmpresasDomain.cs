using ERP.BuildingBlocks;

namespace ERP.Modules.Empresas;

public enum StatusEmpresa
{
    Ativa,
    Inativa,
    Bloqueada
}

public sealed class Empresa
{
    public Empresa(string documento, string nomeFantasia, string razaoSocial)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new DomainException("Documento da empresa e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(nomeFantasia))
        {
            throw new DomainException("Nome fantasia da empresa e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(razaoSocial))
        {
            throw new DomainException("Razao social da empresa e obrigatoria.");
        }

        Id = Guid.NewGuid();
        Documento = documento.Trim().ToUpperInvariant();
        NomeFantasia = nomeFantasia.Trim();
        RazaoSocial = razaoSocial.Trim();
    }

    public Guid Id { get; }
    public string Documento { get; }
    public string NomeFantasia { get; private set; }
    public string RazaoSocial { get; private set; }
    public StatusEmpresa Status { get; private set; } = StatusEmpresa.Ativa;

    public void AtualizarCadastro(string nomeFantasia, string razaoSocial)
    {
        if (string.IsNullOrWhiteSpace(nomeFantasia))
        {
            throw new DomainException("Nome fantasia da empresa e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(razaoSocial))
        {
            throw new DomainException("Razao social da empresa e obrigatoria.");
        }

        NomeFantasia = nomeFantasia.Trim();
        RazaoSocial = razaoSocial.Trim();
    }

    public void Inativar()
    {
        Status = StatusEmpresa.Inativa;
    }

    public void Ativar()
    {
        Status = StatusEmpresa.Ativa;
    }

    public void Bloquear()
    {
        Status = StatusEmpresa.Bloqueada;
    }

    public void GarantirQuePodeOperar()
    {
        if (Status != StatusEmpresa.Ativa)
        {
            throw new DomainException("Empresa inativa ou bloqueada nao pode operar.");
        }
    }
}

public interface IEmpresaRepository
{
    bool DocumentoJaExiste(string documento);
    void Add(Empresa empresa);
}

public sealed class CadastroEmpresaService(IEmpresaRepository repository)
{
    public Empresa Cadastrar(string documento, string nomeFantasia, string razaoSocial)
    {
        if (repository.DocumentoJaExiste(documento))
        {
            throw new DomainException("Ja existe empresa cadastrada com este documento.");
        }

        var empresa = new Empresa(documento, nomeFantasia, razaoSocial);
        repository.Add(empresa);
        return empresa;
    }
}
