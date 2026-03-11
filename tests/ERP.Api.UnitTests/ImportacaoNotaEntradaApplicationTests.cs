using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Storage;
using ERP.BuildingBlocks;
using ERP.Modules.Catalogo;
using ERP.Modules.Fiscal;

namespace ERP.Api.UnitTests;

public sealed class ImportacaoNotaEntradaApplicationTests
{
    [Fact]
    public void Deve_gerar_entrada_em_estoque_quando_importacao_for_conciliada()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000101", "Empresa Principal", "Empresa Principal LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000101", "Fornecedor Principal", "fornecedor1@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-001", "Deposito Principal"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            empresa.Id,
            "P001",
            "SKU-001",
            "Produto teste",
            TipoProduto.Simples,
            10m,
            5m,
            "12345678",
            "0"));

        var resultado = service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            produto.EmpresaId,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-IMPORT-1",
            [new ItemNotaEntradaExternaRequest("EXT-1", "Produto externo", 3m)],
            new Dictionary<string, Guid> { ["EXT-1"] = produto.Id }));

        Assert.True(resultado.ImportadaComSucesso);
        var movimento = Assert.Single(resultado.MovimentosEstoqueGerados);
        Assert.Equal(produto.Id, movimento.ProdutoId);
        Assert.Equal(deposito.Id, movimento.DepositoId);
        Assert.Equal(3m, movimento.Quantidade);

        var saldo = Assert.Single(service.ListarSaldos());
        Assert.Equal(3m, saldo.SaldoAtual);
        var movimentoEstoque = Assert.Single(service.ListarMovimentosEstoque(new ConsultarMovimentosEstoqueRequest(produto.Id, deposito.Id)));
        Assert.Equal(3m, movimentoEstoque.Quantidade);
    }

    [Fact]
    public void Nao_deve_gerar_entrada_em_estoque_quando_houver_pendencia_de_conciliacao()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000119", "Empresa Pendencia", "Empresa Pendencia LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000119", "Fornecedor Pendencia", "fornecedorpendencia@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-007", "Deposito Pendencia"));

        var resultado = service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresa.Id,
            fornecedor.Id,
            deposito.Id,
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
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000102", "Empresa Evento", "Empresa Evento LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000102", "Fornecedor Evento", "fornecedorevento@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-002", "Deposito Evento"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000001", "Cliente Evento", "evento@empresa.com"));

        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            empresa.Id,
            "P003",
            "SKU-003",
            "Produto evento",
            TipoProduto.Simples,
            20m,
            10m,
            "12345678",
            "0"));

        service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresa.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-EVT-1",
            [new ItemNotaEntradaExternaRequest("EXT-3", "Produto evento", 5m)],
            new Dictionary<string, Guid> { ["EXT-3"] = produto.Id }));

        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 2m, 20m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(deposito.Id));

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
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000103", "Empresa Nota", "Empresa Nota LTDA"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-003", "Deposito Nota"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000002", "Cliente Nota", "nota@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(
            empresa.Id,
            "P002",
            "SKU-002",
            "Produto teste",
            TipoProduto.Simples,
            10m,
            5m,
            "12345678",
            "0"));

        service.CriarSaldo(new CriarSaldoEstoqueRequest(produto.Id, deposito.Id, 5m, false));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 2m, 10m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(deposito.Id));
        var nota = service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto.Id, 2m, "12345678", "5102")]));

        service.AutorizarNotaFiscal(nota.Id, new AutorizarNotaFiscalRequest(deposito.Id, "evt-repetido"));
        var segundaResposta = service.AutorizarNotaFiscal(nota.Id, new AutorizarNotaFiscalRequest(deposito.Id, "evt-repetido"));

        var saldo = Assert.Single(service.ListarSaldos());
        Assert.Equal(3m, saldo.SaldoAtual);
        Assert.Equal(StatusNotaFiscal.Autorizada.ToString(), segundaResposta.Status);
    }

    [Fact]
    public void Deve_reverter_reserva_quando_fluxo_integrado_falhar_no_meio()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000104", "Empresa Reserva", "Empresa Reserva LTDA"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-004", "Deposito Reserva"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000003", "Cliente Reserva", "reserva@empresa.com"));

        var produto1 = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P010", "SKU-010", "Produto 1", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var produto2 = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P011", "SKU-011", "Produto 2", TipoProduto.Simples, 20m, 10m, "12345678", "0"));
        service.CriarSaldo(new CriarSaldoEstoqueRequest(produto1.Id, deposito.Id, 5m, false));

        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto1.Id, 1m, 10m));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto2.Id, 1m, 20m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));

        var exception = Assert.Throws<DomainException>(() => service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(deposito.Id)));

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
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000105", "Empresa Produtos", "Empresa Produtos LTDA"));

        service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P100", "SKU-100", "Produto Alpha", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P101", "SKU-101", "Produto Beta", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var outraEmpresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000106", "Empresa Outra", "Empresa Outra LTDA"));
        service.CadastrarProduto(new CreateProdutoRequest(outraEmpresa.Id, "P102", "SKU-102", "Produto Gamma", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        var resultado = service.ConsultarProdutos(new ConsultarProdutosRequest(empresa.Id, true, "Produto", 1, 2));

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
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000107", "Empresa Clientes", "Empresa Clientes LTDA"));

        var clienteAtivo = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "12345678901", "Cliente Alpha", "alpha@empresa.com"));
        var clienteBloqueado = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "12345678902", "Cliente Beta", "beta@empresa.com"));
        service.BloquearCliente(clienteBloqueado.Id, new BloquearClienteRequest("Inadimplencia"));

        var resultado = service.ConsultarClientes(new ConsultarClientesRequest(empresa.Id, "Ativo", "Alpha", 1, 10));

        var cliente = Assert.Single(resultado.Items);
        Assert.Equal(clienteAtivo.Id, cliente.Id);
        Assert.Equal("Ativo", cliente.Status);
    }

    [Fact]
    public void Nao_deve_aprovar_pedido_quando_cliente_cadastrado_estiver_inativo()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000108", "Empresa Inativa", "Empresa Inativa LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000004", "Cliente Inativo", "inativo@empresa.com"));
        service.InativarCliente(cliente.Id);
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P300", "SKU-300", "Produto Cliente", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));

        var exception = Assert.Throws<DomainException>(() => service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true)));

        Assert.Equal("Cliente inativo nao pode ter pedido aprovado.", exception.Message);
    }

    [Fact]
    public void Nao_deve_criar_nota_fiscal_com_cliente_diferente_do_pedido()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000109", "Empresa Pedido", "Empresa Pedido LTDA"));
        var clientePedido = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000005", "Cliente Pedido", "pedido@empresa.com"));
        var clienteNota = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000006", "Cliente Nota", "nota2@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P401A", "SKU-401A", "Produto Cliente Pedido", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(clientePedido.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));

        var exception = Assert.Throws<DomainException>(() => service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            clienteNota.Id,
            [new ItemNotaFiscalRequest(produto.Id, 1m, "12345678", "5102")])));

        Assert.Equal("Cliente informado para a nota fiscal difere do cliente do pedido.", exception.Message);
    }

    [Fact]
    public void Nao_deve_criar_nota_fiscal_com_itens_diferentes_do_pedido()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000110", "Empresa Fiscal", "Empresa Fiscal LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000007", "Cliente Fiscal", "fiscal@empresa.com"));
        var produtoPedido = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P400", "SKU-400", "Produto Pedido", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var produtoNota = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P401", "SKU-401", "Produto Nota", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produtoPedido.Id, 1m, 10m));

        var exception = Assert.Throws<DomainException>(() => service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produtoNota.Id, 1m, "12345678", "5102")])));

        Assert.Equal("Itens da nota fiscal diferem dos itens do pedido.", exception.Message);
    }

    [Fact]
    public void Nao_deve_criar_nota_fiscal_para_pedido_cancelado()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000111", "Empresa Cancelada", "Empresa Cancelada LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000008", "Cliente Cancelado", "cancelado@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P402", "SKU-402", "Produto Cancelado", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.CancelarPedido(pedido.Id, new CancelarPedidoRequest(null, false));

        var exception = Assert.Throws<DomainException>(() => service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto.Id, 1m, "12345678", "5102")])));

        Assert.Equal("Pedido cancelado nao pode gerar nota fiscal.", exception.Message);
    }

    [Fact]
    public void Nao_deve_permitir_segunda_nota_fiscal_ativa_para_o_mesmo_pedido()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000112", "Empresa Duplicada", "Empresa Duplicada LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000009", "Cliente Duplicado", "duplicado@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P403", "SKU-403", "Produto Duplicado", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));
        service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto.Id, 1m, "12345678", "5102")]));

        var exception = Assert.Throws<DomainException>(() => service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto.Id, 1m, "12345678", "5102")])));

        Assert.Equal("Pedido ja possui nota fiscal ativa vinculada.", exception.Message);
    }

    [Fact]
    public void Nao_deve_permitir_alterar_pedido_com_nota_fiscal_vinculada()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000113", "Empresa Vinculada", "Empresa Vinculada LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000010", "Cliente Vinculado", "vinculado@empresa.com"));
        var produto1 = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P404", "SKU-404", "Produto 1", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var produto2 = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P405", "SKU-405", "Produto 2", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto1.Id, 1m, 10m));
        service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto1.Id, 1m, "12345678", "5102")]));

        var exceptionAdicionar = Assert.Throws<DomainException>(() => service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto2.Id, 1m, 10m)));
        var exceptionCancelar = Assert.Throws<DomainException>(() => service.CancelarPedido(pedido.Id, new CancelarPedidoRequest(null, false)));

        Assert.Equal("Pedido com nota fiscal vinculada nao pode ser alterado.", exceptionAdicionar.Message);
        Assert.Equal("Pedido com nota fiscal vinculada nao pode ser alterado.", exceptionCancelar.Message);
    }

    [Fact]
    public void Deve_aplicar_filtro_e_paginacao_na_consulta_de_usuarios()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000114", "Empresa Usuarios", "Empresa Usuarios LTDA"));

        var usuario1 = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "alpha@empresa.com", "Usuario Alpha"));
        var usuario2 = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "beta@empresa.com", "Usuario Beta"));
        var outraEmpresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000115", "Empresa Outra Usuario", "Empresa Outra Usuario LTDA"));
        service.CadastrarUsuario(new CreateUsuarioRequest(outraEmpresa.Id, "gamma@outra.com", "Usuario Gamma"));
        service.AtivarUsuario(usuario1.Id);
        service.AtivarUsuario(usuario2.Id);
        service.BloquearUsuario(usuario2.Id, new BloquearUsuarioRequest("Teste"));

        var resultado = service.ConsultarUsuarios(new ConsultarUsuariosRequest(empresa.Id, "Ativo", "alpha", 1, 10));

        var usuario = Assert.Single(resultado.Items);
        Assert.Equal("alpha@empresa.com", usuario.Email);
        Assert.Equal(1, resultado.Page);
        Assert.Equal(10, resultado.PageSize);
        Assert.Equal(1, resultado.TotalItems);
    }

    [Fact]
    public void Deve_permitir_login_consulta_de_sessao_e_logout()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000131", "Empresa Auth", "Empresa Auth LTDA"));
        var usuario = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "auth@empresa.com", "Usuario Auth"));
        service.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123", true));

        var sessao = service.Login(new LoginRequest(empresa.Id, "auth@empresa.com", "Senha@123"));
        var sessaoConsultada = service.ConsultarSessao(new ConsultarSessaoRequest(sessao.Token));

        Assert.Equal(usuario.Id, sessao.Usuario.Id);
        Assert.Equal(sessao.Token, sessaoConsultada.Token);
        Assert.True(sessao.Usuario.PossuiSenhaConfigurada);

        service.Logout(new LogoutRequest(sessao.Token));

        var exception = Assert.Throws<NotFoundException>(() => service.ConsultarSessao(new ConsultarSessaoRequest(sessao.Token)));
        Assert.Equal("Sessao de autenticacao nao encontrada.", exception.Message);
    }

    [Fact]
    public void Nao_deve_permitir_login_com_credenciais_invalidas()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000132", "Empresa Auth Invalida", "Empresa Auth Invalida LTDA"));
        var usuario = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "invalido@empresa.com", "Usuario Invalido"));
        service.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123", true));

        var exception = Assert.Throws<DomainException>(() => service.Login(new LoginRequest(empresa.Id, "invalido@empresa.com", "Errada@123")));

        Assert.Equal("Credenciais invalidas.", exception.Message);
    }

    [Fact]
    public void Deve_permitir_bootstrap_identity_apenas_quando_empresa_ainda_nao_tem_usuarios()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000133", "Empresa Bootstrap", "Empresa Bootstrap LTDA"));

        Assert.True(service.PermiteBootstrapIdentity(empresa.Id));

        service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "bootstrap@empresa.com", "Usuario Bootstrap"));

        Assert.False(service.PermiteBootstrapIdentity(empresa.Id));
    }

    [Fact]
    public void Nao_deve_validar_acesso_sem_permissao_necessaria()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000134", "Empresa Permissao", "Empresa Permissao LTDA"));
        var usuario = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "permissao@empresa.com", "Usuario Permissao"));
        service.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123", true));
        var sessao = service.Login(new LoginRequest(empresa.Id, "permissao@empresa.com", "Senha@123"));

        var exception = Assert.Throws<ForbiddenException>(() => service.ValidarAcesso(sessao.Token, "ESTOQUE_MANAGE", empresa.Id));

        Assert.Equal("Usuario autenticado nao possui permissao para esta operacao.", exception.Message);
    }

    [Fact]
    public void Nao_deve_validar_acesso_para_outra_empresa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000135", "Empresa Sessao", "Empresa Sessao LTDA"));
        var outraEmpresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000136", "Empresa Outra Sessao", "Empresa Outra Sessao LTDA"));
        var usuario = service.CadastrarUsuario(new CreateUsuarioRequest(empresa.Id, "sessao@empresa.com", "Usuario Sessao"));
        service.DefinirSenhaUsuario(usuario.Id, new DefinirSenhaUsuarioRequest("Senha@123", true));
        service.ConcederPermissao(usuario.Id, new ConcederPermissaoRequest("ESTOQUE_MANAGE"));
        var sessao = service.Login(new LoginRequest(empresa.Id, "sessao@empresa.com", "Senha@123"));

        var exception = Assert.Throws<ForbiddenException>(() => service.ValidarAcesso(sessao.Token, "ESTOQUE_MANAGE", outraEmpresa.Id));

        Assert.Equal("Sessao autenticada nao pertence a empresa informada.", exception.Message);
    }

    [Fact]
    public void Deve_consultar_historico_de_importacoes_e_webhooks()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000116", "Empresa Historico", "Empresa Historico LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000116", "Fornecedor Historico", "fornecedorhistorico@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-005", "Deposito Historico"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P200", "SKU-200", "Produto Historico", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresa.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-HIST-1",
            [new ItemNotaEntradaExternaRequest("EXT-H1", "Produto Historico", 1m)],
            new Dictionary<string, Guid> { ["EXT-H1"] = produto.Id }));

        service.ProcessarWebhook(new ProcessarWebhookRequest("evt-hist-1", "marketplace", "{\"pedido\":\"1\"}"));
        service.ProcessarWebhook(new ProcessarWebhookRequest("evt-hist-1", "marketplace", "{\"pedido\":\"1\"}"));

        var importacoes = service.ConsultarImportacoesNotaEntrada(new ConsultarImportacoesNotaEntradaRequest(empresa.Id, fornecedor.Id, deposito.Id, true, "CHAVE-HIST", 1, 10));
        var webhooks = service.ConsultarWebhooks(new ConsultarWebhooksRequest("marketplace", "IgnoradoDuplicado", "evt-hist", 1, 10));

        var importacao = Assert.Single(importacoes.Items);
        Assert.Equal("CHAVE-HIST-1", importacao.ChaveAcesso);
        Assert.Equal(1, importacao.MovimentosGerados);

        var webhook = Assert.Single(webhooks.Items);
        Assert.Equal("evt-hist-1", webhook.EventoId);
        Assert.Equal("IgnoradoDuplicado", webhook.Status);
    }

    [Fact]
    public void Nao_deve_permitir_operacao_de_estoque_com_deposito_inativo()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000117", "Empresa Deposito", "Empresa Deposito LTDA"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-006", "Deposito Inativo"));
        service.InativarDeposito(deposito.Id);
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "P500", "SKU-500", "Produto Deposito", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        var exception = Assert.Throws<DomainException>(() => service.CriarSaldo(new CriarSaldoEstoqueRequest(produto.Id, deposito.Id, 1m, false)));

        Assert.Equal("Deposito inativo nao pode ser utilizado em operacoes.", exception.Message);
    }

    [Fact]
    public void Nao_deve_cadastrar_cliente_para_empresa_inativa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000118", "Empresa Inativa Cadastro", "Empresa Inativa Cadastro LTDA"));
        service.InativarEmpresa(empresa.Id);

        var exception = Assert.Throws<DomainException>(() => service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000011", "Cliente Bloqueado", "bloqueado@empresa.com")));

        Assert.Equal("Empresa inativa ou bloqueada nao pode operar.", exception.Message);
    }

    [Fact]
    public void Nao_deve_adicionar_produto_de_outra_empresa_ao_pedido()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaPedido = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000120", "Empresa Pedido Cruzado", "Empresa Pedido Cruzado LTDA"));
        var empresaProduto = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000121", "Empresa Produto Cruzado", "Empresa Produto Cruzado LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresaPedido.Id, "10000000012", "Cliente Cruzado", "cruzado@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresaProduto.Id, "P600", "SKU-600", "Produto Cruzado", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));

        var exception = Assert.Throws<DomainException>(() => service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m)));

        Assert.Equal("Produto informado pertence a outra empresa do pedido.", exception.Message);
    }

    [Fact]
    public void Nao_deve_reservar_pedido_em_deposito_de_outra_empresa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaPedido = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000122", "Empresa Pedido Deposito", "Empresa Pedido Deposito LTDA"));
        var empresaDeposito = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000123", "Empresa Deposito Cruzado", "Empresa Deposito Cruzado LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresaPedido.Id, "10000000013", "Cliente Deposito", "deposito@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresaPedido.Id, "P601", "SKU-601", "Produto Pedido", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresaDeposito.Id, "DEP-008", "Deposito Cruzado"));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));

        var exception = Assert.Throws<DomainException>(() => service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(deposito.Id)));

        Assert.Equal("Deposito informado pertence a outra empresa do pedido.", exception.Message);
    }

    [Fact]
    public void Nao_deve_importar_nota_com_produto_conciliado_de_outra_empresa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaImportacao = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000124", "Empresa Importacao", "Empresa Importacao LTDA"));
        var empresaProduto = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000125", "Empresa Produto Importacao", "Empresa Produto Importacao LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresaImportacao.Id, "22000000000124", "Fornecedor Importacao", "fornecedorimportacao@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresaImportacao.Id, "DEP-009", "Deposito Importacao"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresaProduto.Id, "P602", "SKU-602", "Produto Outra Empresa", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        var exception = Assert.Throws<DomainException>(() => service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresaImportacao.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-IMPORT-CRUZADA",
            [new ItemNotaEntradaExternaRequest("EXT-CRUZ", "Produto Outra Empresa", 2m)],
            new Dictionary<string, Guid> { ["EXT-CRUZ"] = produto.Id })));

        Assert.Equal("Produto conciliado pertence a outra empresa.", exception.Message);
    }

    [Fact]
    public void Nao_deve_importar_nota_com_fornecedor_de_outra_empresa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaImportacao = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000128", "Empresa Fornecedor Compra", "Empresa Fornecedor Compra LTDA"));
        var outraEmpresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000129", "Empresa Fornecedor Externo", "Empresa Fornecedor Externo LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(outraEmpresa.Id, "22000000000129", "Fornecedor Externo", "fornecedorexterno@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresaImportacao.Id, "DEP-012", "Deposito Fornecedor"));

        var exception = Assert.Throws<DomainException>(() => service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresaImportacao.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-FORN-CRUZADA",
            [new ItemNotaEntradaExternaRequest("EXT-F1", "Produto", 1m)],
            new Dictionary<string, Guid>())));

        Assert.Equal("Fornecedor informado pertence a outra empresa.", exception.Message);
    }

    [Fact]
    public void Nao_deve_importar_nota_com_fornecedor_inativo()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000130", "Empresa Fornecedor Inativo", "Empresa Fornecedor Inativo LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000130", "Fornecedor Inativo", "fornecedorinativo@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-013", "Deposito Fornecedor Inativo"));
        service.InativarFornecedor(fornecedor.Id);

        var exception = Assert.Throws<DomainException>(() => service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresa.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-FORN-INATIVO",
            [new ItemNotaEntradaExternaRequest("EXT-F2", "Produto", 1m)],
            new Dictionary<string, Guid>())));

        Assert.Equal("Fornecedor inativo ou bloqueado nao pode operar.", exception.Message);
    }

    [Fact]
    public void Nao_deve_autorizar_nota_fiscal_em_deposito_de_outra_empresa()
    {
        var store = new InMemoryErpStore();
        var service = new ErpApplicationService(store);
        var empresaPedido = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000126", "Empresa Nota Deposito", "Empresa Nota Deposito LTDA"));
        var empresaDeposito = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000127", "Empresa Deposito Nota", "Empresa Deposito Nota LTDA"));
        var cliente = service.CadastrarCliente(new CreateClienteRequest(empresaPedido.Id, "10000000014", "Cliente Nota Deposito", "notadeposito@empresa.com"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresaPedido.Id, "P603", "SKU-603", "Produto Nota Deposito", TipoProduto.Simples, 10m, 5m, "12345678", "0"));
        var depositoPedido = service.CadastrarDeposito(new CreateDepositoRequest(empresaPedido.Id, "DEP-010", "Deposito Pedido"));
        var depositoOutro = service.CadastrarDeposito(new CreateDepositoRequest(empresaDeposito.Id, "DEP-011", "Deposito Outro"));
        service.CriarSaldo(new CriarSaldoEstoqueRequest(produto.Id, depositoPedido.Id, 5m, false));
        var pedido = service.CriarPedido(new CreatePedidoVendaRequest(cliente.Id));
        service.AdicionarItemPedido(pedido.Id, new AddItemPedidoRequest(produto.Id, 1m, 10m));
        service.AprovarPedido(pedido.Id, new AprovarPedidoRequest(true));
        service.ReservarPedido(pedido.Id, new ReservarPedidoRequest(depositoPedido.Id));
        var nota = service.CriarNotaFiscal(new CreateNotaFiscalRequest(
            pedido.Id,
            cliente.Id,
            [new ItemNotaFiscalRequest(produto.Id, 1m, "12345678", "5102")]));

        var exception = Assert.Throws<DomainException>(() => service.AutorizarNotaFiscal(nota.Id, new AutorizarNotaFiscalRequest(depositoOutro.Id, "evt-deposito-cruzado")));

        Assert.Equal("Deposito informado pertence a outra empresa da nota fiscal.", exception.Message);
    }
}
