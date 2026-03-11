using ERP.BuildingBlocks;

namespace ERP.Modules.Estoque;

public enum TipoMovimentoEstoque
{
    EntradaManual,
    SaidaManual,
    AjustePositivo,
    AjusteNegativo,
    ReservaPedido,
    LiberacaoReservaPedido,
    BaixaFaturamento,
    TransferenciaSaida,
    TransferenciaEntrada
}

public sealed record MovimentoEstoque(
    Guid ProdutoId,
    Guid DepositoId,
    TipoMovimentoEstoque Tipo,
    decimal Quantidade,
    string Motivo,
    string DocumentoOrigem,
    decimal SaldoAnterior,
    decimal SaldoPosterior,
    DateTimeOffset DataHora);

public sealed class SaldoEstoque
{
    private readonly HashSet<string> _eventosProcessados = [];
    private readonly Dictionary<string, MovimentoEstoque> _movimentosPorEvento = [];

    public SaldoEstoque(Guid produtoId, Guid depositoId, decimal saldoInicial, bool permiteSaldoNegativo)
    {
        ProdutoId = produtoId;
        DepositoId = depositoId;
        SaldoAtual = saldoInicial;
        PermiteSaldoNegativo = permiteSaldoNegativo;
    }

    public Guid ProdutoId { get; }
    public Guid DepositoId { get; }
    public decimal SaldoAtual { get; private set; }
    public decimal Reservado { get; private set; }
    public bool PermiteSaldoNegativo { get; }
    public decimal Disponivel => SaldoAtual - Reservado;

    public MovimentoEstoque Ajustar(decimal quantidade, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new DomainException("Motivo do ajuste de estoque e obrigatorio.");
        }

        var saldoPosterior = SaldoAtual + quantidade;
        if (!PermiteSaldoNegativo && saldoPosterior < 0)
        {
            throw new DomainException("Saldo nao pode ficar negativo para este deposito.");
        }

        var tipo = quantidade >= 0 ? TipoMovimentoEstoque.AjustePositivo : TipoMovimentoEstoque.AjusteNegativo;
        var movimento = new MovimentoEstoque(ProdutoId, DepositoId, tipo, Math.Abs(quantidade), motivo, "AJUSTE-MANUAL", SaldoAtual, saldoPosterior, DateTimeOffset.UtcNow);
        SaldoAtual = saldoPosterior;
        return movimento;
    }

    public MovimentoEstoque Reservar(decimal quantidade, string documentoOrigem)
    {
        if (quantidade <= 0)
        {
            throw new DomainException("Quantidade da reserva deve ser maior que zero.");
        }

        if (Disponivel < quantidade)
        {
            throw new DomainException("Estoque insuficiente para reserva.");
        }

        var saldoAnterior = Reservado;
        Reservado += quantidade;
        return new MovimentoEstoque(ProdutoId, DepositoId, TipoMovimentoEstoque.ReservaPedido, quantidade, "Reserva de pedido", documentoOrigem, saldoAnterior, Reservado, DateTimeOffset.UtcNow);
    }

    public MovimentoEstoque LiberarReserva(decimal quantidade, string documentoOrigem)
    {
        if (quantidade <= 0)
        {
            throw new DomainException("Quantidade da liberacao de reserva deve ser maior que zero.");
        }

        if (Reservado < quantidade)
        {
            throw new DomainException("Quantidade para liberacao excede o estoque reservado.");
        }

        var reservadoAnterior = Reservado;
        Reservado -= quantidade;
        return new MovimentoEstoque(ProdutoId, DepositoId, TipoMovimentoEstoque.LiberacaoReservaPedido, quantidade, "Liberacao de reserva", documentoOrigem, reservadoAnterior, Reservado, DateTimeOffset.UtcNow);
    }

    public MovimentoEstoque ConfirmarBaixaFaturamento(decimal quantidade, string eventoId, string documentoOrigem)
    {
        if (_eventosProcessados.Contains(eventoId))
        {
            return _movimentosPorEvento[eventoId];
        }

        var saldoPosterior = SaldoAtual - quantidade;
        if (!PermiteSaldoNegativo && saldoPosterior < 0)
        {
            throw new DomainException("Saldo nao pode ficar negativo para este deposito.");
        }

        var movimento = new MovimentoEstoque(ProdutoId, DepositoId, TipoMovimentoEstoque.BaixaFaturamento, quantidade, "Baixa por faturamento", documentoOrigem, SaldoAtual, saldoPosterior, DateTimeOffset.UtcNow);
        SaldoAtual = saldoPosterior;
        Reservado = Math.Max(0, Reservado - quantidade);
        _eventosProcessados.Add(eventoId);
        _movimentosPorEvento[eventoId] = movimento;
        return movimento;
    }
}

public sealed class TransferenciaEstoqueService
{
    public (MovimentoEstoque saida, MovimentoEstoque entrada) Transferir(SaldoEstoque origem, SaldoEstoque destino, decimal quantidade, string documentoOrigem)
    {
        if (origem.ProdutoId != destino.ProdutoId)
        {
            throw new DomainException("Transferencia deve ocorrer para o mesmo produto.");
        }

        var saida = origem.Ajustar(-quantidade, "Transferencia entre depositos");
        var entrada = destino.Ajustar(quantidade, "Transferencia entre depositos");

        return
        (
            saida with { Tipo = TipoMovimentoEstoque.TransferenciaSaida, DocumentoOrigem = documentoOrigem },
            entrada with { Tipo = TipoMovimentoEstoque.TransferenciaEntrada, DocumentoOrigem = documentoOrigem }
        );
    }
}
