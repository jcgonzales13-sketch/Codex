namespace ERP.Api.Application.Contracts;

public sealed record CriarSaldoEstoqueRequest(Guid ProdutoId, Guid DepositoId, decimal SaldoInicial, bool PermiteSaldoNegativo);
public sealed record AjustarSaldoRequest(Guid ProdutoId, Guid DepositoId, decimal Quantidade, string Motivo);
public sealed record ReservarSaldoRequest(Guid ProdutoId, Guid DepositoId, decimal Quantidade, string DocumentoOrigem);
public sealed record ConfirmarBaixaRequest(Guid ProdutoId, Guid DepositoId, decimal Quantidade, string EventoId, string DocumentoOrigem);
public sealed record TransferirEstoqueRequest(Guid ProdutoId, Guid DepositoOrigemId, Guid DepositoDestinoId, decimal Quantidade, string DocumentoOrigem);
public sealed record ConsultarSaldosEstoqueRequest(Guid? ProdutoId, Guid? DepositoId, int Page = 1, int PageSize = 20);
public sealed record ConsultarMovimentosEstoqueRequest(Guid? ProdutoId, Guid? DepositoId, int Page = 1, int PageSize = 20);
public sealed record SaldoEstoqueResponse(Guid ProdutoId, Guid DepositoId, decimal SaldoAtual, decimal Reservado, decimal Disponivel, bool PermiteSaldoNegativo);
public sealed record MovimentoEstoqueResponse(Guid ProdutoId, Guid DepositoId, string Tipo, decimal Quantidade, string Motivo, string DocumentoOrigem, decimal SaldoAnterior, decimal SaldoPosterior, DateTimeOffset DataHora);
public sealed record TransferenciaResponse(MovimentoEstoqueResponse Saida, MovimentoEstoqueResponse Entrada);
