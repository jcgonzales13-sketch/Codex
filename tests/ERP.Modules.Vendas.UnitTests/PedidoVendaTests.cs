using ERP.BuildingBlocks;
using ERP.Modules.Vendas;

namespace ERP.Modules.Vendas.UnitTests;

public sealed class PedidoVendaTests
{
    [Fact]
    public void Deve_bloquear_aprovacao_de_pedido_para_cliente_inativo_quando_configurado()
    {
        var pedido = new PedidoVenda(Guid.NewGuid());
        pedido.AdicionarItem(Guid.NewGuid(), 1m, 100m);

        var exception = Assert.Throws<DomainException>(() => pedido.Aprovar(clienteAtivo: false));

        Assert.Equal("Cliente inativo nao pode ter pedido aprovado.", exception.Message);
    }

    [Fact]
    public void Deve_reservar_pedido_quando_houver_estoque_disponivel()
    {
        var produtoId = Guid.NewGuid();
        var pedido = new PedidoVenda(Guid.NewGuid());
        pedido.AdicionarItem(produtoId, 2m, 100m);
        pedido.Aprovar(clienteAtivo: true);

        pedido.Reservar((id, quantidade) => id == produtoId && quantidade <= 5m);

        Assert.Equal(StatusPedidoVenda.Reservado, pedido.Status);
    }

    [Fact]
    public void Deve_impedir_reserva_quando_nao_houver_estoque_disponivel()
    {
        var pedido = new PedidoVenda(Guid.NewGuid());
        pedido.AdicionarItem(Guid.NewGuid(), 2m, 100m);
        pedido.Aprovar(clienteAtivo: true);

        var exception = Assert.Throws<DomainException>(() => pedido.Reservar((_, _) => false));

        Assert.Equal("Estoque insuficiente para reservar o pedido.", exception.Message);
    }
}
