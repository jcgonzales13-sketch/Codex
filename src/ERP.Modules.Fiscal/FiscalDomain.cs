using ERP.BuildingBlocks;

namespace ERP.Modules.Fiscal;

public enum StatusNotaFiscal
{
    Pendente,
    Autorizada,
    Rejeitada,
    Cancelada
}

public sealed record ItemNotaFiscal(Guid ProdutoId, decimal Quantidade, string Ncm, string Cfop);

public sealed class NotaFiscal
{
    private readonly List<string> _historicoTentativas = [];

    public NotaFiscal(Guid pedidoVendaId, Guid clienteId, IReadOnlyCollection<ItemNotaFiscal> itens)
    {
        if (clienteId == Guid.Empty || itens.Count == 0)
        {
            throw new DomainException("Nota fiscal requer cliente valido e ao menos um item.");
        }

        Id = Guid.NewGuid();
        PedidoVendaId = pedidoVendaId;
        ClienteId = clienteId;
        Itens = itens;
    }

    public Guid Id { get; }
    public Guid PedidoVendaId { get; }
    public Guid ClienteId { get; }
    public IReadOnlyCollection<ItemNotaFiscal> Itens { get; }
    public StatusNotaFiscal Status { get; private set; } = StatusNotaFiscal.Pendente;
    public string? CodigoRejeicao { get; private set; }
    public string? MensagemRejeicao { get; private set; }
    public bool EstoqueBaixado { get; private set; }
    public string? JustificativaCancelamento { get; private set; }
    public IReadOnlyCollection<string> HistoricoTentativas => _historicoTentativas;

    public void Autorizar()
    {
        Status = StatusNotaFiscal.Autorizada;
        EstoqueBaixado = true;
        _historicoTentativas.Add("Autorizada");
    }

    public void RegistrarRejeicao(string codigo, string mensagem)
    {
        Status = StatusNotaFiscal.Rejeitada;
        CodigoRejeicao = codigo;
        MensagemRejeicao = mensagem;
        EstoqueBaixado = false;
        _historicoTentativas.Add($"{codigo}:{mensagem}");
    }

    public void Cancelar(string justificativa, bool estornarImpactosOperacionais)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
        {
            throw new DomainException("Justificativa de cancelamento e obrigatoria.");
        }

        Status = StatusNotaFiscal.Cancelada;
        JustificativaCancelamento = justificativa.Trim();
        if (estornarImpactosOperacionais)
        {
            EstoqueBaixado = false;
        }

        _historicoTentativas.Add($"Cancelada:{JustificativaCancelamento}");
    }
}
