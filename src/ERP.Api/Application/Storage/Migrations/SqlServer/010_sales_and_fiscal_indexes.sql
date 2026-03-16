IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PedidosVenda_ClienteStatus'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[PedidosVenda]')
)
BEGIN
    CREATE INDEX [IX_PedidosVenda_ClienteStatus]
        ON [{{SCHEMA_NAME}}].[PedidosVenda] ([ClienteId], [Status], [UpdatedAt] DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_NotasFiscais_PedidoStatus'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[NotasFiscais]')
)
BEGIN
    CREATE INDEX [IX_NotasFiscais_PedidoStatus]
        ON [{{SCHEMA_NAME}}].[NotasFiscais] ([PedidoVendaId], [Status], [UpdatedAt] DESC);
END;
