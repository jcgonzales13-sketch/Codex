IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimentosEstoque_DataHora'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[MovimentosEstoque]')
)
BEGIN
    CREATE INDEX [IX_MovimentosEstoque_DataHora]
        ON [{{SCHEMA_NAME}}].[MovimentosEstoque] ([DataHora] DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovimentosEstoque_ProdutoDeposito'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[MovimentosEstoque]')
)
BEGIN
    CREATE INDEX [IX_MovimentosEstoque_ProdutoDeposito]
        ON [{{SCHEMA_NAME}}].[MovimentosEstoque] ([ProdutoId], [DepositoId], [DataHora] DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_IntegrationEvents_OccurredAt'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[IntegrationEvents]')
)
BEGIN
    CREATE INDEX [IX_IntegrationEvents_OccurredAt]
        ON [{{SCHEMA_NAME}}].[IntegrationEvents] ([OccurredAt] DESC);
END;
