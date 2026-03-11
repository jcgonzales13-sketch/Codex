using ERP.BuildingBlocks;

namespace ERP.Modules.Catalogo;

public enum TipoProduto
{
    Simples,
    Variacao,
    Kit,
    MateriaPrima,
    Servico,
    Digital,
    Brinde
}

public sealed class Produto
{
    public Produto(Guid empresaId, string codigoInterno, string sku, string descricao, TipoProduto tipo, decimal precoVenda, decimal custo, string ncm, string origem)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        CodigoInterno = codigoInterno;
        Sku = sku;
        Descricao = descricao;
        Tipo = tipo;
        PrecoVenda = precoVenda;
        Custo = custo;
        Ncm = ncm;
        Origem = origem;
    }

    public Guid Id { get; }
    public Guid EmpresaId { get; }
    public string CodigoInterno { get; }
    public string Sku { get; }
    public string Descricao { get; }
    public TipoProduto Tipo { get; }
    public decimal PrecoVenda { get; }
    public decimal Custo { get; }
    public string Ncm { get; private set; }
    public string Origem { get; private set; }
    public bool Ativo { get; private set; } = true;
    public IReadOnlyCollection<ProdutoVariacao> Variacoes => _variacoes;
    public IReadOnlyCollection<AuditChange> AuditoriasFiscais => _auditoriasFiscais;

    private readonly List<ProdutoVariacao> _variacoes = [];
    private readonly List<AuditChange> _auditoriasFiscais = [];

    public void Inativar() => Ativo = false;

    public void GarantirQuePodeSerVendido(bool possuiPermissaoEspecial)
    {
        if (!Ativo && !possuiPermissaoEspecial)
        {
            throw new DomainException("Produto inativo nao pode ser vendido sem permissao especial.");
        }
    }

    public void AdicionarVariacao(string sku, string? codigoBarras, decimal? precoVenda)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU da variacao e obrigatorio.");
        }

        _variacoes.Add(new ProdutoVariacao(sku.Trim(), codigoBarras?.Trim(), precoVenda));
    }

    public void AtualizarDadosFiscais(string ncm, string origem)
    {
        var previousValue = $"NCM={Ncm};Origem={Origem}";

        Ncm = ncm.Trim();
        Origem = origem.Trim();

        var currentValue = $"NCM={Ncm};Origem={Origem}";

        if (previousValue != currentValue)
        {
            _auditoriasFiscais.Add(new AuditChange("Fiscal", previousValue, currentValue));
        }
    }
}

public sealed class ProdutoVariacao
{
    public ProdutoVariacao(string sku, string? codigoBarras, decimal? precoVenda)
    {
        Sku = sku;
        CodigoBarras = codigoBarras;
        PrecoVenda = precoVenda;
    }

    public string Sku { get; }
    public string? CodigoBarras { get; }
    public decimal? PrecoVenda { get; }
}

public interface IProdutoRepository
{
    bool SkuJaExiste(Guid empresaId, string sku);
    void Add(Produto produto);
}

public sealed class CadastroProdutoService(IProdutoRepository repository)
{
    public Produto Cadastrar(Guid empresaId, string codigoInterno, string sku, string descricao, TipoProduto tipo, decimal precoVenda, decimal custo, string ncm, string origem)
    {
        if (repository.SkuJaExiste(empresaId, sku))
        {
            throw new DomainException("SKU ja cadastrado para a empresa.");
        }

        var produto = new Produto(empresaId, codigoInterno, sku, descricao, tipo, precoVenda, custo, ncm, origem);
        repository.Add(produto);

        return produto;
    }
}
