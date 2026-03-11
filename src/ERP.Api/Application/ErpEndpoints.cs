using ERP.BuildingBlocks;
using ERP.Api.Application.Contracts;

namespace ERP.Api.Application;

public static class ErpEndpoints
{
    private const string SessionHeader = "X-Session-Token";

    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var empresas = app.MapGroup("/empresas").WithTags("Empresas");
        empresas.MapGet(string.Empty, (string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarEmpresas(new ConsultarEmpresasRequest(status, termo, page ?? 1, pageSize ?? 20)))));
        empresas.MapPost(string.Empty, (HttpContext httpContext, CreateEmpresaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "EMPRESAS_MANAGE");
            var response = service.CadastrarEmpresa(request);
            return Results.Created($"/empresas/{response.Id}", ApiResponses.Ok(response));
        });
        empresas.MapPost("/{empresaId:guid}/atualizar", (HttpContext httpContext, Guid empresaId, AtualizarEmpresaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "EMPRESAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtualizarEmpresa(empresaId, request)));
        });
        empresas.MapPost("/{empresaId:guid}/ativar", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "EMPRESAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtivarEmpresa(empresaId)));
        });
        empresas.MapPost("/{empresaId:guid}/inativar", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "EMPRESAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.InativarEmpresa(empresaId)));
        });
        empresas.MapPost("/{empresaId:guid}/bloquear", (HttpContext httpContext, Guid empresaId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "EMPRESAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.BloquearEmpresa(empresaId)));
        });

        var catalogo = app.MapGroup("/catalogo").WithTags("Catalogo");
        catalogo.MapGet("/produtos", (Guid? empresaId, bool? ativo, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarProdutos(new ConsultarProdutosRequest(empresaId, ativo, termo, page ?? 1, pageSize ?? 20)))));
        catalogo.MapPost("/produtos", (HttpContext httpContext, CreateProdutoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CATALOGO_MANAGE", request.EmpresaId);
            var response = service.CadastrarProduto(request);
            return Results.Created($"/catalogo/produtos/{response.Id}", ApiResponses.Ok(response));
        });
        catalogo.MapPost("/produtos/{produtoId:guid}/inativar", (HttpContext httpContext, Guid produtoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CATALOGO_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.InativarProduto(produtoId)));
        });
        catalogo.MapPost("/produtos/{produtoId:guid}/variacoes", (HttpContext httpContext, Guid produtoId, AddVariacaoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CATALOGO_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AdicionarVariacao(produtoId, request)));
        });
        catalogo.MapPost("/produtos/{produtoId:guid}/dados-fiscais", (HttpContext httpContext, Guid produtoId, UpdateFiscalProdutoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CATALOGO_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtualizarFiscalProduto(produtoId, request)));
        });

        var clientes = app.MapGroup("/clientes").WithTags("Clientes");
        clientes.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarClientes(new ConsultarClientesRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        clientes.MapPost(string.Empty, (HttpContext httpContext, CreateClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CLIENTES_MANAGE", request.EmpresaId);
            var response = service.CadastrarCliente(request);
            return Results.Created($"/clientes/{response.Id}", ApiResponses.Ok(response));
        });
        clientes.MapPost("/{clienteId:guid}/atualizar", (HttpContext httpContext, Guid clienteId, AtualizarClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CLIENTES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtualizarCliente(clienteId, request)));
        });
        clientes.MapPost("/{clienteId:guid}/ativar", (HttpContext httpContext, Guid clienteId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CLIENTES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtivarCliente(clienteId)));
        });
        clientes.MapPost("/{clienteId:guid}/inativar", (HttpContext httpContext, Guid clienteId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CLIENTES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.InativarCliente(clienteId)));
        });
        clientes.MapPost("/{clienteId:guid}/bloquear", (HttpContext httpContext, Guid clienteId, BloquearClienteRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "CLIENTES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.BloquearCliente(clienteId, request)));
        });

        var fornecedores = app.MapGroup("/fornecedores").WithTags("Fornecedores");
        fornecedores.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarFornecedores(new ConsultarFornecedoresRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        fornecedores.MapPost(string.Empty, (HttpContext httpContext, CreateFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FORNECEDORES_MANAGE", request.EmpresaId);
            var response = service.CadastrarFornecedor(request);
            return Results.Created($"/fornecedores/{response.Id}", ApiResponses.Ok(response));
        });
        fornecedores.MapPost("/{fornecedorId:guid}/atualizar", (HttpContext httpContext, Guid fornecedorId, AtualizarFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FORNECEDORES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtualizarFornecedor(fornecedorId, request)));
        });
        fornecedores.MapPost("/{fornecedorId:guid}/ativar", (HttpContext httpContext, Guid fornecedorId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FORNECEDORES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtivarFornecedor(fornecedorId)));
        });
        fornecedores.MapPost("/{fornecedorId:guid}/inativar", (HttpContext httpContext, Guid fornecedorId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FORNECEDORES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.InativarFornecedor(fornecedorId)));
        });
        fornecedores.MapPost("/{fornecedorId:guid}/bloquear", (HttpContext httpContext, Guid fornecedorId, BloquearFornecedorRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FORNECEDORES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.BloquearFornecedor(fornecedorId, request)));
        });

        var depositos = app.MapGroup("/depositos").WithTags("Depositos");
        depositos.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarDepositos(new ConsultarDepositosRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        depositos.MapPost(string.Empty, (HttpContext httpContext, CreateDepositoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "DEPOSITOS_MANAGE", request.EmpresaId);
            var response = service.CadastrarDeposito(request);
            return Results.Created($"/depositos/{response.Id}", ApiResponses.Ok(response));
        });
        depositos.MapPost("/{depositoId:guid}/atualizar", (HttpContext httpContext, Guid depositoId, AtualizarDepositoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "DEPOSITOS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtualizarDeposito(depositoId, request)));
        });
        depositos.MapPost("/{depositoId:guid}/ativar", (HttpContext httpContext, Guid depositoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "DEPOSITOS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtivarDeposito(depositoId)));
        });
        depositos.MapPost("/{depositoId:guid}/inativar", (HttpContext httpContext, Guid depositoId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "DEPOSITOS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.InativarDeposito(depositoId)));
        });

        var identity = app.MapGroup("/identity").WithTags("Identity");
        identity.MapGet("/usuarios", (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarUsuarios(new ConsultarUsuariosRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        identity.MapPost("/usuarios", (HttpContext httpContext, CreateUsuarioRequest request, ErpApplicationService service) =>
        {
            if (!service.PermiteBootstrapIdentity(request.EmpresaId))
            {
                RequirePermission(httpContext, service, "IDENTITY_MANAGE", request.EmpresaId);
            }
            var response = service.CadastrarUsuario(request);
            return Results.Created($"/identity/usuarios/{response.Id}", ApiResponses.Ok(response));
        });
        identity.MapPost("/usuarios/{usuarioId:guid}/senha", (HttpContext httpContext, Guid usuarioId, DefinirSenhaUsuarioRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "IDENTITY_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.DefinirSenhaUsuario(usuarioId, request)));
        });
        identity.MapPost("/usuarios/{usuarioId:guid}/ativar", (HttpContext httpContext, Guid usuarioId, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "IDENTITY_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AtivarUsuario(usuarioId)));
        });
        identity.MapPost("/usuarios/{usuarioId:guid}/bloquear", (HttpContext httpContext, Guid usuarioId, BloquearUsuarioRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "IDENTITY_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.BloquearUsuario(usuarioId, request)));
        });
        identity.MapPost("/usuarios/{usuarioId:guid}/permissoes", (HttpContext httpContext, Guid usuarioId, ConcederPermissaoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "IDENTITY_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.ConcederPermissao(usuarioId, request)));
        });
        identity.MapPost("/auth/login", (LoginRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.Login(request))));
        identity.MapPost("/auth/logout", (LogoutRequest request, ErpApplicationService service) =>
        {
            service.Logout(request);
            return Results.Ok(ApiResponses.Ok(new { success = true }));
        });
        identity.MapPost("/auth/sessao", (ConsultarSessaoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ConsultarSessao(request))));

        var estoque = app.MapGroup("/estoque").WithTags("Estoque");
        estoque.MapGet("/saldos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarSaldos(new ConsultarSaldosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))));
        estoque.MapGet("/movimentos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarMovimentosEstoque(new ConsultarMovimentosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))));
        estoque.MapPost("/saldos", (HttpContext httpContext, CriarSaldoEstoqueRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "ESTOQUE_MANAGE");
            return Results.Created($"/estoque/saldos/{request.ProdutoId}/{request.DepositoId}", ApiResponses.Ok(service.CriarSaldo(request)));
        });
        estoque.MapPost("/saldos/ajustes", (HttpContext httpContext, AjustarSaldoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "ESTOQUE_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AjustarSaldo(request)));
        });
        estoque.MapPost("/saldos/reservas", (HttpContext httpContext, ReservarSaldoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "ESTOQUE_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.ReservarSaldo(request)));
        });
        estoque.MapPost("/saldos/baixas", (HttpContext httpContext, ConfirmarBaixaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "ESTOQUE_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.ConfirmarBaixaFaturamento(request)));
        });
        estoque.MapPost("/transferencias", (HttpContext httpContext, TransferirEstoqueRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "ESTOQUE_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.Transferir(request)));
        });

        var vendas = app.MapGroup("/vendas").WithTags("Vendas");
        vendas.MapGet("/pedidos", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarPedidos(new ConsultarPedidosVendaRequest(status, clienteId, page ?? 1, pageSize ?? 20)))));
        vendas.MapPost("/pedidos", (HttpContext httpContext, CreatePedidoVendaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "VENDAS_MANAGE");
            var response = service.CriarPedido(request);
            return Results.Created($"/vendas/pedidos/{response.Id}", ApiResponses.Ok(response));
        });
        vendas.MapPost("/pedidos/{pedidoId:guid}/itens", (HttpContext httpContext, Guid pedidoId, AddItemPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "VENDAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AdicionarItemPedido(pedidoId, request)));
        });
        vendas.MapPost("/pedidos/{pedidoId:guid}/aprovar", (HttpContext httpContext, Guid pedidoId, AprovarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "VENDAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AprovarPedido(pedidoId, request)));
        });
        vendas.MapPost("/pedidos/{pedidoId:guid}/reservar", (HttpContext httpContext, Guid pedidoId, ReservarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "VENDAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.ReservarPedido(pedidoId, request)));
        });
        vendas.MapPost("/pedidos/{pedidoId:guid}/cancelar", (HttpContext httpContext, Guid pedidoId, CancelarPedidoRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "VENDAS_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.CancelarPedido(pedidoId, request)));
        });

        var compras = app.MapGroup("/compras").WithTags("Compras");
        compras.MapGet("/importacoes-nota-entrada", (Guid? empresaId, Guid? fornecedorId, Guid? depositoId, bool? importadaComSucesso, string? chaveAcesso, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarImportacoesNotaEntrada(new ConsultarImportacoesNotaEntradaRequest(empresaId, fornecedorId, depositoId, importadaComSucesso, chaveAcesso, page ?? 1, pageSize ?? 20)))));
        compras.MapPost("/importacoes-nota-entrada", (HttpContext httpContext, ImportarNotaEntradaRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "COMPRAS_MANAGE", request.EmpresaId);
            return Results.Ok(ApiResponses.Ok(service.ImportarNotaEntrada(request)));
        });

        var fiscal = app.MapGroup("/fiscal").WithTags("Fiscal");
        fiscal.MapGet("/notas", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarNotasFiscais(new ConsultarNotasFiscaisRequest(status, clienteId, page ?? 1, pageSize ?? 20)))));
        fiscal.MapPost("/notas", (HttpContext httpContext, CreateNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FISCAL_MANAGE");
            var response = service.CriarNotaFiscal(request);
            return Results.Created($"/fiscal/notas/{response.Id}", ApiResponses.Ok(response));
        });
        fiscal.MapPost("/notas/{notaFiscalId:guid}/autorizar", (HttpContext httpContext, Guid notaFiscalId, AutorizarNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FISCAL_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.AutorizarNotaFiscal(notaFiscalId, request)));
        });
        fiscal.MapPost("/notas/{notaFiscalId:guid}/rejeitar", (HttpContext httpContext, Guid notaFiscalId, RegistrarRejeicaoNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FISCAL_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.RegistrarRejeicaoNotaFiscal(notaFiscalId, request)));
        });
        fiscal.MapPost("/notas/{notaFiscalId:guid}/cancelar", (HttpContext httpContext, Guid notaFiscalId, CancelarNotaFiscalRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "FISCAL_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.CancelarNotaFiscal(notaFiscalId, request)));
        });

        var integracoes = app.MapGroup("/integracoes").WithTags("Integracoes");
        integracoes.MapGet("/webhooks", (string? origem, string? status, string? eventoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarWebhooks(new ConsultarWebhooksRequest(origem, status, eventoId, page ?? 1, pageSize ?? 20)))));
        integracoes.MapPost("/webhooks", (HttpContext httpContext, ProcessarWebhookRequest request, ErpApplicationService service) =>
        {
            RequirePermission(httpContext, service, "INTEGRACOES_MANAGE");
            return Results.Ok(ApiResponses.Ok(service.ProcessarWebhook(request)));
        });

        return app;
    }

    public static WebApplication UseErpExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                var (statusCode, code, message) = exception switch
                {
                    DomainException domainException => (StatusCodes.Status400BadRequest, "domain_error", domainException.Message),
                    NotFoundException notFoundException => (StatusCodes.Status404NotFound, "not_found", notFoundException.Message),
                    UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, "unauthorized", unauthorizedAccessException.Message),
                    ForbiddenException forbiddenException => (StatusCodes.Status403Forbidden, "forbidden", forbiddenException.Message),
                    _ => (StatusCodes.Status500InternalServerError, "internal_error", "Erro interno da aplicacao.")
                };

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(ApiResponses.Error(code, message));
            });
        });

        return app;
    }

    private static void RequirePermission(HttpContext httpContext, ErpApplicationService service, string permission, Guid? empresaId = null)
    {
        if (!httpContext.Request.Headers.TryGetValue(SessionHeader, out var headerValues) || string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
        {
            throw new UnauthorizedAccessException("Header X-Session-Token e obrigatorio.");
        }

        service.ValidarAcesso(headerValues.First()!, permission, empresaId);
    }
}
