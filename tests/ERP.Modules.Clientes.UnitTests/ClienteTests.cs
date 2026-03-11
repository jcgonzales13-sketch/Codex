using ERP.BuildingBlocks;
using ERP.Modules.Clientes;

namespace ERP.Modules.Clientes.UnitTests;

public sealed class ClienteTests
{
    [Fact]
    public void Deve_impedir_documento_duplicado_na_mesma_empresa()
    {
        var repository = new FakeClienteRepository(existeDocumento: true);
        var service = new CadastroClienteService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Cadastrar(Guid.NewGuid(), "12345678901", "Cliente Teste", "cliente@teste.com"));

        Assert.Equal("Ja existe cliente cadastrado com este documento na empresa.", exception.Message);
    }

    [Fact]
    public void Deve_bloquear_cliente_com_motivo_obrigatorio()
    {
        var cliente = new Cliente(Guid.NewGuid(), "12345678901", "Cliente Teste", "cliente@teste.com");

        var exception = Assert.Throws<DomainException>(() => cliente.Bloquear(string.Empty));

        Assert.Equal("Motivo do bloqueio e obrigatorio.", exception.Message);
    }

    [Fact]
    public void Deve_impedir_compra_quando_cliente_nao_estiver_ativo()
    {
        var cliente = new Cliente(Guid.NewGuid(), "12345678901", "Cliente Teste", "cliente@teste.com");
        cliente.Inativar();

        var exception = Assert.Throws<DomainException>(() => cliente.GarantirQuePodeComprar());

        Assert.Equal("Cliente nao esta apto para realizar compras.", exception.Message);
    }

    private sealed class FakeClienteRepository(bool existeDocumento) : IClienteRepository
    {
        public bool DocumentoJaExiste(Guid empresaId, string documento) => existeDocumento;

        public void Add(Cliente cliente)
        {
        }
    }
}
