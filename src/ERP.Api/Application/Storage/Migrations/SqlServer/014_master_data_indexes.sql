IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Empresas_Documento'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Empresas]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Empresas_Documento]
        ON [{{SCHEMA_NAME}}].[Empresas] ([Documento]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Fornecedores_EmpresaDocumento'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Fornecedores]')
)
BEGIN
    CREATE INDEX [IX_Fornecedores_EmpresaDocumento]
        ON [{{SCHEMA_NAME}}].[Fornecedores] ([EmpresaId], [Documento]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Produtos_EmpresaSku'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Produtos]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Produtos_EmpresaSku]
        ON [{{SCHEMA_NAME}}].[Produtos] ([EmpresaId], [Sku]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Clientes_EmpresaDocumento'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Clientes]')
)
BEGIN
    CREATE INDEX [IX_Clientes_EmpresaDocumento]
        ON [{{SCHEMA_NAME}}].[Clientes] ([EmpresaId], [Documento]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Depositos_EmpresaCodigo'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Depositos]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Depositos_EmpresaCodigo]
        ON [{{SCHEMA_NAME}}].[Depositos] ([EmpresaId], [Codigo]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Usuarios_EmpresaEmail'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Usuarios]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuarios_EmpresaEmail]
        ON [{{SCHEMA_NAME}}].[Usuarios] ([EmpresaId], [Email]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PerfisAcesso_EmpresaNome'
      AND object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[PerfisAcesso]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_PerfisAcesso_EmpresaNome]
        ON [{{SCHEMA_NAME}}].[PerfisAcesso] ([EmpresaId], [Nome]);
END;
