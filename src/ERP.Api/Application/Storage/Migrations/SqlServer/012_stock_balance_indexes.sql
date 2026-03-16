IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SaldosEstoque_DepositoProduto'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SaldosEstoque]')
)
BEGIN
    CREATE INDEX [IX_SaldosEstoque_DepositoProduto]
        ON [{{SCHEMA_NAME}}].[SaldosEstoque] ([DepositoId], [ProdutoId], [UpdatedAt] DESC);
END;
