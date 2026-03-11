using ERP.BuildingBlocks;
using ERP.Modules.Compras;

namespace ERP.Modules.Compras.UnitTests;

public sealed class ImportacaoNotaEntradaServiceTests
{
    [Fact]
    public void Nao_deve_processar_xml_duplicado_para_mesma_empresa()
    {
        var repository = new FakeNotaEntradaRepository(chaveJaImportada: true);
        var service = new ImportacaoNotaEntradaService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Importar(Guid.NewGuid(), "CHAVE-1", [], new Dictionary<string, Guid>()));

        Assert.Equal("XML da nota de entrada ja foi importado para esta empresa.", exception.Message);
    }

    [Fact]
    public void Deve_manter_itens_pendentes_quando_nao_houver_conciliacao()
    {
        var repository = new FakeNotaEntradaRepository(chaveJaImportada: false);
        var service = new ImportacaoNotaEntradaService(repository);
        var itens = new[]
        {
            new ItemNotaEntradaExterna("EXT-1", "Produto externo", 2m)
        };

        var resultado = service.Importar(Guid.NewGuid(), "CHAVE-2", itens, new Dictionary<string, Guid>());

        Assert.False(resultado.ImportadaComSucesso);
        var pendente = Assert.Single(resultado.ItensPendentesConciliacao);
        Assert.Equal("EXT-1", pendente.CodigoExterno);
    }

    private sealed class FakeNotaEntradaRepository(bool chaveJaImportada) : INotaEntradaRepository
    {
        public bool ChaveJaImportada(Guid empresaId, string chaveAcesso) => chaveJaImportada;

        public void RegistrarImportacao(Guid empresaId, string chaveAcesso)
        {
        }
    }
}
