IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_ImportacoesNotaEntrada_ProcessadaEm'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]')
)
BEGIN
    CREATE INDEX [IX_ImportacoesNotaEntrada_ProcessadaEm]
        ON [{{SCHEMA_NAME}}].[ImportacoesNotaEntrada] ([ProcessadaEm] DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_WebhooksProcessados_ProcessadoEm'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[WebhooksProcessados]')
)
BEGIN
    CREATE INDEX [IX_WebhooksProcessados_ProcessadoEm]
        ON [{{SCHEMA_NAME}}].[WebhooksProcessados] ([ProcessadoEm] DESC);
END;
