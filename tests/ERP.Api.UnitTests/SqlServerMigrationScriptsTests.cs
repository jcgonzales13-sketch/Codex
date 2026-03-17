using ERP.Api.Application.Storage;

namespace ERP.Api.UnitTests;

public sealed class SqlServerMigrationScriptsTests
{
    [Fact]
    public void Deve_expor_scripts_ordenados_e_versionados()
    {
        Assert.True(SqlServerMigrationScripts.All.Count >= 15);
        Assert.Equal(
            SqlServerMigrationScripts.All.Select(item => item.Id).OrderBy(item => item).ToArray(),
            SqlServerMigrationScripts.All.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Deve_renderizar_placeholders_de_schema_e_tabelas()
    {
        var script = SqlServerMigrationScripts.All.First();

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[MigrationHistory]", rendered);
        Assert.Contains("[erp].[StateStore]", rendered);
        Assert.DoesNotContain("{{", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabelas_dedicadas_de_operacao()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "003");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[MovimentosEstoque]", rendered);
        Assert.Contains("[erp].[IntegrationEvents]", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabelas_dedicadas_de_importacao_e_webhook()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "005");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[ImportacoesNotaEntrada]", rendered);
        Assert.Contains("[erp].[WebhooksProcessados]", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabelas_dedicadas_de_autenticacao()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "007");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[SessoesAutenticacao]", rendered);
        Assert.Contains("[erp].[RefreshTokens]", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabelas_dedicadas_de_vendas_e_fiscal()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "009");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[PedidosVenda]", rendered);
        Assert.Contains("[erp].[NotasFiscais]", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabela_dedicada_de_saldos()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "011");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[SaldosEstoque]", rendered);
    }

    [Fact]
    public void Deve_renderizar_tabelas_dedicadas_de_dados_mestres()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "013");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("[erp].[Empresas]", rendered);
        Assert.Contains("[erp].[Fornecedores]", rendered);
        Assert.Contains("[erp].[Produtos]", rendered);
        Assert.Contains("[erp].[Clientes]", rendered);
        Assert.Contains("[erp].[Depositos]", rendered);
        Assert.Contains("[erp].[Usuarios]", rendered);
        Assert.Contains("[erp].[PerfisAcesso]", rendered);
        Assert.Contains("[erp].[ChavesImportadas]", rendered);
        Assert.Contains("[erp].[EventosWebhook]", rendered);
    }

    [Fact]
    public void Deve_renderizar_foreign_keys_relacionais()
    {
        var script = SqlServerMigrationScripts.All.Single(item => item.Id == "015");

        var rendered = SqlServerMigrationScripts.Render(script, "erp", "StateStore", "MigrationHistory");

        Assert.Contains("FK_Clientes_Empresas_EmpresaId", rendered);
        Assert.Contains("FK_PedidosVenda_Clientes_ClienteId", rendered);
        Assert.Contains("FK_NotasFiscais_PedidosVenda_PedidoVendaId", rendered);
        Assert.Contains("FK_SaldosEstoque_Produtos_ProdutoId", rendered);
        Assert.Contains("FK_RefreshTokens_SessoesAutenticacao_SessionToken", rendered);
    }
}
