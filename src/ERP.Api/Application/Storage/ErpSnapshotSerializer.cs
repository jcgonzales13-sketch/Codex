using System.Reflection;
using System.Text.Json;
using ERP.Api.Application.Integration;
using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Depositos;
using ERP.Modules.Empresas;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Vendas;

namespace ERP.Api.Application.Storage;

internal static class ErpSnapshotSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string Serialize(IErpStore store)
    {
        var snapshot = new ErpSnapshot(
            store.Empresas.Values.Select(ToSnapshot).ToArray(),
            store.Produtos.Values.Select(ToSnapshot).ToArray(),
            store.Clientes.Values.Select(ToSnapshot).ToArray(),
            store.Depositos.Values.Select(ToSnapshot).ToArray(),
            store.Usuarios.Values.Select(ToSnapshot).ToArray(),
            store.Pedidos.Values.Select(ToSnapshot).ToArray(),
            store.NotasFiscais.Values.Select(ToSnapshot).ToArray(),
            store.Saldos.Values.Select(ToSnapshot).ToArray(),
            store.MovimentosEstoque.Select(ToSnapshot).ToArray(),
            store.ChavesImportadas.Select(item => new ChaveImportadaSnapshot(item.EmpresaId, item.ChaveAcesso)).ToArray(),
            store.EventosWebhook.ToArray(),
            store.ImportacoesNotaEntrada.Select(ToSnapshot).ToArray(),
            store.WebhooksProcessados.Select(ToSnapshot).ToArray(),
            store.IntegrationEvents.Select(ToSnapshot).ToArray());

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static void Load(IErpStore store, string json)
    {
        var snapshot = JsonSerializer.Deserialize<ErpSnapshot>(json, JsonOptions);
        if (snapshot is null)
        {
            return;
        }

        store.Empresas.Clear();
        store.Produtos.Clear();
        store.Clientes.Clear();
        store.Depositos.Clear();
        store.Usuarios.Clear();
        store.Pedidos.Clear();
        store.NotasFiscais.Clear();
        store.Saldos.Clear();
        store.MovimentosEstoque.Clear();
        store.ChavesImportadas.Clear();
        store.EventosWebhook.Clear();
        store.ImportacoesNotaEntrada.Clear();
        store.WebhooksProcessados.Clear();
        store.IntegrationEvents.Clear();

        foreach (var empresaSnapshot in snapshot.Empresas ?? Array.Empty<EmpresaSnapshot>())
        {
            var empresa = RestoreEmpresa(empresaSnapshot);
            store.Empresas[empresa.Id] = empresa;
        }

        foreach (var produtoSnapshot in snapshot.Produtos ?? Array.Empty<ProdutoSnapshot>())
        {
            var produto = RestoreProduto(produtoSnapshot);
            store.Produtos[produto.Id] = produto;
        }

        foreach (var clienteSnapshot in snapshot.Clientes ?? Array.Empty<ClienteSnapshot>())
        {
            var cliente = RestoreCliente(clienteSnapshot);
            store.Clientes[cliente.Id] = cliente;
        }

        foreach (var depositoSnapshot in snapshot.Depositos ?? Array.Empty<DepositoSnapshot>())
        {
            var deposito = RestoreDeposito(depositoSnapshot);
            store.Depositos[deposito.Id] = deposito;
        }

        foreach (var usuarioSnapshot in snapshot.Usuarios ?? Array.Empty<UsuarioSnapshot>())
        {
            var usuario = RestoreUsuario(usuarioSnapshot);
            store.Usuarios[usuario.Id] = usuario;
        }

        foreach (var pedidoSnapshot in snapshot.Pedidos ?? Array.Empty<PedidoSnapshot>())
        {
            var pedido = RestorePedido(pedidoSnapshot);
            store.Pedidos[pedido.Id] = pedido;
        }

        foreach (var notaSnapshot in snapshot.NotasFiscais ?? Array.Empty<NotaFiscalSnapshot>())
        {
            var nota = RestoreNotaFiscal(notaSnapshot);
            store.NotasFiscais[nota.Id] = nota;
        }

        foreach (var saldoSnapshot in snapshot.Saldos ?? Array.Empty<SaldoSnapshot>())
        {
            var saldo = RestoreSaldo(saldoSnapshot);
            store.Saldos[(saldo.ProdutoId, saldo.DepositoId)] = saldo;
        }

        foreach (var movimento in snapshot.MovimentosEstoque ?? Array.Empty<MovimentoEstoqueSnapshot>())
        {
            store.MovimentosEstoque.Add(new MovimentoEstoque(
                movimento.ProdutoId,
                movimento.DepositoId,
                Enum.Parse<TipoMovimentoEstoque>(movimento.Tipo),
                movimento.Quantidade,
                movimento.Motivo,
                movimento.DocumentoOrigem,
                movimento.SaldoAnterior,
                movimento.SaldoPosterior,
                movimento.DataHora));
        }

        foreach (var chave in snapshot.ChavesImportadas ?? Array.Empty<ChaveImportadaSnapshot>())
        {
            store.ChavesImportadas.Add((chave.EmpresaId, chave.ChaveAcesso));
        }

        foreach (var eventoId in snapshot.EventosWebhook ?? Array.Empty<string>())
        {
            store.EventosWebhook.Add(eventoId);
        }

        foreach (var importacao in snapshot.ImportacoesNotaEntrada ?? Array.Empty<ImportacaoNotaEntradaSnapshot>())
        {
            store.ImportacoesNotaEntrada.Add(new ImportacaoNotaEntradaRegistro(
                importacao.EmpresaId,
                importacao.DepositoId,
                importacao.ChaveAcesso,
                importacao.ImportadaComSucesso,
                importacao.ItensExternos,
                importacao.ItensPendentesConciliacao,
                importacao.MovimentosGerados,
                importacao.ProcessadaEm));
        }

        foreach (var webhook in snapshot.WebhooksProcessados ?? Array.Empty<WebhookProcessadoSnapshot>())
        {
            store.WebhooksProcessados.Add(new WebhookProcessadoRegistro(
                webhook.EventoId,
                webhook.Origem,
                webhook.Status,
                webhook.Mensagem,
                webhook.ProcessadoEm));
        }

        foreach (var integrationEvent in snapshot.IntegrationEvents ?? Array.Empty<IntegrationEventSnapshot>())
        {
            store.IntegrationEvents.Add(new IntegrationEvent(
                integrationEvent.Id,
                integrationEvent.Type,
                integrationEvent.SourceModule,
                integrationEvent.AggregateId,
                integrationEvent.Description,
                integrationEvent.OccurredAt));
        }
    }

    private static EmpresaSnapshot ToSnapshot(Empresa empresa)
    {
        return new EmpresaSnapshot(empresa.Id, empresa.Documento, empresa.NomeFantasia, empresa.RazaoSocial, empresa.Status.ToString());
    }

    private static ProdutoSnapshot ToSnapshot(Produto produto)
    {
        return new ProdutoSnapshot(
            produto.Id,
            produto.EmpresaId,
            produto.CodigoInterno,
            produto.Sku,
            produto.Descricao,
            produto.Tipo.ToString(),
            produto.PrecoVenda,
            produto.Custo,
            produto.Ncm,
            produto.Origem,
            produto.Ativo,
            produto.Variacoes.Select(variacao => new ProdutoVariacaoSnapshot(variacao.Sku, variacao.CodigoBarras, variacao.PrecoVenda)).ToArray(),
            produto.AuditoriasFiscais.Select(auditoria => new AuditChangeSnapshot(auditoria.Field, auditoria.PreviousValue, auditoria.CurrentValue)).ToArray());
    }

    private static UsuarioSnapshot ToSnapshot(Usuario usuario)
    {
        return new UsuarioSnapshot(usuario.Id, usuario.EmpresaId, usuario.Email, usuario.Nome, usuario.Status.ToString(), usuario.UltimoBloqueioEm, usuario.Permissoes.ToArray());
    }

    private static ClienteSnapshot ToSnapshot(Cliente cliente)
    {
        return new ClienteSnapshot(cliente.Id, cliente.EmpresaId, cliente.Documento, cliente.Nome, cliente.Email, cliente.Status.ToString(), cliente.UltimoBloqueioEm);
    }

    private static DepositoSnapshot ToSnapshot(Deposito deposito)
    {
        return new DepositoSnapshot(deposito.Id, deposito.EmpresaId, deposito.Codigo, deposito.Nome, deposito.Status.ToString());
    }

    private static PedidoSnapshot ToSnapshot(PedidoVenda pedido)
    {
        return new PedidoSnapshot(pedido.Id, pedido.ClienteId, pedido.Status.ToString(), pedido.Itens.Select(item => new ItemPedidoSnapshot(item.ProdutoId, item.Quantidade, item.PrecoUnitario)).ToArray());
    }

    private static NotaFiscalSnapshot ToSnapshot(NotaFiscal nota)
    {
        return new NotaFiscalSnapshot(
            nota.Id,
            nota.PedidoVendaId,
            nota.ClienteId,
            nota.Status.ToString(),
            nota.CodigoRejeicao,
            nota.MensagemRejeicao,
            nota.EstoqueBaixado,
            nota.JustificativaCancelamento,
            nota.Itens.Select(item => new ItemNotaFiscalSnapshot(item.ProdutoId, item.Quantidade, item.Ncm, item.Cfop)).ToArray(),
            nota.HistoricoTentativas.ToArray());
    }

    private static SaldoSnapshot ToSnapshot(SaldoEstoque saldo)
    {
        return new SaldoSnapshot(saldo.ProdutoId, saldo.DepositoId, saldo.SaldoAtual, saldo.Reservado, saldo.PermiteSaldoNegativo);
    }

    private static MovimentoEstoqueSnapshot ToSnapshot(MovimentoEstoque movimento)
    {
        return new MovimentoEstoqueSnapshot(
            movimento.ProdutoId,
            movimento.DepositoId,
            movimento.Tipo.ToString(),
            movimento.Quantidade,
            movimento.Motivo,
            movimento.DocumentoOrigem,
            movimento.SaldoAnterior,
            movimento.SaldoPosterior,
            movimento.DataHora);
    }

    private static IntegrationEventSnapshot ToSnapshot(IntegrationEvent integrationEvent)
    {
        return new IntegrationEventSnapshot(
            integrationEvent.Id,
            integrationEvent.Type,
            integrationEvent.SourceModule,
            integrationEvent.AggregateId,
            integrationEvent.Description,
            integrationEvent.OccurredAt);
    }

    private static ImportacaoNotaEntradaSnapshot ToSnapshot(ImportacaoNotaEntradaRegistro importacao)
    {
        return new ImportacaoNotaEntradaSnapshot(
            importacao.EmpresaId,
            importacao.DepositoId,
            importacao.ChaveAcesso,
            importacao.ImportadaComSucesso,
            importacao.ItensExternos,
            importacao.ItensPendentesConciliacao,
            importacao.MovimentosGerados,
            importacao.ProcessadaEm);
    }

    private static WebhookProcessadoSnapshot ToSnapshot(WebhookProcessadoRegistro webhook)
    {
        return new WebhookProcessadoSnapshot(
            webhook.EventoId,
            webhook.Origem,
            webhook.Status,
            webhook.Mensagem,
            webhook.ProcessadoEm);
    }

    private static Empresa RestoreEmpresa(EmpresaSnapshot snapshot)
    {
        var empresa = new Empresa(snapshot.Documento, snapshot.NomeFantasia, snapshot.RazaoSocial);
        switch (snapshot.Status)
        {
            case nameof(StatusEmpresa.Inativa):
                empresa.Inativar();
                break;
            case nameof(StatusEmpresa.Bloqueada):
                empresa.Bloquear();
                break;
        }

        SetId(empresa, snapshot.Id);
        return empresa;
    }

    private static Produto RestoreProduto(ProdutoSnapshot snapshot)
    {
        var dadosIniciais = snapshot.AuditoriasFiscais.Count > 0
            ? ParseFiscal(snapshot.AuditoriasFiscais.First().PreviousValue)
            : (snapshot.Ncm, snapshot.Origem);

        var produto = new Produto(snapshot.EmpresaId, snapshot.CodigoInterno, snapshot.Sku, snapshot.Descricao, Enum.Parse<TipoProduto>(snapshot.Tipo), snapshot.PrecoVenda, snapshot.Custo, dadosIniciais.Ncm, dadosIniciais.Origem);

        foreach (var variacao in snapshot.Variacoes)
        {
            produto.AdicionarVariacao(variacao.Sku, variacao.CodigoBarras, variacao.PrecoVenda);
        }

        foreach (var auditoria in snapshot.AuditoriasFiscais)
        {
            var fiscal = ParseFiscal(auditoria.CurrentValue);
            produto.AtualizarDadosFiscais(fiscal.Ncm, fiscal.Origem);
        }

        if (!snapshot.Ativo)
        {
            produto.Inativar();
        }

        SetId(produto, snapshot.Id);
        return produto;
    }

    private static Usuario RestoreUsuario(UsuarioSnapshot snapshot)
    {
        var usuario = new Usuario(snapshot.EmpresaId, snapshot.Email, snapshot.Nome);
        if (!string.Equals(snapshot.Status, nameof(StatusUsuario.PendenteAtivacao), StringComparison.OrdinalIgnoreCase))
        {
            usuario.Ativar();
        }

        foreach (var permissao in snapshot.Permissoes)
        {
            if (string.Equals(snapshot.Status, nameof(StatusUsuario.PendenteAtivacao), StringComparison.OrdinalIgnoreCase))
            {
                usuario.Ativar();
            }

            usuario.ConcederPermissao(permissao);
        }

        if (string.Equals(snapshot.Status, nameof(StatusUsuario.Bloqueado), StringComparison.OrdinalIgnoreCase))
        {
            usuario.Bloquear("Restaurado de persistencia");
            if (snapshot.UltimoBloqueioEm is not null)
            {
                SetBackingField(usuario, "<UltimoBloqueioEm>k__BackingField", snapshot.UltimoBloqueioEm);
            }
        }

        SetId(usuario, snapshot.Id);
        return usuario;
    }

    private static Cliente RestoreCliente(ClienteSnapshot snapshot)
    {
        var cliente = new Cliente(snapshot.EmpresaId, snapshot.Documento, snapshot.Nome, snapshot.Email);
        switch (snapshot.Status)
        {
            case nameof(StatusCliente.Inativo):
                cliente.Inativar();
                break;
            case nameof(StatusCliente.Bloqueado):
                cliente.Bloquear("Restaurado de persistencia");
                if (snapshot.UltimoBloqueioEm is not null)
                {
                    SetBackingField(cliente, "<UltimoBloqueioEm>k__BackingField", snapshot.UltimoBloqueioEm);
                }
                break;
        }

        SetId(cliente, snapshot.Id);
        return cliente;
    }

    private static Deposito RestoreDeposito(DepositoSnapshot snapshot)
    {
        var deposito = new Deposito(snapshot.EmpresaId, snapshot.Codigo, snapshot.Nome);
        if (string.Equals(snapshot.Status, nameof(StatusDeposito.Inativo), StringComparison.OrdinalIgnoreCase))
        {
            deposito.Inativar();
        }

        SetId(deposito, snapshot.Id);
        return deposito;
    }

    private static PedidoVenda RestorePedido(PedidoSnapshot snapshot)
    {
        var pedido = new PedidoVenda(snapshot.ClienteId);

        foreach (var item in snapshot.Itens)
        {
            pedido.AdicionarItem(item.ProdutoId, item.Quantidade, item.PrecoUnitario);
        }

        if (!string.Equals(snapshot.Status, nameof(StatusPedidoVenda.Rascunho), StringComparison.OrdinalIgnoreCase))
        {
            pedido.Aprovar(true);
        }

        if (string.Equals(snapshot.Status, nameof(StatusPedidoVenda.Reservado), StringComparison.OrdinalIgnoreCase))
        {
            pedido.Reservar((_, _) => true);
        }

        SetId(pedido, snapshot.Id);
        return pedido;
    }

    private static NotaFiscal RestoreNotaFiscal(NotaFiscalSnapshot snapshot)
    {
        var nota = new NotaFiscal(snapshot.PedidoVendaId, snapshot.ClienteId, snapshot.Itens.Select(item => new ItemNotaFiscal(item.ProdutoId, item.Quantidade, item.Ncm, item.Cfop)).ToArray());

        foreach (var historico in snapshot.HistoricoTentativas)
        {
            if (historico == "Autorizada")
            {
                nota.Autorizar();
                continue;
            }

            if (historico.StartsWith("Cancelada:", StringComparison.Ordinal))
            {
                nota.Cancelar(historico["Cancelada:".Length..], estornarImpactosOperacionais: !snapshot.EstoqueBaixado);
                continue;
            }

            var partes = historico.Split(':', 2);
            if (partes.Length == 2)
            {
                nota.RegistrarRejeicao(partes[0], partes[1]);
            }
        }

        SetId(nota, snapshot.Id);
        return nota;
    }

    private static SaldoEstoque RestoreSaldo(SaldoSnapshot snapshot)
    {
        var saldoBase = snapshot.SaldoAtual + snapshot.Reservado;
        var saldo = new SaldoEstoque(snapshot.ProdutoId, snapshot.DepositoId, saldoBase, snapshot.PermiteSaldoNegativo);
        if (snapshot.Reservado > 0)
        {
            saldo.Reservar(snapshot.Reservado, "RESTORE");
        }

        if (snapshot.SaldoAtual != saldo.SaldoAtual)
        {
            saldo.Ajustar(snapshot.SaldoAtual - saldo.SaldoAtual, "RESTORE");
        }

        return saldo;
    }

    private static (string Ncm, string Origem) ParseFiscal(string value)
    {
        var partes = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var ncm = partes.FirstOrDefault(parte => parte.StartsWith("NCM=", StringComparison.Ordinal))?.Split('=')[1] ?? string.Empty;
        var origem = partes.FirstOrDefault(parte => parte.StartsWith("Origem=", StringComparison.Ordinal))?.Split('=')[1] ?? string.Empty;
        return (ncm, origem);
    }

    private static void SetId<T>(T instance, Guid id)
    {
        var field = typeof(T).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, id);
    }

    private static void SetBackingField<T, TValue>(T instance, string fieldName, TValue value)
    {
        var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }

    private sealed record ErpSnapshot(
        IReadOnlyCollection<EmpresaSnapshot> Empresas,
        IReadOnlyCollection<ProdutoSnapshot> Produtos,
        IReadOnlyCollection<ClienteSnapshot> Clientes,
        IReadOnlyCollection<DepositoSnapshot> Depositos,
        IReadOnlyCollection<UsuarioSnapshot> Usuarios,
        IReadOnlyCollection<PedidoSnapshot> Pedidos,
        IReadOnlyCollection<NotaFiscalSnapshot> NotasFiscais,
        IReadOnlyCollection<SaldoSnapshot> Saldos,
        IReadOnlyCollection<MovimentoEstoqueSnapshot> MovimentosEstoque,
        IReadOnlyCollection<ChaveImportadaSnapshot> ChavesImportadas,
        IReadOnlyCollection<string> EventosWebhook,
        IReadOnlyCollection<ImportacaoNotaEntradaSnapshot> ImportacoesNotaEntrada,
        IReadOnlyCollection<WebhookProcessadoSnapshot> WebhooksProcessados,
        IReadOnlyCollection<IntegrationEventSnapshot> IntegrationEvents);

    private sealed record EmpresaSnapshot(Guid Id, string Documento, string NomeFantasia, string RazaoSocial, string Status);
    private sealed record ProdutoSnapshot(Guid Id, Guid EmpresaId, string CodigoInterno, string Sku, string Descricao, string Tipo, decimal PrecoVenda, decimal Custo, string Ncm, string Origem, bool Ativo, IReadOnlyCollection<ProdutoVariacaoSnapshot> Variacoes, IReadOnlyCollection<AuditChangeSnapshot> AuditoriasFiscais);
    private sealed record ProdutoVariacaoSnapshot(string Sku, string? CodigoBarras, decimal? PrecoVenda);
    private sealed record AuditChangeSnapshot(string Field, string PreviousValue, string CurrentValue);
    private sealed record ClienteSnapshot(Guid Id, Guid EmpresaId, string Documento, string Nome, string? Email, string Status, DateTimeOffset? UltimoBloqueioEm);
    private sealed record DepositoSnapshot(Guid Id, Guid EmpresaId, string Codigo, string Nome, string Status);
    private sealed record UsuarioSnapshot(Guid Id, Guid EmpresaId, string Email, string Nome, string Status, DateTimeOffset? UltimoBloqueioEm, IReadOnlyCollection<string> Permissoes);
    private sealed record PedidoSnapshot(Guid Id, Guid ClienteId, string Status, IReadOnlyCollection<ItemPedidoSnapshot> Itens);
    private sealed record ItemPedidoSnapshot(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario);
    private sealed record NotaFiscalSnapshot(Guid Id, Guid PedidoVendaId, Guid ClienteId, string Status, string? CodigoRejeicao, string? MensagemRejeicao, bool EstoqueBaixado, string? JustificativaCancelamento, IReadOnlyCollection<ItemNotaFiscalSnapshot> Itens, IReadOnlyCollection<string> HistoricoTentativas);
    private sealed record ItemNotaFiscalSnapshot(Guid ProdutoId, decimal Quantidade, string Ncm, string Cfop);
    private sealed record SaldoSnapshot(Guid ProdutoId, Guid DepositoId, decimal SaldoAtual, decimal Reservado, bool PermiteSaldoNegativo);
    private sealed record MovimentoEstoqueSnapshot(Guid ProdutoId, Guid DepositoId, string Tipo, decimal Quantidade, string Motivo, string DocumentoOrigem, decimal SaldoAnterior, decimal SaldoPosterior, DateTimeOffset DataHora);
    private sealed record ChaveImportadaSnapshot(Guid EmpresaId, string ChaveAcesso);
    private sealed record ImportacaoNotaEntradaSnapshot(Guid EmpresaId, Guid DepositoId, string ChaveAcesso, bool ImportadaComSucesso, int ItensExternos, int ItensPendentesConciliacao, int MovimentosGerados, DateTimeOffset ProcessadaEm);
    private sealed record WebhookProcessadoSnapshot(string EventoId, string Origem, string Status, string Mensagem, DateTimeOffset ProcessadoEm);
    private sealed record IntegrationEventSnapshot(Guid Id, string Type, string SourceModule, string AggregateId, string Description, DateTimeOffset OccurredAt);
}
