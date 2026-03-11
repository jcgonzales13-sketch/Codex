using ERP.BuildingBlocks;
using ERP.Modules.Depositos;

namespace ERP.Modules.Depositos.UnitTests;

public sealed class DepositoTests
{
    [Fact]
    public void Deve_impedir_codigo_duplicado_na_mesma_empresa()
    {
        var repository = new FakeDepositoRepository(existeCodigo: true);
        var service = new CadastroDepositoService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Cadastrar(Guid.NewGuid(), "DEP-01", "Deposito Principal"));

        Assert.Equal("Ja existe deposito cadastrado com este codigo na empresa.", exception.Message);
    }

    [Fact]
    public void Deve_impedir_operacao_em_deposito_inativo()
    {
        var deposito = new Deposito(Guid.NewGuid(), "DEP-01", "Deposito Principal");
        deposito.Inativar();

        var exception = Assert.Throws<DomainException>(() => deposito.GarantirQuePodeOperar());

        Assert.Equal("Deposito inativo nao pode ser utilizado em operacoes.", exception.Message);
    }

    private sealed class FakeDepositoRepository(bool existeCodigo) : IDepositoRepository
    {
        public bool CodigoJaExiste(Guid empresaId, string codigo) => existeCodigo;

        public void Add(Deposito deposito)
        {
        }
    }
}
