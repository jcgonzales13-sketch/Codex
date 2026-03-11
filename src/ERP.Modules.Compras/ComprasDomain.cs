using ERP.BuildingBlocks;

namespace ERP.Modules.Compras;

public interface INotaEntradaRepository
{
    bool ChaveJaImportada(Guid empresaId, string chaveAcesso);
    void RegistrarImportacao(Guid empresaId, string chaveAcesso);
}

public sealed record ItemNotaEntradaExterna(string CodigoExterno, string Descricao, decimal Quantidade);

public sealed class ResultadoImportacaoNota
{
    public ResultadoImportacaoNota(bool importadaComSucesso, IReadOnlyCollection<ItemNotaEntradaExterna> itensPendentesConciliacao)
    {
        ImportadaComSucesso = importadaComSucesso;
        ItensPendentesConciliacao = itensPendentesConciliacao;
    }

    public bool ImportadaComSucesso { get; }
    public IReadOnlyCollection<ItemNotaEntradaExterna> ItensPendentesConciliacao { get; }
}

public sealed class ImportacaoNotaEntradaService(INotaEntradaRepository repository)
{
    public ResultadoImportacaoNota Importar(Guid empresaId, string chaveAcesso, IReadOnlyCollection<ItemNotaEntradaExterna> itensExternos, IReadOnlyDictionary<string, Guid> conciliacoes)
    {
        if (repository.ChaveJaImportada(empresaId, chaveAcesso))
        {
            throw new DomainException("XML da nota de entrada ja foi importado para esta empresa.");
        }

        var pendentes = itensExternos.Where(item => !conciliacoes.ContainsKey(item.CodigoExterno)).ToArray();
        if (pendentes.Length > 0)
        {
            return new ResultadoImportacaoNota(false, pendentes);
        }

        repository.RegistrarImportacao(empresaId, chaveAcesso);
        return new ResultadoImportacaoNota(true, []);
    }
}
