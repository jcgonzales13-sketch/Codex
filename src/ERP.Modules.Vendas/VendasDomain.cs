using ERP.BuildingBlocks;

namespace ERP.Modules.Vendas;

public enum StatusPedidoVenda
{
    Rascunho,
    Aprovado,
    Reservado,
    Faturado,
    Cancelado
}

public sealed record ItemPedidoVenda(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario);

public sealed class PedidoVenda
{
    private readonly List<ItemPedidoVenda> _itens = [];

    public PedidoVenda(Guid clienteId)
    {
        Id = Guid.NewGuid();
        ClienteId = clienteId;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public StatusPedidoVenda Status { get; private set; } = StatusPedidoVenda.Rascunho;
    public IReadOnlyCollection<ItemPedidoVenda> Itens => _itens;

    public void AdicionarItem(Guid produtoId, decimal quantidade, decimal precoUnitario)
    {
        if (produtoId == Guid.Empty || quantidade <= 0 || precoUnitario < 0)
        {
            throw new DomainException("Item de pedido invalido.");
        }

        _itens.Add(new ItemPedidoVenda(produtoId, quantidade, precoUnitario));
    }

    public void Aprovar(bool clienteAtivo)
    {
        if (Status == StatusPedidoVenda.Aprovado)
        {
            return;
        }

        if (Status == StatusPedidoVenda.Reservado || Status == StatusPedidoVenda.Faturado)
        {
            return;
        }

        if (!clienteAtivo)
        {
            throw new DomainException("Cliente inativo nao pode ter pedido aprovado.");
        }

        if (_itens.Count == 0)
        {
            throw new DomainException("Pedido deve possuir ao menos um item.");
        }

        Status = StatusPedidoVenda.Aprovado;
    }

    public void Reservar(Func<Guid, decimal, bool> validarEstoqueDisponivel)
    {
        if (Status != StatusPedidoVenda.Aprovado)
        {
            throw new DomainException("Somente pedidos aprovados podem ser reservados.");
        }

        var possuiEstoqueSuficiente = _itens.All(item => validarEstoqueDisponivel(item.ProdutoId, item.Quantidade));
        if (!possuiEstoqueSuficiente)
        {
            throw new DomainException("Estoque insuficiente para reservar o pedido.");
        }

        Status = StatusPedidoVenda.Reservado;
    }

    public void Faturar()
    {
        if (Status == StatusPedidoVenda.Faturado)
        {
            return;
        }

        if (Status != StatusPedidoVenda.Reservado)
        {
            throw new DomainException("Somente pedidos reservados podem ser faturados.");
        }

        Status = StatusPedidoVenda.Faturado;
    }

    public void Cancelar()
    {
        if (Status == StatusPedidoVenda.Cancelado)
        {
            return;
        }

        if (Status == StatusPedidoVenda.Faturado)
        {
            throw new DomainException("Pedido faturado nao pode ser cancelado.");
        }

        Status = StatusPedidoVenda.Cancelado;
    }
}
