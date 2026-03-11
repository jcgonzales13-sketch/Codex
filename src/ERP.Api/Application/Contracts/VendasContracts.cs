namespace ERP.Api.Application.Contracts;

public sealed record CreatePedidoVendaRequest(Guid ClienteId);
public sealed record AddItemPedidoRequest(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario);
public sealed record AprovarPedidoRequest(bool ClienteAtivo);
public sealed record ReservarPedidoRequest(Guid DepositoId);
public sealed record CancelarPedidoRequest(Guid? DepositoId, bool LiberarReservaEstoque);
public sealed record ConsultarPedidosVendaRequest(string? Status, Guid? ClienteId, int Page = 1, int PageSize = 20);
public sealed record PedidoVendaResponse(Guid Id, Guid ClienteId, string Status, IReadOnlyCollection<ItemPedidoVendaResponse> Itens);
public sealed record ItemPedidoVendaResponse(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario);
