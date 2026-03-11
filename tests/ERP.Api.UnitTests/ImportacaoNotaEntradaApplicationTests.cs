using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Storage;
using ERP.Modules.Catalogo;

namespace ERP.Api.UnitTests;

public sealed class ImportacaoNotaEntradaApplicationTests
{
    [Fact]
    public void Deve_gerar_entrada_em_estoque_quando_importacao_for_conciliada()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            Guid.NewGuid(),
            "P001",
            "SKU-001",
            "Produto teste",
            TipoProduto.Simples,
            10m,
            5m,
            "12345678",
            "0"));

        var depositoId = Guid.NewGuid();
        var resultado = service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            produto.EmpresaId,
            depositoId,
            "CHAVE-IMPORT-1",
            [new ItemNotaEntradaExternaRequest("EXT-1", "Produto externo", 3m)],
            new Dictionary<string, Guid> { ["EXT-1"] = produto.Id }));

        Assert.True(resultado.ImportadaComSucesso);
        var movimento = Assert.Single(resultado.MovimentosEstoqueGerados);
        Assert.Equal(produto.Id, movimento.ProdutoId);
        Assert.Equal(depositoId, movimento.DepositoId);
        Assert.Equal(3m, movimento.Quantidade);

        var saldo = Assert.Single(service.ListarSaldos());
        Assert.Equal(3m, saldo.SaldoAtual);
        var movimentoEstoque = Assert.Single(service.ListarMovimentosEstoque(new ConsultarMovimentosEstoqueRequest(produto.Id, depositoId)));
        Assert.Equal(3m, movimentoEstoque.Quantidade);
    }

    [Fact]
    public void Nao_deve_gerar_entrada_em_estoque_quando_houver_pendencia_de_conciliacao()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);

        var resultado = service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CHAVE-IMPORT-2",
            [new ItemNotaEntradaExternaRequest("EXT-2", "Produto externo", 2m)],
            new Dictionary<string, Guid>()));

        Assert.False(resultado.ImportadaComSucesso);
        Assert.Empty(resultado.MovimentosEstoqueGerados);
        Assert.Empty(service.ListarSaldos());
    }

    [Fact]
    public void Deve_registrar_eventos_internos_das_operacoes_integradas()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var depositoId = Guid.NewGuid();

        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            empresaId,
            "P003",
            "SKU-003",
            "Produto evento",
            TipoProduto.Simples,
            20m,
            10m,
            "12345678",
            "0"));

        service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresaId,
            depositoId,
            "CHAVE-EVT-1",
            [new ItemNotaEntradaExternaRequest("EXT-3", "Produto evento", 5m)],
            new Dictionary<string, Guid> { ["EXT-3"] = produto.Id }));

        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(clienteId));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 2m, 20m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(depositoId));

        var eventos = service.ListarEventosIntegracao();

        Assert.Contains(eventos, item => item.Type == "catalogo.produto_cadastrado");
        Assert.Contains(eventos, item => item.Type == "compras.nota_importada");
        Assert.Contains(eventos, item => item.Type == "vendas.pedido_reservado");
    }

    [Fact]
    public void Deve_ignorar_autorizacao_repetida_sem_nova_baixa_ou_reprocessamento()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var depositoId = Guid.NewGuid();
        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            empresaId,
            "P002",
            "SKU-002",
            "Produto teste",
            TipoProduto.Simples,
            10m,
            5m,
            "12345678",
            "0"));

        service.CriarSaldo(new CriarSaldoEstoqueRequest(produto.Id, depositoId, 5m, false));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(clienteId));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 2m, 10m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(depositoId));
        var nota = service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            clienteId,
            [new ItemNotaFiscalRequest(produto.Id, 2m, "12345678", "5102")]));

        service.AutorizarNotaFiscal(nota.Id, new AutorizarNotaFiscalRequest(depositoId, "evt-repetido"));
        var segundaResposta = service.AutorizarNotaFiscal(nota.Id, new AutorizarNotaFiscalRequest(depositoId, "evt-repetido"));

        var saldo = Assert.Single(service.ListarSaldos());
        Assert.Equal(3m, saldo.SaldoAtual);
        Assert.Equal(StatusNotaFiscal.Autorizada.ToString(), segundaResposta.Status);
    }

    [Fact]
    public void Deve_reverter_reserva_quando_fluxo_integrado_falhar_no_meio()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var depositoId = Guid.NewGuid();

        var produto1 = service.CadastrarProduto(new CreateProdutoRequest(empresaId, "P010", "SKU-010", "Produto 1", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var produto2 = service.CadastrarProduto(new CreateProdutoRequest(empresaId, "P011", "SKU-011", "Produto 2", TipoProduto.Simples, 20m, 10m, "12345678", "0"));
        service.CriarSaldo(new CriarSaldoEstoqueRequest(produto1.Id, depositoId, 5m, false));

        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(clienteId));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto1.Id, 1m, 10m));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto2.Id, 1m, 20m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));

        var exception = Assert.Throws<DomainException>(() => service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(depositoId)));

        Assert.Equal("Estoque insuficiente para reservar o pedido.", exception.Message);
        var saldo = Assert.Single(service.ListarSaldos());
        Assert.Equal(0m, saldo.Reservado);
        Assert.Equal(5m, saldo.SaldoAtual);
    }

    [Fact]
    public void Deve_aplicar_filtro_e_paginacao_na_consulta_de_produtos()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();

        service.CadastrarProduto(new CreateProdutoRequest(empresaId, "P100", "SKU-100", "Produto Alpha", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        service.CadastrarProduto(new CreateProdutoRequest(empresaId, "P101", "SKU-101", "Produto Beta", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        service.CadastrarProduto(new CreateProdutoRequest(Guid.NewGuid(), "P102", "SKU-102", "Produto Gamma", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        var resultado = service.ConsultarProdutos(new ConsultarProdutosRequest(empresaId, true, "Produto", 1, 2));

        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(2, resultado.PageSize);
        Assert.Equal(2, resultado.TotalItems);
    }

    [Fact]
    public void Deve_cadastrar_e_filtrar_clientes()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();

        var clienteAtivo = service.CadastrarCliente(new CreateClienteRequest(empresaId, "12345678901", "Cliente Alpha", "alpha@empresa.com"));
        var clienteBloqueado = service.CadastrarCliente(new CreateClienteRequest(empresaId, "12345678902", "Cliente Beta", "beta@empresa.com"));
        service.BloquearCliente(clienteBloqueado.Id, new BloquearClienteRequest("Inadimplencia"));

        var resultado = service.ConsultarClientes(new ConsultarClientesRequest(empresaId, "Ativo", "Alpha", 1, 10));

        var cliente = Assert.Single(resultado.Items);
        Assert.Equal(clienteAtivo.Id, cliente.Id);
        Assert.Equal("Ativo", cliente.Status);
    }

    [Fact]
    public void Deve_aplicar_filtro_e_paginacao_na_consulta_de_usuarios()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();

        var usuario1 = service.CadastrarUsuario(new CreateUsuarioRequest(empresaId, "alpha@empresa.com", "Usuario Alpha"));
        var usuario2 = service.CadastrarUsuario(new CreateUsuarioRequest(empresaId, "beta@empresa.com", "Usuario Beta"));
        service.CadastrarUsuario(new CreateUsuarioRequest(Guid.NewGuid(), "gamma@outra.com", "Usuario Gamma"));
        service.AtivarUsuario(usuario1.Id);
        service.AtivarUsuario(usuario2.Id);
        service.BloquearUsuario(usuario2.Id, new BloquearUsuarioRequest("Teste"));

        var resultado = service.ConsultarUsuarios(new ConsultarUsuariosRequest(empresaId, "Ativo", "alpha", 1, 10));

        var usuario = Assert.Single(resultado.Items);
        Assert.Equal("alpha@empresa.com", usuario.Email);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(10, resultado.PageSize);
        Assert.Equal(1, resultado.TotalItems);
    }

    [Fact]
    public void Deve_consultar_historico_de_importacoes_e_webhooks()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaId = Guid.NewGuid();
        var depositoId = Guid.NewGuid();
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresaId, "P200", "SKU-200", "Produto Historico", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresaId,
            depositoId,
            "CHAVE-HIST-1",
            [new ItemNotaEntradaExternaRequest("EXT-H1", "Produto Historico", 1m)],
            new Dictionary<string, Guid> { ["EXT-H1"] = produto.Id }));

        service.ProcessarWebhook(new ProcessarWebhookRequest("evt-hist-1", "marketplace", "{\"pedido\":\"1\"}"));
        service.ProcessarWebhook(new ProcessarWebhookRequest("evt-hist-1", "marketplace", "{\"pedido\":\"1\"}"));

        var importacoes = service.ConsultarImportacoesNotaEntrada(new ConsultarImportacoesNotaEntradaRequest(empresaId, depositoId, true, "CHAVE-HIST", 1, 10));
        var webhooks = service.ConsultarWebhooks(new ConsultarWebhooksRequest("marketplace", "IgnoradoDuplicado", "evt-hist", 1, 10));

        var importacao = Assert.Single(importacoes.Items);
        Assert.Equal("CHAVE-HIST-1", importacao.ChaveAcesso);
        Assert.Equal(1, importacao.MovimentosGerados);

        var webhook = Assert.Single(webhooks.Items);
        Assert.Equal("evt-hist-1", webhook.EventoId);
        Assert.Equal("IgnoradoDuplicado", webhook.Status);
    }
}
