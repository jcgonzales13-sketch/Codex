using ERP.BuildingBlocks;
using ERP.Modules.Fornecedores;

namespace ERP.Modules.Fornecedores.UnitTests;

public sealed class FornecedorTests
{
    [Fact]
    public void Deve_bloquear_fornecedor_com_motivo()
    {
        var fornecedor = new Fornecedor(Guid.NewGuid(), "12345678000199", "Fornecedor Teste", "fornecedor@teste.com");

        fornecedor.Bloquear("Pendencia cadastral");

        Assert.Equal(StatusFornecedor.Bloqueado, fornecedor.Status);
        Assert.NotNull(fornecedor.UltimoBloqueioEm);
    }

    [Fact]
    public void Nao_deve_permitir_fornecedor_bloqueado_operar()
    {
        var fornecedor = new Fornecedor(Guid.NewGuid(), "12345678000198", "Fornecedor Bloqueado", "bloqueado@teste.com");
        fornecedor.Bloquear("Restricao");

        var exception = Assert.Throws<DomainException>(() => fornecedor.GarantirQuePodeFornecer());

        Assert.Equal("Fornecedor inativo ou bloqueado nao pode operar.", exception.Message);
    }

    [Fact]
    public void Deve_atualizar_nome_e_email_do_fornecedor()
    {
        var fornecedor = new Fornecedor(Guid.NewGuid(), "12345678000197", "Fornecedor Antigo", "antigo@teste.com");

        fornecedor.AtualizarCadastro("Fornecedor Novo", "novo@teste.com");

        Assert.Equal("Fornecedor Novo", fornecedor.Nome);
        Assert.Equal("novo@teste.com", fornecedor.Email);
    }
}
