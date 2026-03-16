namespace ERP.Api.Application.Storage;

internal sealed record SqlServerMigrationScript(string Id, string Description, string FileName);

internal static class SqlServerMigrationScripts
{
    internal static IReadOnlyList<SqlServerMigrationScript> All { get; } =
    [
        new("001", "Create schema, migration history and state store tables", "001_create_schema_and_state.sql"),
        new("002", "Create index for state update ordering", "002_state_indexes.sql"),
        new("003", "Create dedicated stock movement and integration event tables", "003_operational_events_tables.sql"),
        new("004", "Create operational indexes for dedicated tables", "004_operational_events_indexes.sql"),
        new("005", "Create dedicated purchase import and processed webhook tables", "005_operational_imports_and_webhooks_tables.sql"),
        new("006", "Create operational indexes for imports and processed webhooks", "006_operational_imports_and_webhooks_indexes.sql"),
        new("007", "Create dedicated authentication session and refresh token tables", "007_identity_session_tables.sql"),
        new("008", "Create operational indexes for authentication tables", "008_identity_session_indexes.sql"),
        new("009", "Create dedicated sales order and invoice aggregate tables", "009_sales_and_fiscal_tables.sql"),
        new("010", "Create operational indexes for sales order and invoice aggregates", "010_sales_and_fiscal_indexes.sql"),
        new("011", "Create dedicated stock balance aggregate table", "011_stock_balance_table.sql"),
        new("012", "Create operational index for stock balance aggregate table", "012_stock_balance_indexes.sql"),
        new("013", "Create dedicated master data and idempotency marker tables", "013_master_data_tables.sql"),
        new("014", "Create operational indexes for master data and idempotency marker tables", "014_master_data_indexes.sql")
    ];

    internal static string Render(SqlServerMigrationScript script, string schema, string stateTable, string migrationsTable)
    {
        var template = File.ReadAllText(ResolveScriptPath(script.FileName));
        return template
            .Replace("{{SCHEMA_NAME}}", EscapeIdentifier(schema), StringComparison.Ordinal)
            .Replace("{{STATE_TABLE}}", EscapeIdentifier(stateTable), StringComparison.Ordinal)
            .Replace("{{MIGRATIONS_TABLE}}", EscapeIdentifier(migrationsTable), StringComparison.Ordinal);
    }

    private static string ResolveScriptPath(string fileName)
    {
        var relativeOutputPath = Path.Combine(AppContext.BaseDirectory, "Migrations", "SqlServer", fileName);
        if (File.Exists(relativeOutputPath))
        {
            return relativeOutputPath;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "ERP.Api",
                "Application",
                "Storage",
                "Migrations",
                "SqlServer",
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Migration script '{fileName}' was not found.");
    }

    private static string EscapeIdentifier(string value) => value.Replace("]", "]]", StringComparison.Ordinal);
}
