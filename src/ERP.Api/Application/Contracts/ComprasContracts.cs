namespace ERP.Api.Application.Contracts;

public sealed record ImportarNotaEntradaRequest(Guid EmpresaId, Guid FornecedorId, Guid DepositoId, string ChaveAcesso, IReadOnlyCollection<ItemNotaEntradaExternaRequest> ItensExternos, IReadOnlyDictionary<string, Guid> Conciliacoes);
public sealed record ConsultarImportacoesNotaEntradaRequest(Guid? EmpresaId, Guid? FornecedorId, Guid? DepositoId, bool? ImportadaComSucesso, string? ChaveAcesso, int Page = 1, int PageSize = 20);
public sealed record ItemNotaEntradaExternaRequest(string CodigoExterno, string Descricao, decimal Quantidade);
public sealed record ResultadoImportacaoNotaResponse(bool ImportadaComSucesso, IReadOnlyCollection<ItemNotaEntradaExternaResponse> ItensPendentesConciliacao, IReadOnlyCollection<MovimentoEstoqueImportacaoResponse> MovimentosEstoqueGerados);
public sealed record ImportacaoNotaEntradaResponse(Guid EmpresaId, Guid FornecedorId, Guid DepositoId, string ChaveAcesso, bool ImportadaComSucesso, int ItensExternos, int ItensPendentesConciliacao, int MovimentosGerados, DateTimeOffset ProcessadaEm);
public sealed record ItemNotaEntradaExternaResponse(string CodigoExterno, string Descricao, decimal Quantidade);
public sealed record MovimentoEstoqueImportacaoResponse(Guid ProdutoId, Guid DepositoId, decimal Quantidade, decimal SaldoAnterior, decimal SaldoPosterior, string DocumentoOrigem);
