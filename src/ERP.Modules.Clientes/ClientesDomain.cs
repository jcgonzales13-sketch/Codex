using ERP.BuildingBlocks;

namespace ERP.Modules.Clientes;

public enum StatusCliente
{
    Ativo,
    Inativo,
    Bloqueado
}

public sealed class Cliente
{
    public Cliente(Guid empresaId, string documento, string nome, string? email)
    {
        if (empresaId == Guid.Empty)
        {
            throw new DomainException("Cliente deve estar vinculado a uma empresa valida.");
        }

        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new DomainException("Documento do cliente e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do cliente e obrigatorio.");
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
    public StatusCliente Status { get; private set; } = StatusCliente.Ativo;
    public DateTimeOffset? UltimoBloqueioEm { get; private set; }

    public void AtualizarCadastro(string nome, string? email)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("Nome do cliente e obrigatorio.");
        }

        Nome = nome.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public void Inativar()
    {
        Status = StatusCliente.Inativo;
    }

    public void Ativar()
    {
        Status = StatusCliente.Ativo;
    }

    public void Bloquear(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("Motivo do bloqueio e obrigatorio.");
        }

        Status = StatusCliente.Bloqueado;
        UltimoBloqueioEm = DateTimeOffset.UtcNow;
    }

    public void GarantirQuePodeComprar()
    {
        if (Status != StatusCliente.Ativo)
        {
            throw new DomainException("Cliente nao esta apto para realizar compras.");
        }
    }
}

public interface IClienteRepository
{
    bool DocumentoJaExiste(Guid empresaId, string documento);
    void Add(Cliente cliente);
}

public sealed class CadastroClienteService(IClienteRepository repository)
{
    public Cliente Cadastrar(Guid empresaId, string documento, string nome, string? email)
    {
        if (repository.DocumentoJaExiste(empresaId, documento))
        {
            throw new DomainException("Ja existe cliente cadastrado com este documento na empresa.");
        }

        var cliente = new Cliente(empresaId, documento, nome, email);
        repository.Add(cliente);
        return cliente;
    }
}
