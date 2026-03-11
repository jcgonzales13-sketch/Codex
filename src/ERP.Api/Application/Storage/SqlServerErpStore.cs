using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Depositos;
using ERP.Modules.Empresas;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Identity;
using ERP.Modules.Vendas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ERP.Api.Application.Integration;

namespace ERP.Api.Application.Storage;

public sealed class SqlServerErpStore : IErpStore
{
    private readonly string _connectionString;
    private readonly string _schema;
    private readonly string _table;

    public SqlServerErpStore(IOptions<StorageOptions> options)
    {
        _connectionString = options.Value.ConnectionString ?? throw new InvalidOperationException("Storage:ConnectionString e obrigatoria para o provider SqlServer.");
        _schema = string.IsNullOrWhiteSpace(options.Value.Schema) ? "dbo" : options.Value.Schema.Trim();
        _table = string.IsNullOrWhiteSpace(options.Value.StateTable) ? "ErpState" : options.Value.StateTable.Trim();

        EnsureTable();
        Load();
    }

    public object SyncRoot { get; } = new();
    public Dictionary<Guid, Empresa> Empresas { get; } = [];
    public Dictionary<Guid, Produto> Produtos { get; } = [];
    public Dictionary<Guid, Cliente> Clientes { get; } = [];
    public Dictionary<Guid, Deposito> Depositos { get; } = [];
    public Dictionary<Guid, Usuario> Usuarios { get; } = [];
    public Dictionary<Guid, PedidoVenda> Pedidos { get; } = [];
    public Dictionary<Guid, NotaFiscal> NotasFiscais { get; } = [];
    public Dictionary<(Guid ProdutoId, Guid DepositoId), SaldoEstoque> Saldos { get; } = [];
    public List<MovimentoEstoque> MovimentosEstoque { get; } = [];
    public HashSet<(Guid EmpresaId, string ChaveAcesso)> ChavesImportadas { get; } = [];
    public HashSet<string> EventosWebhook { get; } = [];
    public List<ImportacaoNotaEntradaRegistro> ImportacoesNotaEntrada { get; } = [];
    public List<WebhookProcessadoRegistro> WebhooksProcessados { get; } = [];
    public List<IntegrationEvent> IntegrationEvents { get; } = [];

    public void Persist()
    {
        lock (SyncRoot)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var sql = $"""
                MERGE [{_schema}].[{_table}] AS target
                USING (SELECT @StateKey AS StateKey, @Payload AS Payload, SYSUTCDATETIME() AS UpdatedAt) AS source
                ON target.StateKey = source.StateKey
                WHEN MATCHED THEN
                    UPDATE SET Payload = source.Payload, UpdatedAt = source.UpdatedAt
                WHEN NOT MATCHED THEN
                    INSERT (StateKey, Payload, UpdatedAt) VALUES (source.StateKey, source.Payload, source.UpdatedAt);
                """;

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@StateKey", "default");
            command.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.Serialize(this));
            command.ExecuteNonQuery();
        }
    }

    private void Load()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var sql = $"SELECT Payload FROM [{_schema}].[{_table}] WHERE StateKey = @StateKey";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@StateKey", "default");

        var payload = command.ExecuteScalar() as string;
        if (!string.IsNullOrWhiteSpace(payload))
        {
            ErpSnapshotSerializer.Load(this, payload);
        }
    }

    private void EnsureTable()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var sql = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_schema}')
                EXEC('CREATE SCHEMA [{_schema}]');

            IF OBJECT_ID('{_schema}.{_table}', 'U') IS NULL
            BEGIN
                CREATE TABLE [{_schema}].[{_table}]
                (
                    [StateKey] NVARCHAR(100) NOT NULL PRIMARY KEY,
                    [Payload] NVARCHAR(MAX) NOT NULL,
                    [UpdatedAt] DATETIME2 NOT NULL
                );
            END
            """;

        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }
}
