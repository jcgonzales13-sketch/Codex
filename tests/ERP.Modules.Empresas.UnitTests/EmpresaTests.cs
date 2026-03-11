using ERP.BuildingBlocks;
using ERP.Modules.Empresas;

namespace ERP.Modules.Empresas.UnitTests;

public sealed class EmpresaTests
{
    [Fact]
    public void Deve_impedir_documento_duplicado()
    {
        var repository = new FakeEmpresaRepository(existeDocumento: true);
        var service = new CadastroEmpresaService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Cadastrar("12345678000199", "Empresa Teste", "Empresa Teste LTDA"));

        Assert.Equal("Ja existe empresa cadastrada com este documento.", exception.Message);
    }

    [Fact]
    public void Deve_impedir_operacao_quando_empresa_estiver_inativa()
    {
        var empresa = new Empresa("12345678000199", "Empresa Teste", "Empresa Teste LTDA");
        empresa.Inativar();

        var exception = Assert.Throws<DomainException>(() => empresa.GarantirQuePodeOperar());

        Assert.Equal("Empresa inativa ou bloqueada nao pode operar.", exception.Message);
    }

    private sealed class FakeEmpresaRepository(bool existeDocumento) : IEmpresaRepository
    {
        public bool DocumentoJaExiste(string documento) => existeDocumento;

        public void Add(Empresa empresa)
        {
        }
    }
}
