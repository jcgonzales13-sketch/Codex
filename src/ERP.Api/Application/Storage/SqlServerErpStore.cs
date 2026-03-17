using ERP.Modules.Catalogo;
using ERP.Modules.Clientes;
using ERP.Modules.Depositos;
using ERP.Modules.Empresas;
using ERP.Modules.Estoque;
using ERP.Modules.Fiscal;
using ERP.Modules.Fornecedores;
using ERP.Modules.Identity;
using ERP.Modules.Vendas;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ERP.Api.Application.Integration;

namespace ERP.Api.Application.Storage;

public sealed class SqlServerErpStore : IErpStore
{
    private const string StockMovementsTable = "MovimentosEstoque";
    private const string IntegrationEventsTable = "IntegrationEvents";
    private const string PurchaseImportsTable = "ImportacoesNotaEntrada";
    private const string ProcessedWebhooksTable = "WebhooksProcessados";
    private const string AuthSessionsTable = "SessoesAutenticacao";
    private const string RefreshTokensTable = "RefreshTokens";
    private const string SalesOrdersTable = "PedidosVenda";
    private const string InvoicesTable = "NotasFiscais";
    private const string StockBalancesTable = "SaldosEstoque";
    private const string CompaniesTable = "Empresas";
    private const string SuppliersTable = "Fornecedores";
    private const string ProductsTable = "Produtos";
    private const string CustomersTable = "Clientes";
    private const string WarehousesTable = "Depositos";
    private const string UsersTable = "Usuarios";
    private const string AccessProfilesTable = "PerfisAcesso";
    private const string ImportedKeysTable = "ChavesImportadas";
    private const string WebhookEventsTable = "EventosWebhook";
    private static readonly string[] DedicatedTables =
    [
        StockMovementsTable,
        IntegrationEventsTable,
        PurchaseImportsTable,
        ProcessedWebhooksTable,
        AuthSessionsTable,
        RefreshTokensTable,
        SalesOrdersTable,
        InvoicesTable,
        StockBalancesTable,
        CompaniesTable,
        SuppliersTable,
        ProductsTable,
        CustomersTable,
        WarehousesTable,
        UsersTable,
        AccessProfilesTable,
        ImportedKeysTable,
        WebhookEventsTable
    ];
    private readonly string _connectionString;
    private readonly string _schema;
    private readonly string _table;
    private readonly string _migrationsTable;
    private readonly bool _persistLegacyStateSnapshot;

    public SqlServerErpStore(IOptions<StorageOptions> options)
    {
        _connectionString = options.Value.ConnectionString ?? throw new InvalidOperationException("Storage:ConnectionString e obrigatoria para o provider SqlServer.");
        _schema = string.IsNullOrWhiteSpace(options.Value.Schema) ? "dbo" : options.Value.Schema.Trim();
        _table = string.IsNullOrWhiteSpace(options.Value.StateTable) ? "ErpState" : options.Value.StateTable.Trim();
        _migrationsTable = string.IsNullOrWhiteSpace(options.Value.MigrationsTable) ? "ErpMigrations" : options.Value.MigrationsTable.Trim();
        _persistLegacyStateSnapshot = options.Value.PersistLegacyStateSnapshot;

        EnsureSchemaMigrated();
        Load();
    }

    public object SyncRoot { get; } = new();
    public Dictionary<Guid, Empresa> Empresas { get; } = [];
    public Dictionary<Guid, Fornecedor> Fornecedores { get; } = [];
    public Dictionary<Guid, Produto> Produtos { get; } = [];
    public Dictionary<Guid, Cliente> Clientes { get; } = [];
    public Dictionary<Guid, Deposito> Depositos { get; } = [];
    public Dictionary<Guid, Usuario> Usuarios { get; } = [];
    public Dictionary<Guid, PerfilAcesso> PerfisAcesso { get; } = [];
    public Dictionary<Guid, PedidoVenda> Pedidos { get; } = [];
    public Dictionary<Guid, NotaFiscal> NotasFiscais { get; } = [];
    public Dictionary<(Guid ProdutoId, Guid DepositoId), SaldoEstoque> Saldos { get; } = [];
    public List<MovimentoEstoque> MovimentosEstoque { get; } = [];
    public HashSet<(Guid EmpresaId, string ChaveAcesso)> ChavesImportadas { get; } = [];
    public HashSet<string> EventosWebhook { get; } = [];
    public List<ImportacaoNotaEntradaRegistro> ImportacoesNotaEntrada { get; } = [];
    public List<WebhookProcessadoRegistro> WebhooksProcessados { get; } = [];
    public List<SessaoAutenticacaoRegistro> SessoesAutenticacao { get; } = [];
    public List<RefreshTokenRegistro> RefreshTokens { get; } = [];
    public List<IntegrationEvent> IntegrationEvents { get; } = [];

