namespace ERP.Api.Application.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "InMemory";
    public string FilePath { get; set; } = "App_Data/erp-store.json";
    public string? ConnectionString { get; set; }
    public string Schema { get; set; } = "dbo";
    public string StateTable { get; set; } = "ErpState";
    public string MigrationsTable { get; set; } = "ErpMigrations";
    public bool PersistLegacyStateSnapshot { get; set; } = true;
}
