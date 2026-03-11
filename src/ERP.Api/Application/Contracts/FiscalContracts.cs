namespace ERP.Api.Application.Contracts;

public sealed record CreateNotaFiscalRequest(Guid PedidoVendaId, Guid ClienteId, IReadOnlyCollection<ItemNotaFiscalRequest> Itens);
public sealed record ItemNotaFiscalRequest(Guid ProdutoId, decimal Quantidade, string Ncm, string Cfop);
public sealed record AutorizarNotaFiscalRequest(Guid DepositoId, string EventoId);
public sealed record RegistrarRejeicaoNotaFiscalRequest(string Codigo, string Mensagem);
public sealed record CancelarNotaFiscalRequest(string Justificativa, bool EstornarImpactosOperacionais);
public sealed record ConsultarNotasFiscaisRequest(string? Status, Guid? ClienteId, int Page = 1, int PageSize = 20);
public sealed record NotaFiscalResponse(Guid Id, Guid PedidoVendaId, Guid ClienteId, string Status, string? CodigoRejeicao, string? MensagemRejeicao, bool EstoqueBaixado, string? JustificativaCancelamento, IReadOnlyCollection<ItemNotaFiscalResponse> Itens, IReadOnlyCollection<string> HistoricoTentativas);
public sealed record ItemNotaFiscalResponse(Guid ProdutoId, decimal Quantidade, string Ncm, string Cfop);
