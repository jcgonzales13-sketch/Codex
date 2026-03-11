using ERP.BuildingBlocks;

namespace ERP.Modules.Depositos;

public enum StatusDeposito
{
    Ativo,
    Inativo
}

public sealed class Deposito
{
    public Deposito(Guid empresaId, string codigo, string nome)
    {
        if (empresaId == Guid.Empty)
        {
            throw new DomainException("Deposito deve estar vinculado a uma empresa valida.");
        }

        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new DomainException("Codigo do deposito e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do deposito e obrigatorio.");
        }

        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        Codigo = codigo.Trim().ToUpperInvariant();
        Nome = nome.Trim();
    }

    public Guid Id { get; }
    public Guid EmpresaId { get; }
    public string Codigo { get; }
    public string Nome { get; private set; }
    public StatusDeposito Status { get; private set; } = StatusDeposito.Ativo;

    public void AtualizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do deposito e obrigatorio.");
        }

        Nome = nome.Trim();
    }

    public void Inativar()
    {
        Status = StatusDeposito.Inativo;
    }

    public void Ativar()
    {
        Status = StatusDeposito.Ativo;
    }

    public void GarantirQuePodeOperar()
    {
        if (Status != StatusDeposito.Ativo)
        {
            throw new DomainException("Deposito inativo nao pode ser utilizado em operacoes.");
        }
    }
}

public interface IDepositoRepository
{
    bool CodigoJaExiste(Guid empresaId, string codigo);
    void Add(Deposito deposito);
}

public sealed class CadastroDepositoService(IDepositoRepository repository)
{
    public Deposito Cadastrar(Guid empresaId, string codigo, string nome)
    {
        if (repository.CodigoJaExiste(empresaId, codigo))
        {
            throw new DomainException("Ja existe deposito cadastrado com este codigo na empresa.");
        }

        var deposito = new Deposito(empresaId, codigo, nome);
        repository.Add(deposito);
        return deposito;
    }
}