    public void Persist()
    {
        lock (SyncRoot)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            if (_persistLegacyStateSnapshot)
            {
                foreach (var (sectionName, payload) in ErpSnapshotSerializer.SerializeSections(this))
                {
                    var sql = $"""
                        MERGE [{_schema}].[{_table}] AS target
                        USING (SELECT @StateKey AS StateKey, @Payload AS Payload, SYSUTCDATETIME() AS UpdatedAt) AS source
                        ON target.StateKey = source.StateKey
                        WHEN MATCHED THEN
                            UPDATE SET Payload = source.Payload, UpdatedAt = source.UpdatedAt
                        WHEN NOT MATCHED THEN
                            INSERT (StateKey, Payload, UpdatedAt) VALUES (source.StateKey, source.Payload, source.UpdatedAt);
                        """;

                    using var command = new SqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@StateKey", sectionName);
                    command.Parameters.AddWithValue("@Payload", payload);
                    command.ExecuteNonQuery();
                }
            }
            else if (TableExists(connection, _table))
            {
                using var cleanupCommand = new SqlCommand($"DELETE FROM [{_schema}].[{_table}];", connection, transaction);
                cleanupCommand.ExecuteNonQuery();
            }

            PersistStockMovements(connection, transaction);
            PersistIntegrationEvents(connection, transaction);
            PersistPurchaseImports(connection, transaction);
            PersistProcessedWebhooks(connection, transaction);
            PersistAuthSessions(connection, transaction);
            PersistRefreshTokens(connection, transaction);
            PersistSalesOrders(connection, transaction);
            PersistInvoices(connection, transaction);
            PersistStockBalances(connection, transaction);
            PersistCompanies(connection, transaction);
            PersistSuppliers(connection, transaction);
            PersistProducts(connection, transaction);
            PersistCustomers(connection, transaction);
            PersistWarehouses(connection, transaction);
            PersistUsers(connection, transaction);
            PersistAccessProfiles(connection, transaction);
            PersistImportedKeys(connection, transaction);
            PersistWebhookEvents(connection, transaction);
            transaction.Commit();
        }
    }

    public (string? LastAppliedMigrationId, int PendingMigrations) GetMigrationStatus()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        if (!MigrationHistoryTableExists(connection))
        {
            return (null, SqlServerMigrationScripts.All.Count);
        }

        using var command = new SqlCommand(
            $"""
            SELECT [MigrationId]
            FROM [{_schema}].[{_migrationsTable}]
            ORDER BY [MigrationId];
            """,
            connection);

        var appliedMigrations = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            appliedMigrations.Add(reader.GetString(0));
        }

        var pendingMigrations = SqlServerMigrationScripts.All.Count(script => !appliedMigrations.Contains(script.Id, StringComparer.Ordinal));
        return (appliedMigrations.LastOrDefault(), pendingMigrations);
    }

    public (int LegacyStateRows, int DedicatedTablesWithData) GetStorageDiagnostics()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var legacyStateRows = 0;
        if (TableExists(connection, _table))
        {
            using var stateCommand = new SqlCommand($"SELECT COUNT(1) FROM [{_schema}].[{_table}];", connection);
            legacyStateRows = Convert.ToInt32(stateCommand.ExecuteScalar());
        }

        var dedicatedTablesWithData = GetDedicatedTablesWithData(connection);

        return (legacyStateRows, dedicatedTablesWithData);
    }

    public bool PersistLegacyStateSnapshot => _persistLegacyStateSnapshot;

    private void Load()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var sql = $"SELECT StateKey, Payload FROM [{_schema}].[{_table}]";
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var command = new SqlCommand(sql, connection))
        {
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sections[reader.GetString(0)] = reader.GetString(1);
            }
        }

        if (!_persistLegacyStateSnapshot && GetDedicatedTablesWithData(connection) > 0)
        {
            LoadDedicatedOperationalData(connection);
            return;
        }

        if (sections.Count == 0)
        {
            LoadDedicatedOperationalData(connection);
            return;
        }

        if (sections.Count == 1 && sections.TryGetValue("default", out var legacyPayload))
        {
            ErpSnapshotSerializer.Load(this, legacyPayload);
            LoadDedicatedOperationalData(connection);
            return;
        }

        ErpSnapshotSerializer.LoadSections(this, sections);
        LoadDedicatedOperationalData(connection);
    }

    private void EnsureSchemaMigrated()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        foreach (var migration in SqlServerMigrationScripts.All)
        {
            if (MigrationAlreadyApplied(connection, migration.Id))
            {
                continue;
            }

            var sql = SqlServerMigrationScripts.Render(migration, _schema, _table, _migrationsTable);
            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(sql, connection, transaction);
            command.ExecuteNonQuery();

            EnsureMigrationHistoryExists(connection, transaction);
            using var insertCommand = new SqlCommand(
                $"""
                INSERT INTO [{_schema}].[{_migrationsTable}] ([MigrationId], [Description], [AppliedAt])
                VALUES (@MigrationId, @Description, SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@MigrationId", migration.Id);
            insertCommand.Parameters.AddWithValue("@Description", migration.Description);
            insertCommand.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    private bool MigrationAlreadyApplied(SqlConnection connection, string migrationId)
    {
        if (!MigrationHistoryTableExists(connection))
        {
            return false;
        }

        using var command = new SqlCommand(
            $"SELECT COUNT(1) FROM [{_schema}].[{_migrationsTable}] WHERE [MigrationId] = @MigrationId",
            connection);
        command.Parameters.AddWithValue("@MigrationId", migrationId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private bool MigrationHistoryTableExists(SqlConnection connection)
    {
        using var command = new SqlCommand(
            """
            SELECT COUNT(1)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = @SchemaName AND t.name = @TableName
            """,
            connection);
        command.Parameters.AddWithValue("@SchemaName", _schema);
        command.Parameters.AddWithValue("@TableName", _migrationsTable);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private void EnsureMigrationHistoryExists(SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(
            $"""
            IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_schema}')
                EXEC(N'CREATE SCHEMA [{_schema}]');

            IF OBJECT_ID(N'[{_schema}].[{_migrationsTable}]', N'U') IS NULL
            BEGIN
                CREATE TABLE [{_schema}].[{_migrationsTable}]
                (
                    [MigrationId] NVARCHAR(50) NOT NULL PRIMARY KEY,
                    [Description] NVARCHAR(200) NOT NULL,
                    [AppliedAt] DATETIME2 NOT NULL CONSTRAINT [DF_{_migrationsTable}_AppliedAt] DEFAULT SYSUTCDATETIME()
                );
            END;
            """,
            connection,
            transaction);
        command.ExecuteNonQuery();
    }

    private void LoadDedicatedOperationalData(SqlConnection connection)
    {
        if (TableExists(connection, StockMovementsTable))
        {
            MovimentosEstoque.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [ProdutoId], [DepositoId], [Tipo], [Quantidade], [Motivo], [DocumentoOrigem], [SaldoAnterior], [SaldoPosterior], [DataHora]
                FROM [{_schema}].[{StockMovementsTable}]
                ORDER BY [DataHora], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                MovimentosEstoque.Add(new MovimentoEstoque(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    Enum.Parse<TipoMovimentoEstoque>(reader.GetString(2)),
                    reader.GetDecimal(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetDecimal(6),
                    reader.GetDecimal(7),
                    reader.GetFieldValue<DateTimeOffset>(8)));
            }
        }

        if (TableExists(connection, CompaniesTable))
        {
            Empresas.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{CompaniesTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var empresa = ErpSnapshotSerializer.DeserializeEmpresa(reader.GetString(0));
                if (empresa is not null)
                {
                    Empresas[empresa.Id] = empresa;
                }
            }
        }

        if (TableExists(connection, SuppliersTable))
        {
            Fornecedores.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{SuppliersTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fornecedor = ErpSnapshotSerializer.DeserializeFornecedor(reader.GetString(0));
                if (fornecedor is not null)
                {
                    Fornecedores[fornecedor.Id] = fornecedor;
                }
            }
        }

        if (TableExists(connection, ProductsTable))
        {
            Produtos.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{ProductsTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var produto = ErpSnapshotSerializer.DeserializeProduto(reader.GetString(0));
                if (produto is not null)
                {
                    Produtos[produto.Id] = produto;
                }
            }
        }

        if (TableExists(connection, CustomersTable))
        {
            Clientes.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{CustomersTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var cliente = ErpSnapshotSerializer.DeserializeCliente(reader.GetString(0));
                if (cliente is not null)
                {
                    Clientes[cliente.Id] = cliente;
                }
            }
        }

        if (TableExists(connection, WarehousesTable))
        {
            Depositos.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{WarehousesTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var deposito = ErpSnapshotSerializer.DeserializeDeposito(reader.GetString(0));
                if (deposito is not null)
                {
                    Depositos[deposito.Id] = deposito;
                }
            }
        }

        if (TableExists(connection, UsersTable))
        {
            Usuarios.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{UsersTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var usuario = ErpSnapshotSerializer.DeserializeUsuario(reader.GetString(0));
                if (usuario is not null)
                {
                    Usuarios[usuario.Id] = usuario;
                }
            }
        }

        if (TableExists(connection, AccessProfilesTable))
        {
            PerfisAcesso.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{AccessProfilesTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var perfilAcesso = ErpSnapshotSerializer.DeserializePerfilAcesso(reader.GetString(0));
                if (perfilAcesso is not null)
                {
                    PerfisAcesso[perfilAcesso.Id] = perfilAcesso;
                }
            }
        }

        if (TableExists(connection, ImportedKeysTable))
        {
            ChavesImportadas.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [EmpresaId], [ChaveAcesso]
                FROM [{_schema}].[{ImportedKeysTable}]
                ORDER BY [UpdatedAt], [EmpresaId], [ChaveAcesso];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ChavesImportadas.Add((reader.GetGuid(0), reader.GetString(1)));
            }
        }

        if (TableExists(connection, WebhookEventsTable))
        {
            EventosWebhook.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [EventoId]
                FROM [{_schema}].[{WebhookEventsTable}]
                ORDER BY [UpdatedAt], [EventoId];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                EventosWebhook.Add(reader.GetString(0));
            }
        }

        if (TableExists(connection, IntegrationEventsTable))
        {
            IntegrationEvents.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Id], [Type], [SourceModule], [AggregateId], [Description], [OccurredAt]
                FROM [{_schema}].[{IntegrationEventsTable}]
                ORDER BY [OccurredAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                IntegrationEvents.Add(new IntegrationEvent(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetFieldValue<DateTimeOffset>(5)));
            }
        }

        if (TableExists(connection, PurchaseImportsTable))
        {
            ImportacoesNotaEntrada.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [EmpresaId], [FornecedorId], [DepositoId], [ChaveAcesso], [ImportadaComSucesso], [ItensExternos], [ItensPendentesConciliacao], [MovimentosGerados], [ProcessadaEm]
                FROM [{_schema}].[{PurchaseImportsTable}]
                ORDER BY [ProcessadaEm], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ImportacoesNotaEntrada.Add(new ImportacaoNotaEntradaRegistro(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetBoolean(4),
                    reader.GetInt32(5),
                    reader.GetInt32(6),
                    reader.GetInt32(7),
                    reader.GetFieldValue<DateTimeOffset>(8)));
            }
        }

        if (TableExists(connection, ProcessedWebhooksTable))
        {
            WebhooksProcessados.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [EventoId], [Origem], [Status], [Mensagem], [ProcessadoEm]
                FROM [{_schema}].[{ProcessedWebhooksTable}]
                ORDER BY [ProcessadoEm], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                WebhooksProcessados.Add(new WebhookProcessadoRegistro(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }
        }

        if (TableExists(connection, AuthSessionsTable))
        {
            SessoesAutenticacao.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Token], [UsuarioId], [EmpresaId], [Email], [CriadaEm], [ExpiraEm]
                FROM [{_schema}].[{AuthSessionsTable}]
                ORDER BY [CriadaEm], [Token];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                SessoesAutenticacao.Add(new SessaoAutenticacaoRegistro(
                    reader.GetString(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4),
                    reader.GetFieldValue<DateTimeOffset>(5)));
            }
        }

        if (TableExists(connection, RefreshTokensTable))
        {
            RefreshTokens.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Token], [SessionToken], [UsuarioId], [CriadoEm], [ExpiraEm]
                FROM [{_schema}].[{RefreshTokensTable}]
                ORDER BY [CriadoEm], [Token];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                RefreshTokens.Add(new RefreshTokenRegistro(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetGuid(2),
                    reader.GetFieldValue<DateTimeOffset>(3),
                    reader.GetFieldValue<DateTimeOffset>(4)));
            }
        }

        if (TableExists(connection, SalesOrdersTable))
        {
            Pedidos.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{SalesOrdersTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var pedido = ErpSnapshotSerializer.DeserializePedidoVenda(reader.GetString(0));
                if (pedido is not null)
                {
                    Pedidos[pedido.Id] = pedido;
                }
            }
        }

        if (TableExists(connection, InvoicesTable))
        {
            NotasFiscais.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{InvoicesTable}]
                ORDER BY [UpdatedAt], [Id];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var nota = ErpSnapshotSerializer.DeserializeNotaFiscal(reader.GetString(0));
                if (nota is not null)
                {
                    NotasFiscais[nota.Id] = nota;
                }
            }
        }

        if (TableExists(connection, StockBalancesTable))
        {
            Saldos.Clear();
            using var command = new SqlCommand(
                $"""
                SELECT [Payload]
                FROM [{_schema}].[{StockBalancesTable}]
                ORDER BY [UpdatedAt], [DepositoId], [ProdutoId];
                """,
                connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var saldo = ErpSnapshotSerializer.DeserializeSaldoEstoque(reader.GetString(0));
                if (saldo is not null)
                {
                    Saldos[(saldo.ProdutoId, saldo.DepositoId)] = saldo;
                }
            }
        }
    }

    private void PersistStockMovements(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, StockMovementsTable))
        {
            return;
        }

        using (var deleteCommand = new SqlCommand($"DELETE FROM [{_schema}].[{StockMovementsTable}];", connection, transaction))
        {
            deleteCommand.ExecuteNonQuery();
        }

        foreach (var movimento in MovimentosEstoque)
        {
            using var insertCommand = new SqlCommand(
                $"""
                INSERT INTO [{_schema}].[{StockMovementsTable}]
                (
                    [ProdutoId], [DepositoId], [Tipo], [Quantidade], [Motivo], [DocumentoOrigem], [SaldoAnterior], [SaldoPosterior], [DataHora]
                )
                VALUES
                (
                    @ProdutoId, @DepositoId, @Tipo, @Quantidade, @Motivo, @DocumentoOrigem, @SaldoAnterior, @SaldoPosterior, @DataHora
                );
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@ProdutoId", movimento.ProdutoId);
            insertCommand.Parameters.AddWithValue("@DepositoId", movimento.DepositoId);
            insertCommand.Parameters.AddWithValue("@Tipo", movimento.Tipo.ToString());
            insertCommand.Parameters.AddWithValue("@Quantidade", movimento.Quantidade);
            insertCommand.Parameters.AddWithValue("@Motivo", movimento.Motivo);
            insertCommand.Parameters.AddWithValue("@DocumentoOrigem", movimento.DocumentoOrigem);
            insertCommand.Parameters.AddWithValue("@SaldoAnterior", movimento.SaldoAnterior);
            insertCommand.Parameters.AddWithValue("@SaldoPosterior", movimento.SaldoPosterior);
            insertCommand.Parameters.AddWithValue("@DataHora", movimento.DataHora);
            insertCommand.ExecuteNonQuery();
        }
    }

    private void PersistCompanies(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, CompaniesTable))
        {
            return;
        }
        foreach (var empresa in Empresas.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{CompaniesTable}] AS target
                USING (SELECT @Id AS [Id], @Documento AS [Documento], @Status AS [Status], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [Documento] = source.[Documento], [Status] = source.[Status], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [Documento], [Status], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[Documento], source.[Status], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", empresa.Id);
            insertCommand.Parameters.AddWithValue("@Documento", empresa.Documento);
            insertCommand.Parameters.AddWithValue("@Status", empresa.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeEmpresa(empresa));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, CompaniesTable, Empresas.Keys);
    }

    private void PersistSuppliers(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, SuppliersTable))
        {
            return;
        }
        foreach (var fornecedor in Fornecedores.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{SuppliersTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Documento AS [Documento], @Status AS [Status], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Documento] = source.[Documento], [Status] = source.[Status], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Documento], [Status], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Documento], source.[Status], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", fornecedor.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", fornecedor.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Documento", fornecedor.Documento);
            insertCommand.Parameters.AddWithValue("@Status", fornecedor.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeFornecedor(fornecedor));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, SuppliersTable, Fornecedores.Keys);
    }

    private void PersistProducts(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, ProductsTable))
        {
            return;
        }
        foreach (var produto in Produtos.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{ProductsTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Sku AS [Sku], @Ativo AS [Ativo], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Sku] = source.[Sku], [Ativo] = source.[Ativo], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Sku], [Ativo], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Sku], source.[Ativo], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", produto.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", produto.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Sku", produto.Sku);
            insertCommand.Parameters.AddWithValue("@Ativo", produto.Ativo);
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeProduto(produto));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, ProductsTable, Produtos.Keys);
    }

    private void PersistCustomers(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, CustomersTable))
        {
            return;
        }
        foreach (var cliente in Clientes.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{CustomersTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Documento AS [Documento], @Status AS [Status], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Documento] = source.[Documento], [Status] = source.[Status], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Documento], [Status], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Documento], source.[Status], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", cliente.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", cliente.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Documento", cliente.Documento);
            insertCommand.Parameters.AddWithValue("@Status", cliente.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeCliente(cliente));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, CustomersTable, Clientes.Keys);
    }

    private void PersistWarehouses(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, WarehousesTable))
        {
            return;
        }
        foreach (var deposito in Depositos.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{WarehousesTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Codigo AS [Codigo], @Status AS [Status], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Codigo] = source.[Codigo], [Status] = source.[Status], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Codigo], [Status], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Codigo], source.[Status], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", deposito.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", deposito.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Codigo", deposito.Codigo);
            insertCommand.Parameters.AddWithValue("@Status", deposito.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeDeposito(deposito));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, WarehousesTable, Depositos.Keys);
    }

    private void PersistUsers(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, UsersTable))
        {
            return;
        }
        foreach (var usuario in Usuarios.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{UsersTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Email AS [Email], @Status AS [Status], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Email] = source.[Email], [Status] = source.[Status], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Email], [Status], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Email], source.[Status], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", usuario.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", usuario.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Email", usuario.Email);
            insertCommand.Parameters.AddWithValue("@Status", usuario.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeUsuario(usuario));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, UsersTable, Usuarios.Keys);
    }

    private void PersistAccessProfiles(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, AccessProfilesTable))
        {
            return;
        }
        foreach (var perfilAcesso in PerfisAcesso.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{AccessProfilesTable}] AS target
                USING (SELECT @Id AS [Id], @EmpresaId AS [EmpresaId], @Nome AS [Nome], @Payload AS [Payload]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [EmpresaId] = source.[EmpresaId], [Nome] = source.[Nome], [Payload] = source.[Payload], [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [EmpresaId], [Nome], [Payload], [UpdatedAt])
                    VALUES (source.[Id], source.[EmpresaId], source.[Nome], source.[Payload], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", perfilAcesso.Id);
            insertCommand.Parameters.AddWithValue("@EmpresaId", perfilAcesso.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Nome", perfilAcesso.Nome);
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializePerfilAcesso(perfilAcesso));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, AccessProfilesTable, PerfisAcesso.Keys);
    }

    private void PersistImportedKeys(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, ImportedKeysTable))
        {
            return;
        }
        foreach (var chave in ChavesImportadas)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{ImportedKeysTable}] AS target
                USING (SELECT @EmpresaId AS [EmpresaId], @ChaveAcesso AS [ChaveAcesso]) AS source
                ON target.[EmpresaId] = source.[EmpresaId] AND target.[ChaveAcesso] = source.[ChaveAcesso]
                WHEN MATCHED THEN
                    UPDATE SET [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([EmpresaId], [ChaveAcesso], [UpdatedAt])
                    VALUES (source.[EmpresaId], source.[ChaveAcesso], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@EmpresaId", chave.EmpresaId);
            insertCommand.Parameters.AddWithValue("@ChaveAcesso", chave.ChaveAcesso);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingImportedKeys(connection, transaction);
    }

    private void PersistWebhookEvents(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, WebhookEventsTable))
        {
            return;
        }
        foreach (var eventoId in EventosWebhook)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{WebhookEventsTable}] AS target
                USING (SELECT @EventoId AS [EventoId]) AS source
                ON target.[EventoId] = source.[EventoId]
                WHEN MATCHED THEN
                    UPDATE SET [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([EventoId], [UpdatedAt])
                    VALUES (source.[EventoId], SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@EventoId", eventoId);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingStringRows(connection, transaction, WebhookEventsTable, "EventoId", EventosWebhook);
    }

    private void DeleteMissingGuidRows(SqlConnection connection, SqlTransaction transaction, string tableName, IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0)
        {
            using var deleteAll = new SqlCommand($"DELETE FROM [{_schema}].[{tableName}];", connection, transaction);
            deleteAll.ExecuteNonQuery();
            return;
        }

        var parameterNames = new List<string>(idList.Length);
        using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        for (var index = 0; index < idList.Length; index++)
        {
            var parameterName = $"@Id{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, idList[index]);
        }

        command.CommandText = $"DELETE FROM [{_schema}].[{tableName}] WHERE [Id] NOT IN ({string.Join(", ", parameterNames)});";
        command.ExecuteNonQuery();
    }

    private void DeleteMissingStringRows(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, IEnumerable<string> values)
    {
        var valueList = values.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).ToArray();
        if (valueList.Length == 0)
        {
            using var deleteAll = new SqlCommand($"DELETE FROM [{_schema}].[{tableName}];", connection, transaction);
            deleteAll.ExecuteNonQuery();
            return;
        }

        var parameterNames = new List<string>(valueList.Length);
        using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        for (var index = 0; index < valueList.Length; index++)
        {
            var parameterName = $"@Value{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, valueList[index]);
        }

        command.CommandText = $"DELETE FROM [{_schema}].[{tableName}] WHERE [{columnName}] NOT IN ({string.Join(", ", parameterNames)});";
        command.ExecuteNonQuery();
    }

    private void DeleteMissingImportedKeys(SqlConnection connection, SqlTransaction transaction)
    {
        if (ChavesImportadas.Count == 0)
        {
            using var deleteAll = new SqlCommand($"DELETE FROM [{_schema}].[{ImportedKeysTable}];", connection, transaction);
            deleteAll.ExecuteNonQuery();
            return;
        }

        var clauses = new List<string>(ChavesImportadas.Count);
        using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        var index = 0;
        foreach (var chave in ChavesImportadas)
        {
            var empresaId = $"@EmpresaId{index}";
            var chaveAcesso = $"@ChaveAcesso{index}";
            clauses.Add($"([EmpresaId] = {empresaId} AND [ChaveAcesso] = {chaveAcesso})");
            command.Parameters.AddWithValue(empresaId, chave.EmpresaId);
            command.Parameters.AddWithValue(chaveAcesso, chave.ChaveAcesso);
            index++;
        }

        command.CommandText = $"DELETE FROM [{_schema}].[{ImportedKeysTable}] WHERE NOT ({string.Join(" OR ", clauses)});";
        command.ExecuteNonQuery();
    }

    private void PersistPurchaseImports(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, PurchaseImportsTable))
        {
            return;
        }

        foreach (var importacao in ImportacoesNotaEntrada)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{PurchaseImportsTable}] AS target
                USING (SELECT @EmpresaId AS [EmpresaId], @FornecedorId AS [FornecedorId], @DepositoId AS [DepositoId], @ChaveAcesso AS [ChaveAcesso]) AS source
                ON target.[EmpresaId] = source.[EmpresaId] AND target.[ChaveAcesso] = source.[ChaveAcesso]
                WHEN MATCHED THEN
                    UPDATE SET [FornecedorId] = @FornecedorId, [DepositoId] = @DepositoId, [ImportadaComSucesso] = @ImportadaComSucesso, [ItensExternos] = @ItensExternos, [ItensPendentesConciliacao] = @ItensPendentesConciliacao, [MovimentosGerados] = @MovimentosGerados, [ProcessadaEm] = @ProcessadaEm
                WHEN NOT MATCHED THEN
                    INSERT ([EmpresaId], [FornecedorId], [DepositoId], [ChaveAcesso], [ImportadaComSucesso], [ItensExternos], [ItensPendentesConciliacao], [MovimentosGerados], [ProcessadaEm])
                    VALUES (@EmpresaId, @FornecedorId, @DepositoId, @ChaveAcesso, @ImportadaComSucesso, @ItensExternos, @ItensPendentesConciliacao, @MovimentosGerados, @ProcessadaEm);
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@EmpresaId", importacao.EmpresaId);
            insertCommand.Parameters.AddWithValue("@FornecedorId", importacao.FornecedorId);
            insertCommand.Parameters.AddWithValue("@DepositoId", importacao.DepositoId);
            insertCommand.Parameters.AddWithValue("@ChaveAcesso", importacao.ChaveAcesso);
            insertCommand.Parameters.AddWithValue("@ImportadaComSucesso", importacao.ImportadaComSucesso);
            insertCommand.Parameters.AddWithValue("@ItensExternos", importacao.ItensExternos);
            insertCommand.Parameters.AddWithValue("@ItensPendentesConciliacao", importacao.ItensPendentesConciliacao);
            insertCommand.Parameters.AddWithValue("@MovimentosGerados", importacao.MovimentosGerados);
            insertCommand.Parameters.AddWithValue("@ProcessadaEm", importacao.ProcessadaEm);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingPurchaseImports(connection, transaction);
    }

    private void PersistIntegrationEvents(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, IntegrationEventsTable))
        {
            return;
        }

        foreach (var integrationEvent in IntegrationEvents)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{IntegrationEventsTable}] AS target
                USING (SELECT @Id AS [Id]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [Type] = @Type, [SourceModule] = @SourceModule, [AggregateId] = @AggregateId, [Description] = @Description, [OccurredAt] = @OccurredAt
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [Type], [SourceModule], [AggregateId], [Description], [OccurredAt])
                    VALUES (@Id, @Type, @SourceModule, @AggregateId, @Description, @OccurredAt);
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", integrationEvent.Id);
            insertCommand.Parameters.AddWithValue("@Type", integrationEvent.Type);
            insertCommand.Parameters.AddWithValue("@SourceModule", integrationEvent.SourceModule);
            insertCommand.Parameters.AddWithValue("@AggregateId", integrationEvent.AggregateId);
            insertCommand.Parameters.AddWithValue("@Description", integrationEvent.Description);
            insertCommand.Parameters.AddWithValue("@OccurredAt", integrationEvent.OccurredAt);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, IntegrationEventsTable, IntegrationEvents.Select(item => item.Id));
    }

    private void PersistProcessedWebhooks(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, ProcessedWebhooksTable))
        {
            return;
        }

        foreach (var webhook in WebhooksProcessados)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{ProcessedWebhooksTable}] AS target
                USING (SELECT @EventoId AS [EventoId]) AS source
                ON target.[EventoId] = source.[EventoId]
                WHEN MATCHED THEN
                    UPDATE SET [Origem] = @Origem, [Status] = @Status, [Mensagem] = @Mensagem, [ProcessadoEm] = @ProcessadoEm
                WHEN NOT MATCHED THEN
                    INSERT ([EventoId], [Origem], [Status], [Mensagem], [ProcessadoEm])
                    VALUES (@EventoId, @Origem, @Status, @Mensagem, @ProcessadoEm);
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@EventoId", webhook.EventoId);
            insertCommand.Parameters.AddWithValue("@Origem", webhook.Origem);
            insertCommand.Parameters.AddWithValue("@Status", webhook.Status);
            insertCommand.Parameters.AddWithValue("@Mensagem", webhook.Mensagem);
            insertCommand.Parameters.AddWithValue("@ProcessadoEm", webhook.ProcessadoEm);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingStringRows(connection, transaction, ProcessedWebhooksTable, "EventoId", WebhooksProcessados.Select(item => item.EventoId));
    }

    private void PersistAuthSessions(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, AuthSessionsTable))
        {
            return;
        }

        foreach (var sessao in SessoesAutenticacao)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{AuthSessionsTable}] AS target
                USING (SELECT @Token AS [Token]) AS source
                ON target.[Token] = source.[Token]
                WHEN MATCHED THEN
                    UPDATE SET [UsuarioId] = @UsuarioId, [EmpresaId] = @EmpresaId, [Email] = @Email, [CriadaEm] = @CriadaEm, [ExpiraEm] = @ExpiraEm
                WHEN NOT MATCHED THEN
                    INSERT ([Token], [UsuarioId], [EmpresaId], [Email], [CriadaEm], [ExpiraEm])
                    VALUES (@Token, @UsuarioId, @EmpresaId, @Email, @CriadaEm, @ExpiraEm);
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Token", sessao.Token);
            insertCommand.Parameters.AddWithValue("@UsuarioId", sessao.UsuarioId);
            insertCommand.Parameters.AddWithValue("@EmpresaId", sessao.EmpresaId);
            insertCommand.Parameters.AddWithValue("@Email", sessao.Email);
            insertCommand.Parameters.AddWithValue("@CriadaEm", sessao.CriadaEm);
            insertCommand.Parameters.AddWithValue("@ExpiraEm", sessao.ExpiraEm);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingStringRows(connection, transaction, AuthSessionsTable, "Token", SessoesAutenticacao.Select(item => item.Token));
    }

    private void PersistRefreshTokens(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, RefreshTokensTable))
        {
            return;
        }

        foreach (var refreshToken in RefreshTokens)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{RefreshTokensTable}] AS target
                USING (SELECT @Token AS [Token]) AS source
                ON target.[Token] = source.[Token]
                WHEN MATCHED THEN
                    UPDATE SET [SessionToken] = @SessionToken, [UsuarioId] = @UsuarioId, [CriadoEm] = @CriadoEm, [ExpiraEm] = @ExpiraEm
                WHEN NOT MATCHED THEN
                    INSERT ([Token], [SessionToken], [UsuarioId], [CriadoEm], [ExpiraEm])
                    VALUES (@Token, @SessionToken, @UsuarioId, @CriadoEm, @ExpiraEm);
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Token", refreshToken.Token);
            insertCommand.Parameters.AddWithValue("@SessionToken", refreshToken.SessionToken);
            insertCommand.Parameters.AddWithValue("@UsuarioId", refreshToken.UsuarioId);
            insertCommand.Parameters.AddWithValue("@CriadoEm", refreshToken.CriadoEm);
            insertCommand.Parameters.AddWithValue("@ExpiraEm", refreshToken.ExpiraEm);
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingStringRows(connection, transaction, RefreshTokensTable, "Token", RefreshTokens.Select(item => item.Token));
    }

    private void PersistSalesOrders(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, SalesOrdersTable))
        {
            return;
        }

        foreach (var pedido in Pedidos.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{SalesOrdersTable}] AS target
                USING (SELECT @Id AS [Id]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [ClienteId] = @ClienteId, [Status] = @Status, [Payload] = @Payload, [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [ClienteId], [Status], [Payload], [UpdatedAt])
                    VALUES (@Id, @ClienteId, @Status, @Payload, SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", pedido.Id);
            insertCommand.Parameters.AddWithValue("@ClienteId", pedido.ClienteId);
            insertCommand.Parameters.AddWithValue("@Status", pedido.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializePedidoVenda(pedido));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, SalesOrdersTable, Pedidos.Keys);
    }

    private void PersistInvoices(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, InvoicesTable))
        {
            return;
        }

        foreach (var nota in NotasFiscais.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{InvoicesTable}] AS target
                USING (SELECT @Id AS [Id]) AS source
                ON target.[Id] = source.[Id]
                WHEN MATCHED THEN
                    UPDATE SET [PedidoVendaId] = @PedidoVendaId, [ClienteId] = @ClienteId, [Status] = @Status, [Payload] = @Payload, [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([Id], [PedidoVendaId], [ClienteId], [Status], [Payload], [UpdatedAt])
                    VALUES (@Id, @PedidoVendaId, @ClienteId, @Status, @Payload, SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@Id", nota.Id);
            insertCommand.Parameters.AddWithValue("@PedidoVendaId", nota.PedidoVendaId);
            insertCommand.Parameters.AddWithValue("@ClienteId", nota.ClienteId);
            insertCommand.Parameters.AddWithValue("@Status", nota.Status.ToString());
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeNotaFiscal(nota));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingGuidRows(connection, transaction, InvoicesTable, NotasFiscais.Keys);
    }

    private void PersistStockBalances(SqlConnection connection, SqlTransaction transaction)
    {
        if (!TableExists(connection, StockBalancesTable))
        {
            return;
        }

        foreach (var saldo in Saldos.Values)
        {
            using var insertCommand = new SqlCommand(
                $"""
                MERGE [{_schema}].[{StockBalancesTable}] AS target
                USING (SELECT @ProdutoId AS [ProdutoId], @DepositoId AS [DepositoId]) AS source
                ON target.[ProdutoId] = source.[ProdutoId] AND target.[DepositoId] = source.[DepositoId]
                WHEN MATCHED THEN
                    UPDATE SET [PermiteSaldoNegativo] = @PermiteSaldoNegativo, [Payload] = @Payload, [UpdatedAt] = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT ([ProdutoId], [DepositoId], [PermiteSaldoNegativo], [Payload], [UpdatedAt])
                    VALUES (@ProdutoId, @DepositoId, @PermiteSaldoNegativo, @Payload, SYSUTCDATETIME());
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("@ProdutoId", saldo.ProdutoId);
            insertCommand.Parameters.AddWithValue("@DepositoId", saldo.DepositoId);
            insertCommand.Parameters.AddWithValue("@PermiteSaldoNegativo", saldo.PermiteSaldoNegativo);
            insertCommand.Parameters.AddWithValue("@Payload", ErpSnapshotSerializer.SerializeSaldoEstoque(saldo));
            insertCommand.ExecuteNonQuery();
        }

        DeleteMissingStockBalances(connection, transaction);
    }

    private void DeleteMissingPurchaseImports(SqlConnection connection, SqlTransaction transaction)
    {
        if (ImportacoesNotaEntrada.Count == 0)
        {
            using var deleteAll = new SqlCommand($"DELETE FROM [{_schema}].[{PurchaseImportsTable}];", connection, transaction);
            deleteAll.ExecuteNonQuery();
            return;
        }

        var clauses = new List<string>(ImportacoesNotaEntrada.Count);
        using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        for (var index = 0; index < ImportacoesNotaEntrada.Count; index++)
        {
            var empresaId = $"@ImportEmpresaId{index}";
            var chaveAcesso = $"@ImportChaveAcesso{index}";
            clauses.Add($"([EmpresaId] = {empresaId} AND [ChaveAcesso] = {chaveAcesso})");
            command.Parameters.AddWithValue(empresaId, ImportacoesNotaEntrada[index].EmpresaId);
            command.Parameters.AddWithValue(chaveAcesso, ImportacoesNotaEntrada[index].ChaveAcesso);
        }

        command.CommandText = $"DELETE FROM [{_schema}].[{PurchaseImportsTable}] WHERE NOT ({string.Join(" OR ", clauses)});";
        command.ExecuteNonQuery();
    }

    private void DeleteMissingStockBalances(SqlConnection connection, SqlTransaction transaction)
    {
        if (Saldos.Count == 0)
        {
            using var deleteAll = new SqlCommand($"DELETE FROM [{_schema}].[{StockBalancesTable}];", connection, transaction);
            deleteAll.ExecuteNonQuery();
            return;
        }

        var clauses = new List<string>(Saldos.Count);
        using var command = new SqlCommand();
        command.Connection = connection;
        command.Transaction = transaction;

        var index = 0;
        foreach (var chave in Saldos.Keys)
        {
            var produtoId = $"@SaldoProdutoId{index}";
            var depositoId = $"@SaldoDepositoId{index}";
            clauses.Add($"([ProdutoId] = {produtoId} AND [DepositoId] = {depositoId})");
            command.Parameters.AddWithValue(produtoId, chave.ProdutoId);
            command.Parameters.AddWithValue(depositoId, chave.DepositoId);
            index++;
        }

        command.CommandText = $"DELETE FROM [{_schema}].[{StockBalancesTable}] WHERE NOT ({string.Join(" OR ", clauses)});";
        command.ExecuteNonQuery();
    }

    private bool TableExists(SqlConnection connection, string tableName)
    {
        using var command = new SqlCommand(
            """
            SELECT COUNT(1)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = @SchemaName AND t.name = @TableName
            """,
            connection);
        command.Parameters.AddWithValue("@SchemaName", _schema);
        command.Parameters.AddWithValue("@TableName", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private int GetDedicatedTablesWithData(SqlConnection connection)
    {
        var dedicatedTablesWithData = 0;
        foreach (var tableName in DedicatedTables)
        {
            if (!TableExists(connection, tableName))
            {
                continue;
            }

            using var command = new SqlCommand($"SELECT TOP (1) 1 FROM [{_schema}].[{tableName}];", connection);
            var result = command.ExecuteScalar();
            if (result is not null && result is not DBNull)
            {
                dedicatedTablesWithData++;
            }
        }

        return dedicatedTablesWithData;
    }
}
