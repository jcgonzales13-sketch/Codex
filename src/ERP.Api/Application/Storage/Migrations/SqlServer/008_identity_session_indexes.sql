IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_SessoesAutenticacao_UsuarioExpiraEm'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SessoesAutenticacao]')
)
BEGIN
    CREATE INDEX [IX_SessoesAutenticacao_UsuarioExpiraEm]
        ON [{{SCHEMA_NAME}}].[SessoesAutenticacao] ([UsuarioId], [ExpiraEm] DESC);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_RefreshTokens_UsuarioExpiraEm'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[RefreshTokens]')
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UsuarioExpiraEm]
        ON [{{SCHEMA_NAME}}].[RefreshTokens] ([UsuarioId], [ExpiraEm] DESC);
END;
