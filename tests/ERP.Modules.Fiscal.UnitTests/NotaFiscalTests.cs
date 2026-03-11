using ERP.BuildingBlocks;
using ERP.Modules.Fiscal;

namespace ERP.Modules.Fiscal.UnitTests;

public sealed class NotaFiscalTests
{
    [Fact]
    public void Nao_deve_permitir_emissao_sem_cliente_valido_e_itens()
    {
        var exception = Assert.Throws<DomainException>(() =>
            new NotaFiscal(Guid.NewGuid(), Guid.Empty, []));

        Assert.Equal("Nota fiscal requer cliente valido e ao menos um item.", exception.Message);
    }

    [Fact]
    public void Deve_registrar_rejeicao_sem_baixar_estoque()
    {
        var nota = CriarNota();

        nota.RegistrarRejeicao("539", "Duplicidade de NF-e");

        Assert.Equal(StatusNotaFiscal.Rejeitada, nota.Status);
        Assert.False(nota.EstoqueBaixado);
        Assert.Equal("539", nota.CodigoRejeicao);
        Assert.Single(nota.HistoricoTentativas);
    }

    [Fact]
    public void Deve_cancelar_com_justificativa_sem_apagar_historico()
    {
        var nota = CriarNota();
        nota.Autorizar();
        nota.RegistrarRejeicao("100", "Tentativa anterior");

        nota.Cancelar("Erro de emissao", estornarImpactosOperacionais: true);

        Assert.Equal(StatusNotaFiscal.Cancelada, nota.Status);
        Assert.Equal("Erro de emissao", nota.JustificativaCancelamento);
        Assert.NotEmpty(nota.HistoricoTentativas);
    }

    private static NotaFiscal CriarNota()
    {
        return new NotaFiscal(Guid.NewGuid(), Guid.NewGuid(), [new ItemNotaFiscal(Guid.NewGuid(), 1m, "12345678", "5102")]);
    }
}
