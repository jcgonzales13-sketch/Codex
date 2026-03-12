using ERP.Api.Application;
using ERP.Api.Application.Contracts;
using ERP.Api.Application.Storage;
using ERP.Modules.Catalogo;

namespace ERP.Api.UnitTests;

public sealed class StorageSectioningTests
{
    [Fact]
    public void Deve_serializar_em_secoes_por_modulo_e_restaurar_estado()
    {
        var original = new InMemoryErpStore();
        var service = new ErpApplicationService(original);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000801", "Empresa Storage", "Empresa Storage LTDA"));
        var fornecedor = service.CadastrarFornecedor(new CreateFornecedorRequest(empresa.Id, "22000000000801", "Fornecedor Storage", "fornecedorstorage@empresa.com"));
        var deposito = service.CadastrarDeposito(new CreateDepositoRequest(empresa.Id, "DEP-STO", "Deposito Storage"));
        var produto = service.CadastrarProduto(new CreateProdutoRequest(empresa.Id, "PRD-STO", "SKU-STO", "Produto Storage", TipoProduto.Simples, 10m, 5m, "12345678", "0"));

        service.ImportarNotaEntrada(new ImportarNotaEntradaRequest(
            empresa.Id,
            fornecedor.Id,
            deposito.Id,
            "CHAVE-STORAGE-1",
            [new ItemNotaEntradaExternaRequest("EXT-STO-1", "Produto Storage", 2m)],
            new Dictionary<string, Guid> { ["EXT-STO-1"] = produto.Id }));

        var sections = ErpSnapshotSerializer.SerializeSections(original);

        Assert.Contains("empresas", sections.Keys);
        Assert.Contains("catalogo", sections.Keys);
        Assert.Contains("compras", sections.Keys);
        Assert.Contains("estoque", sections.Keys);

        var restored = new InMemoryErpStore();
        ErpSnapshotSerializer.LoadSections(restored, sections);

        Assert.Single(restored.Empresas);
        Assert.Single(restored.Fornecedores);
        Assert.Single(restored.Produtos);
        Assert.Single(restored.Depositos);
        Assert.Single(restored.ImportacoesNotaEntrada);
        Assert.Single(restored.Saldos);
    }

    [Fact]
    public void Deve_manter_compatibilidade_com_snapshot_legado()
    {
        var original = new InMemoryErpStore();
        var service = new ErpApplicationService(original);
        var empresa = service.CadastrarEmpresa(new CreateEmpresaRequest("12345678000802", "Empresa Legado", "Empresa Legado LTDA"));
        service.CadastrarCliente(new CreateClienteRequest(empresa.Id, "10000000802", "Cliente Legado", "clientelegado@empresa.com"));

        var legacyPayload = ErpSnapshotSerializer.Serialize(original);
        var restored = new InMemoryErpStore();
        ErpSnapshotSerializer.Load(restored, legacyPayload);

        Assert.Single(restored.Empresas);
        Assert.Single(restored.Clientes);
    }
}
