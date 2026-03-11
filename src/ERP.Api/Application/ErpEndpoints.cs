using ERP.BuildingBlocks;
using ERP.Api.Application.Contracts;

namespace ERP.Api.Application;

public static class ErpEndpoints
{
    public static IEndpointRouteBuilder MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var catalogo = app.MapGroup("/catalogo").WithTags("Catalogo");
        catalogo.MapGet("/produtos", (Guid? empresaId, bool? ativo, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarProdutos(new ConsultarProdutosRequest(empresaId, ativo, termo, page ?? 1, pageSize ?? 20)))));
        catalogo.MapPost("/produtos", (CreateProdutoRequest request, ErpApplicationService service) =>
        {
            var response = service.CadastrarProduto(request);
            return Results.Created($"/catalogo/produtos/{response.Id}", ApiResponses.Ok(response));
        });
        catalogo.MapPost("/produtos/{produtoId:guid}/inativar", (Guid produtoId, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.InativarProduto(produtoId))));
        catalogo.MapPost("/produtos/{produtoId:guid}/variacoes", (Guid produtoId, AddVariacaoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AdicionarVariacao(produtoId, request))));
        catalogo.MapPost("/produtos/{produtoId:guid}/dados-fiscais", (Guid produtoId, UpdateFiscalProdutoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AtualizarFiscalProduto(produtoId, request))));

        var clientes = app.MapGroup("/clientes").WithTags("Clientes");
        clientes.MapGet(string.Empty, (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarClientes(new ConsultarClientesRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        clientes.MapPost(string.Empty, (CreateClienteRequest request, ErpApplicationService service) =>
        {
            var response = service.CadastrarCliente(request);
            return Results.Created($"/clientes/{response.Id}", ApiResponses.Ok(response));
        });
        clientes.MapPost("/{clienteId:guid}/atualizar", (Guid clienteId, AtualizarClienteRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AtualizarCliente(clienteId, request))));
        clientes.MapPost("/{clienteId:guid}/ativar", (Guid clienteId, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AtivarCliente(clienteId))));
        clientes.MapPost("/{clienteId:guid}/inativar", (Guid clienteId, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.InativarCliente(clienteId))));
        clientes.MapPost("/{clienteId:guid}/bloquear", (Guid clienteId, BloquearClienteRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.BloquearCliente(clienteId, request))));

        var identity = app.MapGroup("/identity").WithTags("Identity");
        identity.MapGet("/usuarios", (Guid? empresaId, string? status, string? termo, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarUsuarios(new ConsultarUsuariosRequest(empresaId, status, termo, page ?? 1, pageSize ?? 20)))));
        identity.MapPost("/usuarios", (CreateUsuarioRequest request, ErpApplicationService service) =>
        {
            var response = service.CadastrarUsuario(request);
            return Results.Created($"/identity/usuarios/{response.Id}", ApiResponses.Ok(response));
        });
        identity.MapPost("/usuarios/{usuarioId:guid}/ativar", (Guid usuarioId, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AtivarUsuario(usuarioId))));
        identity.MapPost("/usuarios/{usuarioId:guid}/bloquear", (Guid usuarioId, BloquearUsuarioRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.BloquearUsuario(usuarioId, request))));
        identity.MapPost("/usuarios/{usuarioId:guid}/permissoes", (Guid usuarioId, ConcederPermissaoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ConcederPermissao(usuarioId, request))));

        var estoque = app.MapGroup("/estoque").WithTags("Estoque");
        estoque.MapGet("/saldos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarSaldos(new ConsultarSaldosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))));
        estoque.MapGet("/movimentos", (Guid? produtoId, Guid? depositoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarMovimentosEstoque(new ConsultarMovimentosEstoqueRequest(produtoId, depositoId, page ?? 1, pageSize ?? 20)))));
        estoque.MapPost("/saldos", (CriarSaldoEstoqueRequest request, ErpApplicationService service) => Results.Created($"/estoque/saldos/{request.ProdutoId}/{request.DepositoId}", ApiResponses.Ok(service.CriarSaldo(request))));
        estoque.MapPost("/saldos/ajustes", (AjustarSaldoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AjustarSaldo(request))));
        estoque.MapPost("/saldos/reservas", (ReservarSaldoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ReservarSaldo(request))));
        estoque.MapPost("/saldos/baixas", (ConfirmarBaixaRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ConfirmarBaixaFaturamento(request))));
        estoque.MapPost("/transferencias", (TransferirEstoqueRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.Transferir(request))));

        var vendas = app.MapGroup("/vendas").WithTags("Vendas");
        vendas.MapGet("/pedidos", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarPedidos(new ConsultarPedidosVendaRequest(status, clienteId, page ?? 1, pageSize ?? 20)))));
        vendas.MapPost("/pedidos", (CreatePedidoVendaRequest request, ErpApplicationService service) =>
        {
            var response = service.CriarPedido(request);
            return Results.Created($"/vendas/pedidos/{response.Id}", ApiResponses.Ok(response));
        });
        vendas.MapPost("/pedidos/{pedidoId:guid}/itens", (Guid pedidoId, AddItemPedidoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AdicionarItemPedido(pedidoId, request))));
        vendas.MapPost("/pedidos/{pedidoId:guid}/aprovar", (Guid pedidoId, AprovarPedidoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AprovarPedido(pedidoId, request))));
        vendas.MapPost("/pedidos/{pedidoId:guid}/reservar", (Guid pedidoId, ReservarPedidoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ReservarPedido(pedidoId, request))));
        vendas.MapPost("/pedidos/{pedidoId:guid}/cancelar", (Guid pedidoId, CancelarPedidoRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.CancelarPedido(pedidoId, request))));

        var compras = app.MapGroup("/compras").WithTags("Compras");
        compras.MapGet("/importacoes-nota-entrada", (Guid? empresaId, Guid? depositoId, bool? importadaComSucesso, string? chaveAcesso, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarImportacoesNotaEntrada(new ConsultarImportacoesNotaEntradaRequest(empresaId, depositoId, importadaComSucesso, chaveAcesso, page ?? 1, pageSize ?? 20)))));
        compras.MapPost("/importacoes-nota-entrada", (ImportarNotaEntradaRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ImportarNotaEntrada(request))));

        var fiscal = app.MapGroup("/fiscal").WithTags("Fiscal");
        fiscal.MapGet("/notas", (string? status, Guid? clienteId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarNotasFiscais(new ConsultarNotasFiscaisRequest(status, clienteId, page ?? 1, pageSize ?? 20)))));
        fiscal.MapPost("/notas", (CreateNotaFiscalRequest request, ErpApplicationService service) =>
        {
            var response = service.CriarNotaFiscal(request);
            return Results.Created($"/fiscal/notas/{response.Id}", ApiResponses.Ok(response));
        });
        fiscal.MapPost("/notas/{notaFiscalId:guid}/autorizar", (Guid notaFiscalId, AutorizarNotaFiscalRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.AutorizarNotaFiscal(notaFiscalId, request))));
        fiscal.MapPost("/notas/{notaFiscalId:guid}/rejeitar", (Guid notaFiscalId, RegistrarRejeicaoNotaFiscalRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.RegistrarRejeicaoNotaFiscal(notaFiscalId, request))));
        fiscal.MapPost("/notas/{notaFiscalId:guid}/cancelar", (Guid notaFiscalId, CancelarNotaFiscalRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.CancelarNotaFiscal(notaFiscalId, request))));

        var integracoes = app.MapGroup("/integracoes").WithTags("Integracoes");
        integracoes.MapGet("/webhooks", (string? origem, string? status, string? eventoId, int? page, int? pageSize, ErpApplicationService service) =>
            Results.Ok(ApiResponses.Ok(service.ConsultarWebhooks(new ConsultarWebhooksRequest(origem, status, eventoId, page ?? 1, pageSize ?? 20)))));
        integracoes.MapPost("/webhooks", (ProcessarWebhookRequest request, ErpApplicationService service) => Results.Ok(ApiResponses.Ok(service.ProcessarWebhook(request))));

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
                    _ => (StatusCodes.Status500InternalServerError, "internal_error", "Erro interno da aplicacao.")
                };

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(ApiResponses.Error(code, message));
            });
        });

        return app;
    }
}
