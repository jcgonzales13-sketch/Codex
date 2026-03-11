using ERP.BuildingBlocks;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Integration;
using ERP.Api.Application.Storage;
using ERP.Api.Application.Validation;
using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Depositos;
using ERP.Modules.Empresas;
using ERP.Modules.Compras;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Integracoes;
using ERP.Modules.Vendas;

namespace ERP.Api.Application;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ErpApplicationService(IErpStore store)
{
    public PagedResponse<EmpresaResponse> ConsultarEmpresas(ConsultarEmpresasRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Empresas.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<StatusEmpresa>(request.Status, true, out var status))
            {
                query = query.Where(item => item.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Termo))
            {
                var termo = request.Termo.Trim();
                query = query.Where(item =>
                    item.Documento.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.NomeFantasia.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.RazaoSocial.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(query.Select(MapEmpresa), request.Page, request.PageSize);
        }
    }

    public EmpresaResponse CadastrarEmpresa(CreateEmpresaRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Documento, "Documento");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.NomeFantasia, "NomeFantasia");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.RazaoSocial, "RazaoSocial");
            var repository = new EmpresaRepository(store);
            var service = new CadastroEmpresaService(repository);
            var empresa = service.Cadastrar(request.Documento, request.NomeFantasia, request.RazaoSocial);
            AddIntegrationEvent("empresas.empresa_cadastrada", "Empresas", empresa.Id.ToString(), $"Empresa {empresa.Documento} cadastrada.");
            store.Persist();
            return MapEmpresa(empresa);
        }
    }

    public EmpresaResponse AtualizarEmpresa(Guid empresaId, AtualizarEmpresaRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.NomeFantasia, "NomeFantasia");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.RazaoSocial, "RazaoSocial");
            var empresa = GetEmpresa(empresaId);
            empresa.AtualizarCadastro(request.NomeFantasia, request.RazaoSocial);
            store.Persist();
            return MapEmpresa(empresa);
        }
    }

    public EmpresaResponse AtivarEmpresa(Guid empresaId)
    {
        lock (store.SyncRoot)
        {
            var empresa = GetEmpresa(empresaId);
            empresa.Ativar();
            store.Persist();
            return MapEmpresa(empresa);
        }
    }

    public EmpresaResponse InativarEmpresa(Guid empresaId)
    {
        lock (store.SyncRoot)
        {
            var empresa = GetEmpresa(empresaId);
            empresa.Inativar();
            store.Persist();
            return MapEmpresa(empresa);
        }
    }

    public EmpresaResponse BloquearEmpresa(Guid empresaId)
    {
        lock (store.SyncRoot)
        {
            var empresa = GetEmpresa(empresaId);
            empresa.Bloquear();
            store.Persist();
            return MapEmpresa(empresa);
        }
    }

    public IReadOnlyCollection<ProdutoResponse> ListarProdutos()
    {
        lock (store.SyncRoot)
        {
            return store.Produtos.Values.Select(MapProduto).ToArray();
        }
    }

    public PagedResponse<ProdutoResponse> ConsultarProdutos(ConsultarProdutosRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Produtos.Values.AsEnumerable();
            if (request.EmpresaId is not null)
            {
                query = query.Where(item => item.EmpresaId == request.EmpresaId);
            }

            if (request.Ativo is not null)
            {
                query = query.Where(item => item.Ativo == request.Ativo);
            }

            if (!string.IsNullOrWhiteSpace(request.Termo))
            {
                var termo = request.Termo.Trim();
                query = query.Where(item =>
                    item.Sku.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.CodigoInterno.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(query.Select(MapProduto), request.Page, request.PageSize);
        }
    }

    public ProdutoResponse CadastrarProduto(CreateProdutoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.EmpresaId, "EmpresaId");
            _ = GetEmpresaOperacional(request.EmpresaId);
            ApplicationGuard.AgainstNullOrWhiteSpace(request.CodigoInterno, "CodigoInterno");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Sku, "Sku");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Descricao, "Descricao");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Ncm, "Ncm");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Origem, "Origem");
            ApplicationGuard.AgainstNegative(request.PrecoVenda, "PrecoVenda");
            ApplicationGuard.AgainstNegative(request.Custo, "Custo");

            var repository = new ProdutoRepository(store);
            var service = new CadastroProdutoService(repository);
            var produto = service.Cadastrar(request.EmpresaId, request.CodigoInterno, request.Sku, request.Descricao, request.Tipo, request.PrecoVenda, request.Custo, request.Ncm, request.Origem);
            AddIntegrationEvent("catalogo.produto_cadastrado", "Catalogo", produto.Id.ToString(), $"Produto {produto.Sku} cadastrado.");
            store.Persist();
            return MapProduto(produto);
        }
    }

    public ProdutoResponse InativarProduto(Guid produtoId)
    {
        lock (store.SyncRoot)
        {
            var produto = GetProduto(produtoId);
            produto.Inativar();
            store.Persist();
            return MapProduto(produto);
        }
    }

    public ProdutoResponse AdicionarVariacao(Guid produtoId, AddVariacaoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Sku, "Sku");
            var produto = GetProduto(produtoId);
            produto.AdicionarVariacao(request.Sku, request.CodigoBarras, request.PrecoVenda);
            store.Persist();
            return MapProduto(produto);
        }
    }

    public ProdutoResponse AtualizarFiscalProduto(Guid produtoId, UpdateFiscalProdutoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Ncm, "Ncm");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Origem, "Origem");
            var produto = GetProduto(produtoId);
            produto.AtualizarDadosFiscais(request.Ncm, request.Origem);
            store.Persist();
            return MapProduto(produto);
        }
    }

    public PagedResponse<ClienteResponse> ConsultarClientes(ConsultarClientesRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Clientes.Values.AsEnumerable();
            if (request.EmpresaId is not null)
            {
                query = query.Where(item => item.EmpresaId == request.EmpresaId);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<StatusCliente>(request.Status, true, out var status))
            {
                query = query.Where(item => item.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Termo))
            {
                var termo = request.Termo.Trim();
                query = query.Where(item =>
                    item.Documento.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    (item.Email is not null && item.Email.Contains(termo, StringComparison.OrdinalIgnoreCase)));
            }

            return ToPagedResponse(query.Select(MapCliente), request.Page, request.PageSize);
        }
    }

    public ClienteResponse CadastrarCliente(CreateClienteRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.EmpresaId, "EmpresaId");
            _ = GetEmpresaOperacional(request.EmpresaId);
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Documento, "Documento");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Nome, "Nome");
            var repository = new ClienteRepository(store);
            var service = new CadastroClienteService(repository);
            var cliente = service.Cadastrar(request.EmpresaId, request.Documento, request.Nome, request.Email);
            AddIntegrationEvent("clientes.cliente_cadastrado", "Clientes", cliente.Id.ToString(), $"Cliente {cliente.Documento} cadastrado.");
            store.Persist();
            return MapCliente(cliente);
        }
    }

    public ClienteResponse AtualizarCliente(Guid clienteId, AtualizarClienteRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Nome, "Nome");
            var cliente = GetCliente(clienteId);
            cliente.AtualizarCadastro(request.Nome, request.Email);
            store.Persist();
            return MapCliente(cliente);
        }
    }

    public ClienteResponse AtivarCliente(Guid clienteId)
    {
        lock (store.SyncRoot)
        {
            var cliente = GetCliente(clienteId);
            cliente.Ativar();
            store.Persist();
            return MapCliente(cliente);
        }
    }

    public ClienteResponse InativarCliente(Guid clienteId)
    {
        lock (store.SyncRoot)
        {
            var cliente = GetCliente(clienteId);
            cliente.Inativar();
            store.Persist();
            return MapCliente(cliente);
        }
    }

    public ClienteResponse BloquearCliente(Guid clienteId, BloquearClienteRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Motivo, "Motivo");
            var cliente = GetCliente(clienteId);
            cliente.Bloquear(request.Motivo);
            store.Persist();
            return MapCliente(cliente);
        }
    }

    public PagedResponse<DepositoResponse> ConsultarDepositos(ConsultarDepositosRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Depositos.Values.AsEnumerable();
            if (request.EmpresaId is not null)
            {
                query = query.Where(item => item.EmpresaId == request.EmpresaId);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<StatusDeposito>(request.Status, true, out var status))
            {
                query = query.Where(item => item.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Termo))
            {
                var termo = request.Termo.Trim();
                query = query.Where(item =>
                    item.Codigo.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(query.Select(MapDeposito), request.Page, request.PageSize);
        }
    }

    public DepositoResponse CadastrarDeposito(CreateDepositoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.EmpresaId, "EmpresaId");
            _ = GetEmpresaOperacional(request.EmpresaId);
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Codigo, "Codigo");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Nome, "Nome");
            var repository = new DepositoRepository(store);
            var service = new CadastroDepositoService(repository);
            var deposito = service.Cadastrar(request.EmpresaId, request.Codigo, request.Nome);
            AddIntegrationEvent("depositos.deposito_cadastrado", "Depositos", deposito.Id.ToString(), $"Deposito {deposito.Codigo} cadastrado.");
            store.Persist();
            return MapDeposito(deposito);
        }
    }

    public DepositoResponse AtualizarDeposito(Guid depositoId, AtualizarDepositoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Nome, "Nome");
            var deposito = GetDeposito(depositoId);
            deposito.AtualizarNome(request.Nome);
            store.Persist();
            return MapDeposito(deposito);
        }
    }

    public DepositoResponse AtivarDeposito(Guid depositoId)
    {
        lock (store.SyncRoot)
        {
            var deposito = GetDeposito(depositoId);
            deposito.Ativar();
            store.Persist();
            return MapDeposito(deposito);
        }
    }

    public DepositoResponse InativarDeposito(Guid depositoId)
    {
        lock (store.SyncRoot)
        {
            var deposito = GetDeposito(depositoId);
            deposito.Inativar();
            store.Persist();
            return MapDeposito(deposito);
        }
    }

    public IReadOnlyCollection<UsuarioResponse> ListarUsuarios()
    {
        lock (store.SyncRoot)
        {
            return store.Usuarios.Values.Select(MapUsuario).ToArray();
        }
    }

    public PagedResponse<UsuarioResponse> ConsultarUsuarios(ConsultarUsuariosRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Usuarios.Values.AsEnumerable();
            if (request.EmpresaId is not null)
            {
                query = query.Where(item => item.EmpresaId == request.EmpresaId);
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<StatusUsuario>(request.Status, true, out var status))
            {
                query = query.Where(item => item.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Termo))
            {
                var termo = request.Termo.Trim();
                query = query.Where(item =>
                    item.Email.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    item.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(query.Select(MapUsuario), request.Page, request.PageSize);
        }
    }

    public UsuarioResponse CadastrarUsuario(CreateUsuarioRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.EmpresaId, "EmpresaId");
            _ = GetEmpresaOperacional(request.EmpresaId);
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Email, "Email");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Nome, "Nome");
            var repository = new UsuarioRepository(store);
            var service = new CadastroUsuarioService(repository);
            var usuario = service.Cadastrar(request.EmpresaId, request.Email, request.Nome);
            AddIntegrationEvent("identity.usuario_cadastrado", "Identity", usuario.Id.ToString(), $"Usuario {usuario.Email} cadastrado.");
            store.Persist();
            return MapUsuario(usuario);
        }
    }

    public UsuarioResponse AtivarUsuario(Guid usuarioId)
    {
        lock (store.SyncRoot)
        {
            var usuario = GetUsuario(usuarioId);
            usuario.Ativar();
            store.Persist();
            return MapUsuario(usuario);
        }
    }

    public UsuarioResponse BloquearUsuario(Guid usuarioId, BloquearUsuarioRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Motivo, "Motivo");
            var usuario = GetUsuario(usuarioId);
            usuario.Bloquear(request.Motivo);
            store.Persist();
            return MapUsuario(usuario);
        }
    }

    public UsuarioResponse ConcederPermissao(Guid usuarioId, ConcederPermissaoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Permissao, "Permissao");
            var usuario = GetUsuario(usuarioId);
            usuario.ConcederPermissao(request.Permissao);
            store.Persist();
            return MapUsuario(usuario);
        }
    }

    public IReadOnlyCollection<SaldoEstoqueResponse> ListarSaldos()
    {
        lock (store.SyncRoot)
        {
            return store.Saldos.Values.Select(MapSaldo).ToArray();
        }
    }

    public PagedResponse<SaldoEstoqueResponse> ConsultarSaldos(ConsultarSaldosEstoqueRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Saldos.Values.AsEnumerable();
            if (request.ProdutoId is not null)
            {
                query = query.Where(item => item.ProdutoId == request.ProdutoId);
            }

            if (request.DepositoId is not null)
            {
                query = query.Where(item => item.DepositoId == request.DepositoId);
            }

            return ToPagedResponse(query.Select(MapSaldo), request.Page, request.PageSize);
        }
    }

    public IReadOnlyCollection<MovimentoEstoqueResponse> ListarMovimentosEstoque(ConsultarMovimentosEstoqueRequest request)
    {
        lock (store.SyncRoot)
        {
            return ToPagedResponse(store.MovimentosEstoque
                .Where(item => request.ProdutoId is null || item.ProdutoId == request.ProdutoId)
                .Where(item => request.DepositoId is null || item.DepositoId == request.DepositoId)
                .OrderByDescending(item => item.DataHora)
                .Select(MapMovimento), request.Page, request.PageSize).Items;
        }
    }

    public PagedResponse<MovimentoEstoqueResponse> ConsultarMovimentosEstoque(ConsultarMovimentosEstoqueRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.MovimentosEstoque
                .Where(item => request.ProdutoId is null || item.ProdutoId == request.ProdutoId)
                .Where(item => request.DepositoId is null || item.DepositoId == request.DepositoId)
                .OrderByDescending(item => item.DataHora)
                .Select(MapMovimento);

            return ToPagedResponse(query, request.Page, request.PageSize);
        }
    }

    public SaldoEstoqueResponse CriarSaldo(CriarSaldoEstoqueRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
            var produto = GetProduto(request.ProdutoId);
            var deposito = GetDepositoOperacional(request.DepositoId);
            EnsureSameEmpresa(produto.EmpresaId, deposito.EmpresaId, "Produto e deposito devem pertencer a mesma empresa.");
            var key = (request.ProdutoId, request.DepositoId);
            if (store.Saldos.ContainsKey(key))
            {
                throw new DomainException("Saldo de estoque ja cadastrado para produto e deposito.");
            }

            var saldo = new SaldoEstoque(request.ProdutoId, request.DepositoId, request.SaldoInicial, request.PermiteSaldoNegativo);
            store.Saldos[key] = saldo;
            store.Persist();
            return MapSaldo(saldo);
        }
    }

    public MovimentoEstoqueResponse AjustarSaldo(AjustarSaldoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
            var produto = GetProduto(request.ProdutoId);
            var deposito = GetDepositoOperacional(request.DepositoId);
            EnsureSameEmpresa(produto.EmpresaId, deposito.EmpresaId, "Produto e deposito devem pertencer a mesma empresa.");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Motivo, "Motivo");
            var saldo = GetSaldo(request.ProdutoId, request.DepositoId);
            var movimento = saldo.Ajustar(request.Quantidade, request.Motivo);
            AddStockMovement(movimento);
            store.Persist();
            return MapMovimento(movimento);
        }
    }

    public MovimentoEstoqueResponse ReservarSaldo(ReservarSaldoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
            var produto = GetProduto(request.ProdutoId);
            var deposito = GetDepositoOperacional(request.DepositoId);
            EnsureSameEmpresa(produto.EmpresaId, deposito.EmpresaId, "Produto e deposito devem pertencer a mesma empresa.");
            ApplicationGuard.AgainstZeroOrNegative(request.Quantidade, "Quantidade");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.DocumentoOrigem, "DocumentoOrigem");
            var saldo = GetSaldo(request.ProdutoId, request.DepositoId);
            var movimento = saldo.Reservar(request.Quantidade, request.DocumentoOrigem);
            AddStockMovement(movimento);
            store.Persist();
            return MapMovimento(movimento);
        }
    }

    public MovimentoEstoqueResponse ConfirmarBaixaFaturamento(ConfirmarBaixaRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
            var produto = GetProduto(request.ProdutoId);
            var deposito = GetDepositoOperacional(request.DepositoId);
            EnsureSameEmpresa(produto.EmpresaId, deposito.EmpresaId, "Produto e deposito devem pertencer a mesma empresa.");
            ApplicationGuard.AgainstZeroOrNegative(request.Quantidade, "Quantidade");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.EventoId, "EventoId");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.DocumentoOrigem, "DocumentoOrigem");
            var saldo = GetSaldo(request.ProdutoId, request.DepositoId);
            var movimento = saldo.ConfirmarBaixaFaturamento(request.Quantidade, request.EventoId, request.DocumentoOrigem);
            AddStockMovement(movimento);
            store.Persist();
            return MapMovimento(movimento);
        }
    }

    public TransferenciaResponse Transferir(TransferirEstoqueRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoOrigemId, "DepositoOrigemId");
            ApplicationGuard.AgainstEmptyGuid(request.DepositoDestinoId, "DepositoDestinoId");
            var produto = GetProduto(request.ProdutoId);
            var depositoOrigem = GetDepositoOperacional(request.DepositoOrigemId);
            var depositoDestino = GetDepositoOperacional(request.DepositoDestinoId);
            EnsureSameEmpresa(produto.EmpresaId, depositoOrigem.EmpresaId, "Produto e deposito de origem devem pertencer a mesma empresa.");
            EnsureSameEmpresa(produto.EmpresaId, depositoDestino.EmpresaId, "Produto e deposito de destino devem pertencer a mesma empresa.");
            EnsureSameEmpresa(depositoOrigem.EmpresaId, depositoDestino.EmpresaId, "Depositos de transferencia devem pertencer a mesma empresa.");
            ApplicationGuard.AgainstZeroOrNegative(request.Quantidade, "Quantidade");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.DocumentoOrigem, "DocumentoOrigem");
            var origem = GetSaldo(request.ProdutoId, request.DepositoOrigemId);
            var destino = GetSaldo(request.ProdutoId, request.DepositoDestinoId);
            var service = new TransferenciaEstoqueService();
            var (saida, entrada) = service.Transferir(origem, destino, request.Quantidade, request.DocumentoOrigem);
            AddStockMovement(saida);
            AddStockMovement(entrada);
            store.Persist();
            return new TransferenciaResponse(MapMovimento(saida), MapMovimento(entrada));
        }
    }

    public IReadOnlyCollection<PedidoVendaResponse> ListarPedidos()
    {
        lock (store.SyncRoot)
        {
            return store.Pedidos.Values.Select(MapPedido).ToArray();
        }
    }

    public PagedResponse<PedidoVendaResponse> ConsultarPedidos(ConsultarPedidosVendaRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.Pedidos.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(item => string.Equals(item.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (request.ClienteId is not null)
            {
                query = query.Where(item => item.ClienteId == request.ClienteId);
            }

            return ToPagedResponse(query.Select(MapPedido), request.Page, request.PageSize);
        }
    }

    public PedidoVendaResponse CriarPedido(CreatePedidoVendaRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ClienteId, "ClienteId");
            _ = GetCliente(request.ClienteId);
            var pedido = new PedidoVenda(request.ClienteId);
            store.Pedidos[pedido.Id] = pedido;
            store.Persist();
            return MapPedido(pedido);
        }
    }

    public PedidoVendaResponse AdicionarItemPedido(Guid pedidoId, AddItemPedidoRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.ProdutoId, "ProdutoId");
            ApplicationGuard.AgainstZeroOrNegative(request.Quantidade, "Quantidade");
            ApplicationGuard.AgainstNegative(request.PrecoUnitario, "PrecoUnitario");
            var pedido = GetPedido(pedidoId);
            var cliente = GetCliente(pedido.ClienteId);
            var produto = GetProduto(request.ProdutoId);
            EnsureSameEmpresa(cliente.EmpresaId, produto.EmpresaId, "Produto informado pertence a outra empresa do pedido.");
            EnsurePedidoPodeSerAlterado(pedido.Id);
            pedido.AdicionarItem(request.ProdutoId, request.Quantidade, request.PrecoUnitario);
            store.Persist();
            return MapPedido(pedido);
        }
    }

    public PedidoVendaResponse AprovarPedido(Guid pedidoId, AprovarPedidoRequest request)
    {
        lock (store.SyncRoot)
        {
            var pedido = GetPedido(pedidoId);
            var cliente = GetCliente(pedido.ClienteId);
            var clienteAtivo = request.ClienteAtivo && cliente.Status == StatusCliente.Ativo;
            pedido.Aprovar(clienteAtivo);
            store.Persist();
            return MapPedido(pedido);
        }
    }

    public PedidoVendaResponse ReservarPedido(Guid pedidoId, ReservarPedidoRequest request)
    {
        lock (store.SyncRoot)
        {
            return ExecuteInLogicalTransaction(() =>
            {
                ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
                var deposito = GetDepositoOperacional(request.DepositoId);
                var pedido = GetPedido(pedidoId);
                var cliente = GetCliente(pedido.ClienteId);
                EnsureSameEmpresa(cliente.EmpresaId, deposito.EmpresaId, "Deposito informado pertence a outra empresa do pedido.");

                if (pedido.Status == StatusPedidoVenda.Reservado || pedido.Status == StatusPedidoVenda.Faturado)
                {
                    return MapPedido(pedido);
                }

                pedido.Reservar((produtoId, quantidade) =>
                {
                    var produto = GetProduto(produtoId);
                    EnsureSameEmpresa(cliente.EmpresaId, produto.EmpresaId, "Produto informado pertence a outra empresa do pedido.");
                    var key = (produtoId, request.DepositoId);
                    return store.Saldos.TryGetValue(key, out var saldo) && saldo.Disponivel >= quantidade;
                });

                foreach (var item in pedido.Itens)
                {
                    var saldo = GetSaldo(item.ProdutoId, request.DepositoId);
                    var movimento = saldo.Reservar(item.Quantidade, $"PED-{pedido.Id}");
                    AddStockMovement(movimento);
                }

                AddIntegrationEvent("vendas.pedido_reservado", "Vendas", pedido.Id.ToString(), $"Pedido reservado no deposito {request.DepositoId}.");
                return MapPedido(pedido);
            });
        }
    }

    public PedidoVendaResponse CancelarPedido(Guid pedidoId, CancelarPedidoRequest request)
    {
        lock (store.SyncRoot)
        {
            return ExecuteInLogicalTransaction(() =>
            {
                var pedido = GetPedido(pedidoId);

                if (pedido.Status == StatusPedidoVenda.Cancelado)
                {
                    return MapPedido(pedido);
                }

                EnsurePedidoPodeSerAlterado(pedido.Id);

                if (request.LiberarReservaEstoque && pedido.Status == StatusPedidoVenda.Reservado)
                {
                    if (request.DepositoId is null || request.DepositoId == Guid.Empty)
                    {
                        throw new DomainException("DepositoId e obrigatorio para liberar reserva de estoque.");
                    }

                    var deposito = GetDepositoOperacional(request.DepositoId.Value);
                    var cliente = GetCliente(pedido.ClienteId);
                    EnsureSameEmpresa(cliente.EmpresaId, deposito.EmpresaId, "Deposito informado pertence a outra empresa do pedido.");

                    foreach (var item in pedido.Itens)
                    {
                        var saldo = GetSaldo(item.ProdutoId, request.DepositoId.Value);
                        var movimento = saldo.LiberarReserva(item.Quantidade, $"PED-{pedido.Id}");
                        AddStockMovement(movimento);
                    }

                    AddIntegrationEvent("estoque.reserva_liberada", "Estoque", pedido.Id.ToString(), $"Reserva do pedido liberada no deposito {request.DepositoId.Value}.");
                }

                pedido.Cancelar();
                AddIntegrationEvent("vendas.pedido_cancelado", "Vendas", pedido.Id.ToString(), "Pedido cancelado.");
                return MapPedido(pedido);
            });
        }
    }

    public ResultadoImportacaoNotaResponse ImportarNotaEntrada(ImportarNotaEntradaRequest request)
    {
        lock (store.SyncRoot)
        {
            return ExecuteInLogicalTransaction(() =>
            {
                ApplicationGuard.AgainstEmptyGuid(request.EmpresaId, "EmpresaId");
                var empresa = GetEmpresaOperacional(request.EmpresaId);
                ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
                var deposito = GetDepositoOperacional(request.DepositoId);
                EnsureSameEmpresa(empresa.Id, deposito.EmpresaId, "Deposito informado pertence a outra empresa.");
                ApplicationGuard.AgainstNullOrWhiteSpace(request.ChaveAcesso, "ChaveAcesso");
                ApplicationGuard.AgainstEmptyCollection(request.ItensExternos, "ItensExternos");
                var repository = new NotaEntradaRepository(store);
                var service = new ImportacaoNotaEntradaService(repository);
                var resultado = service.Importar(
                    request.EmpresaId,
                    request.ChaveAcesso,
                    request.ItensExternos.Select(item => new ItemNotaEntradaExterna(item.CodigoExterno, item.Descricao, item.Quantidade)).ToArray(),
                    request.Conciliacoes);

                var movimentos = new List<MovimentoEstoqueImportacaoResponse>();
                if (resultado.ImportadaComSucesso)
                {
                    foreach (var item in request.ItensExternos)
                    {
                        var produtoId = request.Conciliacoes[item.CodigoExterno];
                        var produto = GetProduto(produtoId);
                        EnsureSameEmpresa(empresa.Id, produto.EmpresaId, "Produto conciliado pertence a outra empresa.");
                        var saldo = GetOrCreateSaldoParaEntrada(produtoId, request.DepositoId);
                        var movimento = saldo.Ajustar(item.Quantidade, $"Entrada por nota importada {request.ChaveAcesso}");
                        AddStockMovement(movimento);
                        movimentos.Add(new MovimentoEstoqueImportacaoResponse(
                            produtoId,
                            request.DepositoId,
                            item.Quantidade,
                            movimento.SaldoAnterior,
                            movimento.SaldoPosterior,
                            request.ChaveAcesso));
                    }

                    AddIntegrationEvent("compras.nota_importada", "Compras", request.ChaveAcesso, $"Nota importada para o deposito {request.DepositoId}.");
                }

                store.ImportacoesNotaEntrada.Add(new ImportacaoNotaEntradaRegistro(
                    request.EmpresaId,
                    request.DepositoId,
                    request.ChaveAcesso,
                    resultado.ImportadaComSucesso,
                    request.ItensExternos.Count,
                    resultado.ItensPendentesConciliacao.Count,
                    movimentos.Count,
                    DateTimeOffset.UtcNow));

                return new ResultadoImportacaoNotaResponse(
                    resultado.ImportadaComSucesso,
                    resultado.ItensPendentesConciliacao.Select(item => new ItemNotaEntradaExternaResponse(item.CodigoExterno, item.Descricao, item.Quantidade)).ToArray(),
                    movimentos);
            });
        }
    }

    public PagedResponse<ImportacaoNotaEntradaResponse> ConsultarImportacoesNotaEntrada(ConsultarImportacoesNotaEntradaRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.ImportacoesNotaEntrada.AsEnumerable();
            if (request.EmpresaId is not null)
            {
                query = query.Where(item => item.EmpresaId == request.EmpresaId);
            }

            if (request.DepositoId is not null)
            {
                query = query.Where(item => item.DepositoId == request.DepositoId);
            }

            if (request.ImportadaComSucesso is not null)
            {
                query = query.Where(item => item.ImportadaComSucesso == request.ImportadaComSucesso);
            }

            if (!string.IsNullOrWhiteSpace(request.ChaveAcesso))
            {
                query = query.Where(item => item.ChaveAcesso.Contains(request.ChaveAcesso.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(
                query.OrderByDescending(item => item.ProcessadaEm).Select(MapImportacaoNotaEntrada),
                request.Page,
                request.PageSize);
        }
    }

    public IReadOnlyCollection<NotaFiscalResponse> ListarNotasFiscais()
    {
        lock (store.SyncRoot)
        {
            return store.NotasFiscais.Values.Select(MapNotaFiscal).ToArray();
        }
    }

    public PagedResponse<NotaFiscalResponse> ConsultarNotasFiscais(ConsultarNotasFiscaisRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.NotasFiscais.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(item => string.Equals(item.Status.ToString(), request.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (request.ClienteId is not null)
            {
                query = query.Where(item => item.ClienteId == request.ClienteId);
            }

            return ToPagedResponse(query.Select(MapNotaFiscal), request.Page, request.PageSize);
        }
    }

    public NotaFiscalResponse CriarNotaFiscal(CreateNotaFiscalRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstEmptyGuid(request.PedidoVendaId, "PedidoVendaId");
            ApplicationGuard.AgainstEmptyGuid(request.ClienteId, "ClienteId");
            ApplicationGuard.AgainstEmptyCollection(request.Itens, "Itens");
            var pedido = GetPedido(request.PedidoVendaId);
            var clienteNota = GetCliente(request.ClienteId);
            var clientePedido = GetCliente(pedido.ClienteId);
            EnsureSameEmpresa(clientePedido.EmpresaId, clienteNota.EmpresaId, "Cliente informado pertence a outra empresa do pedido.");
            if (pedido.ClienteId != request.ClienteId)
            {
                throw new DomainException("Cliente informado para a nota fiscal difere do cliente do pedido.");
            }

            if (HasNotaFiscalAtivaParaPedido(pedido.Id))
            {
                throw new DomainException("Pedido ja possui nota fiscal ativa vinculada.");
            }

            if (pedido.Status == StatusPedidoVenda.Cancelado)
            {
                throw new DomainException("Pedido cancelado nao pode gerar nota fiscal.");
            }

            foreach (var item in request.Itens)
            {
                ApplicationGuard.AgainstEmptyGuid(item.ProdutoId, "ProdutoId");
                ApplicationGuard.AgainstZeroOrNegative(item.Quantidade, "Quantidade");
                ApplicationGuard.AgainstNullOrWhiteSpace(item.Ncm, "Ncm");
                ApplicationGuard.AgainstNullOrWhiteSpace(item.Cfop, "Cfop");
                var produto = GetProduto(item.ProdutoId);
                EnsureSameEmpresa(clientePedido.EmpresaId, produto.EmpresaId, "Produto informado pertence a outra empresa do pedido.");
            }

            EnsureNotaFiscalMatchesPedido(pedido, request.Itens);

            var itens = request.Itens.Select(item => new ItemNotaFiscal(item.ProdutoId, item.Quantidade, item.Ncm, item.Cfop)).ToArray();
            var nota = new NotaFiscal(request.PedidoVendaId, request.ClienteId, itens);
            store.NotasFiscais[nota.Id] = nota;
            store.Persist();
            return MapNotaFiscal(nota);
        }
    }

    public NotaFiscalResponse AutorizarNotaFiscal(Guid notaFiscalId, AutorizarNotaFiscalRequest request)
    {
        lock (store.SyncRoot)
        {
            return ExecuteInLogicalTransaction(() =>
            {
                ApplicationGuard.AgainstEmptyGuid(request.DepositoId, "DepositoId");
                var deposito = GetDepositoOperacional(request.DepositoId);
                ApplicationGuard.AgainstNullOrWhiteSpace(request.EventoId, "EventoId");
                var nota = GetNotaFiscal(notaFiscalId);
                var cliente = GetCliente(nota.ClienteId);
                EnsureSameEmpresa(cliente.EmpresaId, deposito.EmpresaId, "Deposito informado pertence a outra empresa da nota fiscal.");
                if (nota.Status == StatusNotaFiscal.Autorizada)
                {
                    return MapNotaFiscal(nota);
                }

                nota.Autorizar();

                foreach (var item in nota.Itens)
                {
                    var saldo = GetSaldo(item.ProdutoId, request.DepositoId);
                    var movimento = saldo.ConfirmarBaixaFaturamento(item.Quantidade, $"{request.EventoId}:{item.ProdutoId}", $"NF-{nota.Id}");
                    AddStockMovement(movimento);
                }

                if (store.Pedidos.TryGetValue(nota.PedidoVendaId, out var pedido) && pedido.Status == StatusPedidoVenda.Reservado)
                {
                    pedido.Faturar();
                    AddIntegrationEvent("vendas.pedido_faturado", "Vendas", pedido.Id.ToString(), $"Pedido faturado pela nota {nota.Id}.");
                }

                AddIntegrationEvent("fiscal.nota_autorizada", "Fiscal", nota.Id.ToString(), $"Nota autorizada com evento {request.EventoId}.");
                return MapNotaFiscal(nota);
            });
        }
    }

    public NotaFiscalResponse RegistrarRejeicaoNotaFiscal(Guid notaFiscalId, RegistrarRejeicaoNotaFiscalRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Codigo, "Codigo");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Mensagem, "Mensagem");
            var nota = GetNotaFiscal(notaFiscalId);
            if (nota.Status == StatusNotaFiscal.Rejeitada && nota.CodigoRejeicao == request.Codigo && nota.MensagemRejeicao == request.Mensagem)
            {
                return MapNotaFiscal(nota);
            }

            nota.RegistrarRejeicao(request.Codigo, request.Mensagem);
            AddIntegrationEvent("fiscal.nota_rejeitada", "Fiscal", nota.Id.ToString(), $"Nota rejeitada com codigo {request.Codigo}.");
            store.Persist();
            return MapNotaFiscal(nota);
        }
    }

    public NotaFiscalResponse CancelarNotaFiscal(Guid notaFiscalId, CancelarNotaFiscalRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Justificativa, "Justificativa");
            var nota = GetNotaFiscal(notaFiscalId);
            if (nota.Status == StatusNotaFiscal.Cancelada && string.Equals(nota.JustificativaCancelamento, request.Justificativa.Trim(), StringComparison.Ordinal))
            {
                return MapNotaFiscal(nota);
            }

            nota.Cancelar(request.Justificativa, request.EstornarImpactosOperacionais);
            AddIntegrationEvent("fiscal.nota_cancelada", "Fiscal", nota.Id.ToString(), "Nota fiscal cancelada.");
            store.Persist();
            return MapNotaFiscal(nota);
        }
    }

    public ResultadoWebhookResponse ProcessarWebhook(ProcessarWebhookRequest request)
    {
        lock (store.SyncRoot)
        {
            ApplicationGuard.AgainstNullOrWhiteSpace(request.EventoId, "EventoId");
            ApplicationGuard.AgainstNullOrWhiteSpace(request.Origem, "Origem");
            var repository = new EventoIntegracaoRepository(store);
            var service = new ProcessamentoWebhookService(repository);
            var resultado = service.Processar(new WebhookRecebido(request.EventoId, request.Origem, request.Payload));
            store.WebhooksProcessados.Add(new WebhookProcessadoRegistro(
                request.EventoId,
                request.Origem,
                resultado.Status.ToString(),
                resultado.Mensagem,
                DateTimeOffset.UtcNow));
            AddIntegrationEvent("integracoes.webhook_processado", "Integracoes", request.EventoId, $"Webhook processado da origem {request.Origem}.");
            store.Persist();
            return new ResultadoWebhookResponse(resultado.Status.ToString(), resultado.Mensagem);
        }
    }

    public PagedResponse<WebhookProcessadoResponse> ConsultarWebhooks(ConsultarWebhooksRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.WebhooksProcessados.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Origem))
            {
                query = query.Where(item => string.Equals(item.Origem, request.Origem, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(item => string.Equals(item.Status, request.Status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.EventoId))
            {
                query = query.Where(item => item.EventoId.Contains(request.EventoId.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(
                query.OrderByDescending(item => item.ProcessadoEm).Select(MapWebhookProcessado),
                request.Page,
                request.PageSize);
        }
    }

    public IReadOnlyCollection<IntegrationEventResponse> ListarEventosIntegracao()
    {
        lock (store.SyncRoot)
        {
            return store.IntegrationEvents
                .OrderByDescending(item => item.OccurredAt)
                .Select(item => new IntegrationEventResponse(item.Id, item.Type, item.SourceModule, item.AggregateId, item.Description, item.OccurredAt))
                .ToArray();
        }
    }

    public PagedResponse<IntegrationEventResponse> ConsultarEventosIntegracao(ConsultarEventosIntegracaoRequest request)
    {
        lock (store.SyncRoot)
        {
            var query = store.IntegrationEvents.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                query = query.Where(item => string.Equals(item.Type, request.Type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.SourceModule))
            {
                query = query.Where(item => string.Equals(item.SourceModule, request.SourceModule, StringComparison.OrdinalIgnoreCase));
            }

            return ToPagedResponse(
                query.OrderByDescending(item => item.OccurredAt)
                    .Select(item => new IntegrationEventResponse(item.Id, item.Type, item.SourceModule, item.AggregateId, item.Description, item.OccurredAt)),
                request.Page,
                request.PageSize);
        }
    }

    private Produto GetProduto(Guid produtoId)
    {
        if (!store.Produtos.TryGetValue(produtoId, out var produto))
        {
            throw new NotFoundException("Produto nao encontrado.");
        }

        return produto;
    }

    private Empresa GetEmpresa(Guid empresaId)
    {
        if (!store.Empresas.TryGetValue(empresaId, out var empresa))
        {
            throw new NotFoundException("Empresa nao encontrada.");
        }

        return empresa;
    }

    private Empresa GetEmpresaOperacional(Guid empresaId)
    {
        var empresa = GetEmpresa(empresaId);
        empresa.GarantirQuePodeOperar();
        return empresa;
    }

    private Usuario GetUsuario(Guid usuarioId)
    {
        if (!store.Usuarios.TryGetValue(usuarioId, out var usuario))
        {
            throw new NotFoundException("Usuario nao encontrado.");
        }

        return usuario;
    }

    private Cliente GetCliente(Guid clienteId)
    {
        if (!store.Clientes.TryGetValue(clienteId, out var cliente))
        {
            throw new NotFoundException("Cliente nao encontrado.");
        }

        return cliente;
    }

    private Deposito GetDeposito(Guid depositoId)
    {
        if (!store.Depositos.TryGetValue(depositoId, out var deposito))
        {
            throw new NotFoundException("Deposito nao encontrado.");
        }

        return deposito;
    }

    private Deposito GetDepositoOperacional(Guid depositoId)
    {
        var deposito = GetDeposito(depositoId);
        deposito.GarantirQuePodeOperar();
        return deposito;
    }

    private PedidoVenda GetPedido(Guid pedidoId)
    {
        if (!store.Pedidos.TryGetValue(pedidoId, out var pedido))
        {
            throw new NotFoundException("Pedido de venda nao encontrado.");
        }

        return pedido;
    }

    private NotaFiscal GetNotaFiscal(Guid notaFiscalId)
    {
        if (!store.NotasFiscais.TryGetValue(notaFiscalId, out var nota))
        {
            throw new NotFoundException("Nota fiscal nao encontrada.");
        }

        return nota;
    }

    private SaldoEstoque GetSaldo(Guid produtoId, Guid depositoId)
    {
        if (!store.Saldos.TryGetValue((produtoId, depositoId), out var saldo))
        {
            throw new NotFoundException("Saldo de estoque nao encontrado para produto e deposito informados.");
        }

        return saldo;
    }

    private SaldoEstoque GetOrCreateSaldoParaEntrada(Guid produtoId, Guid depositoId)
    {
        if (store.Saldos.TryGetValue((produtoId, depositoId), out var saldo))
        {
            return saldo;
        }

        saldo = new SaldoEstoque(produtoId, depositoId, 0m, permiteSaldoNegativo: false);
        store.Saldos[(produtoId, depositoId)] = saldo;
        return saldo;
    }

    private ProdutoResponse MapProduto(Produto produto)
    {
        return new ProdutoResponse(
            produto.Id,
            produto.EmpresaId,
            produto.CodigoInterno,
            produto.Sku,
            produto.Descricao,
            produto.Tipo.ToString(),
            produto.PrecoVenda,
            produto.Custo,
            produto.Ncm,
            produto.Origem,
            produto.Ativo,
            produto.Variacoes.Select(variacao => new ProdutoVariacaoResponse(variacao.Sku, variacao.CodigoBarras, variacao.PrecoVenda)).ToArray(),
            produto.AuditoriasFiscais.Select(auditoria => new AuditChangeResponse(auditoria.Field, auditoria.PreviousValue, auditoria.CurrentValue)).ToArray());
    }

    private static UsuarioResponse MapUsuario(Usuario usuario)
    {
        return new UsuarioResponse(usuario.Id, usuario.EmpresaId, usuario.Email, usuario.Nome, usuario.Status.ToString(), usuario.UltimoBloqueioEm, usuario.Permissoes.ToArray());
    }

    private static ClienteResponse MapCliente(Cliente cliente)
    {
        return new ClienteResponse(cliente.Id, cliente.EmpresaId, cliente.Documento, cliente.Nome, cliente.Email, cliente.Status.ToString(), cliente.UltimoBloqueioEm);
    }

    private static EmpresaResponse MapEmpresa(Empresa empresa)
    {
        return new EmpresaResponse(empresa.Id, empresa.Documento, empresa.NomeFantasia, empresa.RazaoSocial, empresa.Status.ToString());
    }

    private static DepositoResponse MapDeposito(Deposito deposito)
    {
        return new DepositoResponse(deposito.Id, deposito.EmpresaId, deposito.Codigo, deposito.Nome, deposito.Status.ToString());
    }

    private static ImportacaoNotaEntradaResponse MapImportacaoNotaEntrada(ImportacaoNotaEntradaRegistro importacao)
    {
        return new ImportacaoNotaEntradaResponse(
            importacao.EmpresaId,
            importacao.DepositoId,
            importacao.ChaveAcesso,
            importacao.ImportadaComSucesso,
            importacao.ItensExternos,
            importacao.ItensPendentesConciliacao,
            importacao.MovimentosGerados,
            importacao.ProcessadaEm);
    }

    private static WebhookProcessadoResponse MapWebhookProcessado(WebhookProcessadoRegistro webhook)
    {
        return new WebhookProcessadoResponse(
            webhook.EventoId,
            webhook.Origem,
            webhook.Status,
            webhook.Mensagem,
            webhook.ProcessadoEm);
    }

    private static SaldoEstoqueResponse MapSaldo(SaldoEstoque saldo)
    {
        return new SaldoEstoqueResponse(saldo.ProdutoId, saldo.DepositoId, saldo.SaldoAtual, saldo.Reservado, saldo.Disponivel, saldo.PermiteSaldoNegativo);
    }

    private static MovimentoEstoqueResponse MapMovimento(MovimentoEstoque movimento)
    {
        return new MovimentoEstoqueResponse(
            movimento.ProdutoId,
            movimento.DepositoId,
            movimento.Tipo.ToString(),
            movimento.Quantidade,
            movimento.Motivo,
            movimento.DocumentoOrigem,
            movimento.SaldoAnterior,
            movimento.SaldoPosterior,
            movimento.DataHora);
    }

    private static PedidoVendaResponse MapPedido(PedidoVenda pedido)
    {
        return new PedidoVendaResponse(
            pedido.Id,
            pedido.ClienteId,
            pedido.Status.ToString(),
            pedido.Itens.Select(item => new ItemPedidoVendaResponse(item.ProdutoId, item.Quantidade, item.PrecoUnitario)).ToArray());
    }

    private static NotaFiscalResponse MapNotaFiscal(NotaFiscal nota)
    {
        return new NotaFiscalResponse(
            nota.Id,
            nota.PedidoVendaId,
            nota.ClienteId,
            nota.Status.ToString(),
            nota.CodigoRejeicao,
            nota.MensagemRejeicao,
            nota.EstoqueBaixado,
            nota.JustificativaCancelamento,
            nota.Itens.Select(item => new ItemNotaFiscalResponse(item.ProdutoId, item.Quantidade, item.Ncm, item.Cfop)).ToArray(),
            nota.HistoricoTentativas.ToArray());
    }

    private static PagedResponse<T> ToPagedResponse<T>(IEnumerable<T> items, int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 200);
        var materialized = items.ToArray();
        var pagedItems = materialized
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToArray();

        return new PagedResponse<T>(pagedItems, normalizedPage, normalizedPageSize, materialized.Length);
    }

    private bool HasNotaFiscalAtivaParaPedido(Guid pedidoId)
    {
        return store.NotasFiscais.Values.Any(item =>
            item.PedidoVendaId == pedidoId &&
            item.Status != StatusNotaFiscal.Cancelada);
    }

    private void EnsurePedidoPodeSerAlterado(Guid pedidoId)
    {
        if (HasNotaFiscalAtivaParaPedido(pedidoId))
        {
            throw new DomainException("Pedido com nota fiscal vinculada nao pode ser alterado.");
        }
    }

    private static void EnsureSameEmpresa(Guid expectedEmpresaId, Guid actualEmpresaId, string message)
    {
        if (expectedEmpresaId != actualEmpresaId)
        {
            throw new DomainException(message);
        }
    }

    private static void EnsureNotaFiscalMatchesPedido(PedidoVenda pedido, IReadOnlyCollection<ItemNotaFiscalRequest> itensNota)
    {
        var itensPedido = pedido.Itens
            .GroupBy(item => item.ProdutoId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantidade));
        var itensSolicitados = itensNota
            .GroupBy(item => item.ProdutoId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantidade));

        if (itensPedido.Count != itensSolicitados.Count)
        {
            throw new DomainException("Itens da nota fiscal diferem dos itens do pedido.");
        }

        foreach (var itemPedido in itensPedido)
        {
            if (!itensSolicitados.TryGetValue(itemPedido.Key, out var quantidadeNota) || quantidadeNota != itemPedido.Value)
            {
                throw new DomainException("Itens da nota fiscal diferem dos itens do pedido.");
            }
        }
    }

    private void AddIntegrationEvent(string type, string sourceModule, string aggregateId, string description)
    {
        store.IntegrationEvents.Add(new IntegrationEvent(
            Guid.NewGuid(),
            type,
            sourceModule,
            aggregateId,
            description,
            DateTimeOffset.UtcNow));
    }

    private void AddStockMovement(MovimentoEstoque movimento)
    {
        store.MovimentosEstoque.Add(movimento);
    }

    private T ExecuteInLogicalTransaction<T>(Func<T> action)
    {
        var snapshot = ErpSnapshotSerializer.Serialize(store);
        try
        {
            var result = action();
            store.Persist();
            return result;
        }
        catch
        {
            ErpSnapshotSerializer.Load(store, snapshot);
            throw;
        }
    }

    private sealed class EmpresaRepository(IErpStore store) : IEmpresaRepository
    {
        public bool DocumentoJaExiste(string documento) =>
            store.Empresas.Values.Any(empresa => string.Equals(empresa.Documento, documento.Trim(), StringComparison.OrdinalIgnoreCase));

        public void Add(Empresa empresa) => store.Empresas[empresa.Id] = empresa;
    }

    private sealed class ProdutoRepository(IErpStore store) : IProdutoRepository
    {
        public bool SkuJaExiste(Guid empresaId, string sku) =>
            store.Produtos.Values.Any(produto => produto.EmpresaId == empresaId && string.Equals(produto.Sku, sku, StringComparison.OrdinalIgnoreCase));

        public void Add(Produto produto) => store.Produtos[produto.Id] = produto;
    }

    private sealed class ClienteRepository(IErpStore store) : IClienteRepository
    {
        public bool DocumentoJaExiste(Guid empresaId, string documento) =>
            store.Clientes.Values.Any(cliente => cliente.EmpresaId == empresaId && string.Equals(cliente.Documento, documento.Trim(), StringComparison.OrdinalIgnoreCase));

        public void Add(Cliente cliente) => store.Clientes[cliente.Id] = cliente;
    }

    private sealed class DepositoRepository(IErpStore store) : IDepositoRepository
    {
        public bool CodigoJaExiste(Guid empresaId, string codigo) =>
            store.Depositos.Values.Any(deposito => deposito.EmpresaId == empresaId && string.Equals(deposito.Codigo, codigo.Trim(), StringComparison.OrdinalIgnoreCase));

        public void Add(Deposito deposito) => store.Depositos[deposito.Id] = deposito;
    }

    private sealed class UsuarioRepository(IErpStore store) : IUsuarioRepository
    {
        public bool EmailJaExiste(Guid empresaId, string email) =>
            store.Usuarios.Values.Any(usuario => usuario.EmpresaId == empresaId && string.Equals(usuario.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

        public void Add(Usuario usuario) => store.Usuarios[usuario.Id] = usuario;
    }

    private sealed class NotaEntradaRepository(IErpStore store) : INotaEntradaRepository
    {
        public bool ChaveJaImportada(Guid empresaId, string chaveAcesso) => store.ChavesImportadas.Contains((empresaId, chaveAcesso));

        public void RegistrarImportacao(Guid empresaId, string chaveAcesso) => store.ChavesImportadas.Add((empresaId, chaveAcesso));
    }

    private sealed class EventoIntegracaoRepository(IErpStore store) : IEventoIntegracaoRepository
    {
        public bool EventoJaProcessado(string eventoId) => store.EventosWebhook.Contains(eventoId);

        public void Registrar(string eventoId, string origem) => store.EventosWebhook.Add(eventoId);
    }
}
