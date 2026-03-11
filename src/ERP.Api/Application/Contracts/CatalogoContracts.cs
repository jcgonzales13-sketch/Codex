using ERP.Modules.Catalogo;

namespace ERP.Api.Application.Contracts;

public sealed record CreateProdutoRequest(Guid EmpresaId, string CodigoInterno, string Sku, string Descricao, TipoProduto Tipo, decimal PrecoVenda, decimal Custo, string Ncm, string Origem);
public sealed record AddVariacaoRequest(string Sku, string? CodigoBarras, decimal? PrecoVenda);
public sealed record UpdateFiscalProdutoRequest(string Ncm, string Origem);
public sealed record ConsultarProdutosRequest(Guid? EmpresaId, bool? Ativo, string? Termo, int Page = 1, int PageSize = 20);
public sealed record ProdutoResponse(Guid Id, Guid EmpresaId, string CodigoInterno, string Sku, string Descricao, string Tipo, decimal PrecoVenda, decimal Custo, string Ncm, string Origem, bool Ativo, IReadOnlyCollection<ProdutoVariacaoResponse> Variacoes, IReadOnlyCollection<AuditChangeResponse> AuditoriasFiscais);
public sealed record ProdutoVariacaoResponse(string Sku, string? CodigoBarras, decimal? PrecoVenda);
public sealed record AuditChangeResponse(string Field, string PreviousValue, string CurrentValue);
