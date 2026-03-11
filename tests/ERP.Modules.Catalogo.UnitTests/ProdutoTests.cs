using ERP.BuildingBlocks;
using ERP.Modules.Catalogo;

namespace ERP.Modules.Catalogo.UnitTests;

public sealed class ProdutoTests
{
    [Fact]
    public void Deve_impedir_cadastro_com_sku_duplicado_na_mesma_empresa()
    {
        var repository = new FakeProdutoRepository(existeSku: true);
        var service = new CadastroProdutoService(repository);

        var exception = Assert.Throws<DomainException>(() =>
            service.Cadastrar(Guid.NewGuid(), "P001", "SKU-001", "Produto", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        Assert.Equal("SKU ja cadastrado para a empresa.", exception.Message);
    }

    [Fact]
    public void Deve_impedir_venda_de_produto_inativo_sem_permissao_especial()
    {
        var produto = CriarProduto();
        produto.Inativar();

        var exception = Assert.Throws<DomainException>(() => produto.GarantirQuePodeSerVendido(possuiPermissaoEspecial: false));

        Assert.Equal("Produto inativo nao pode ser vendido sem permissao especial.", exception.Message);
    }

    [Fact]
    public void Deve_gerar_auditoria_ao_alterar_campos_fiscais_sensiveis()
    {
        var produto = CriarProduto();

        produto.AtualizarDadosFiscais("87654321", "1");

        var auditoria = Assert.Single(produto.AuditoriasFiscais);
        Assert.Equal("Fiscal", auditoria.Field);
        Assert.Equal("NCM=12345678;Origem=0", auditoria.PreviousValue);
        Assert.Equal("NCM=87654321;Origem=1", auditoria.CurrentValue);
    }

    [Fact]
    public void Deve_permitir_variacao_com_sku_proprio()
    {
        var produto = CriarProduto();

        produto.AdicionarVariacao("SKU-001-AZUL-P", "7890000000011", 12m);

        var variacao = Assert.Single(produto.Variacoes);
        Assert.Equal("SKU-001-AZUL-P", variacao.Sku);
        Assert.Equal("7890000000011", variacao.CodigoBarras);
        Assert.Equal(12m, variacao.PrecoVenda);
    }

    private static Produto CriarProduto()
    {
        return new Produto(Guid.NewGuid(), "P001", "SKU-001", "Produto", TipoProduto.Simples, 10m, 5m, "12345678", "0");
    }

    private sealed class FakeProdutoRepository(bool existeSku) : IProdutoRepository
    {
        public bool AddCalled { get; private set; }

        public bool SkuJaExiste(Guid empresaId, string sku) => existeSku;

        public void Add(Produto produto) => AddCalled = true;
    }
}
