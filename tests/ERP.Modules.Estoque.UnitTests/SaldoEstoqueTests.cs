using ERP.BuildingBlocks;
using ERP.Modules.Estoque;

namespace ERP.Modules.Estoque.UnitTests;

public sealed class SaldoEstoqueTests
{
    [Fact]
    public void Nao_deve_permitir_saldo_negativo_quando_empresa_nao_permitir()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 5m, permiteSaldoNegativo: false);

        var exception = Assert.Throws<DomainException>(() => saldo.Ajustar(-10m, "Perda por avaria"));

        Assert.Equal("Saldo nao pode ficar negativo para este deposito.", exception.Message);
    }

    [Fact]
    public void Todo_ajuste_deve_exigir_motivo()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 5m, permiteSaldoNegativo: true);

        var exception = Assert.Throws<DomainException>(() => saldo.Ajustar(-1m, string.Empty));

        Assert.Equal("Motivo do ajuste de estoque e obrigatorio.", exception.Message);
    }

    [Fact]
    public void Baixa_por_faturamento_deve_ser_idempotente()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 10m, permiteSaldoNegativo: false);

        var primeiro = saldo.ConfirmarBaixaFaturamento(2m, "evt-1", "NF-1");
        var segundo = saldo.ConfirmarBaixaFaturamento(2m, "evt-1", "NF-1");

        Assert.Equal(8m, saldo.SaldoAtual);
        Assert.Equal(primeiro, segundo);
    }

    [Fact]
    public void Baixa_por_faturamento_deve_consumir_reserva_existente()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 10m, permiteSaldoNegativo: false);
        saldo.Reservar(3m, "PED-1");

        saldo.ConfirmarBaixaFaturamento(2m, "evt-2", "NF-2");

        Assert.Equal(8m, saldo.SaldoAtual);
        Assert.Equal(1m, saldo.Reservado);
        Assert.Equal(7m, saldo.Disponivel);
    }

    [Fact]
    public void Liberacao_de_reserva_deve_reduzir_quantidade_reservada()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 10m, permiteSaldoNegativo: false);
        saldo.Reservar(4m, "PED-2");

        var movimento = saldo.LiberarReserva(2m, "PED-2");

        Assert.Equal(TipoMovimentoEstoque.LiberacaoReservaPedido, movimento.Tipo);
        Assert.Equal(2m, saldo.Reservado);
        Assert.Equal(8m, saldo.Disponivel);
    }

    [Fact]
    public void Nao_deve_liberar_reserva_acima_do_quantitativo_reservado()
    {
        var saldo = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 10m, permiteSaldoNegativo: false);
        saldo.Reservar(1m, "PED-3");

        var exception = Assert.Throws<DomainException>(() => saldo.LiberarReserva(2m, "PED-3"));

        Assert.Equal("Quantidade para liberacao excede o estoque reservado.", exception.Message);
    }

    [Fact]
    public void Transferencia_deve_gerar_saida_e_entrada_vinculadas()
    {
        var origem = new SaldoEstoque(Guid.NewGuid(), Guid.NewGuid(), 8m, permiteSaldoNegativo: false);
        var destino = new SaldoEstoque(origem.ProdutoId, Guid.NewGuid(), 1m, permiteSaldoNegativo: false);
        var service = new TransferenciaEstoqueService();

        var (saida, entrada) = service.Transferir(origem, destino, 3m, "TRF-1");

        Assert.Equal(TipoMovimentoEstoque.TransferenciaSaida, saida.Tipo);
        Assert.Equal(TipoMovimentoEstoque.TransferenciaEntrada, entrada.Tipo);
        Assert.Equal("TRF-1", saida.DocumentoOrigem);
        Assert.Equal("TRF-1", entrada.DocumentoOrigem);
        Assert.Equal(5m, origem.SaldoAtual);
        Assert.Equal(4m, destino.SaldoAtual);
    }
}
