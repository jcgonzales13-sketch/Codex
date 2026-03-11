namespace ERP.BuildingBlocks;

public sealed record AuditChange(string Field, string PreviousValue, string CurrentValue);
