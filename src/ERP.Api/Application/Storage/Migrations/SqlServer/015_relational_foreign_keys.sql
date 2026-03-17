IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Fornecedores_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Fornecedores]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[Fornecedores] WITH CHECK
    ADD CONSTRAINT [FK_Fornecedores_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Produtos_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Produtos]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[Produtos] WITH CHECK
    ADD CONSTRAINT [FK_Produtos_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Clientes_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Clientes]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[Clientes] WITH CHECK
    ADD CONSTRAINT [FK_Clientes_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Depositos_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Depositos]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[Depositos] WITH CHECK
    ADD CONSTRAINT [FK_Depositos_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Usuarios_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[Usuarios]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[Usuarios] WITH CHECK
    ADD CONSTRAINT [FK_Usuarios_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_PerfisAcesso_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[PerfisAcesso]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[PerfisAcesso] WITH CHECK
    ADD CONSTRAINT [FK_PerfisAcesso_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ChavesImportadas_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[ChavesImportadas]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[ChavesImportadas] WITH CHECK
    ADD CONSTRAINT [FK_ChavesImportadas_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_PedidosVenda_Clientes_ClienteId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[PedidosVenda]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[PedidosVenda] WITH CHECK
    ADD CONSTRAINT [FK_PedidosVenda_Clientes_ClienteId]
        FOREIGN KEY ([ClienteId]) REFERENCES [{{SCHEMA_NAME}}].[Clientes] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_NotasFiscais_PedidosVenda_PedidoVendaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[NotasFiscais]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[NotasFiscais] WITH CHECK
    ADD CONSTRAINT [FK_NotasFiscais_PedidosVenda_PedidoVendaId]
        FOREIGN KEY ([PedidoVendaId]) REFERENCES [{{SCHEMA_NAME}}].[PedidosVenda] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_NotasFiscais_Clientes_ClienteId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[NotasFiscais]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[NotasFiscais] WITH CHECK
    ADD CONSTRAINT [FK_NotasFiscais_Clientes_ClienteId]
        FOREIGN KEY ([ClienteId]) REFERENCES [{{SCHEMA_NAME}}].[Clientes] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SaldosEstoque_Produtos_ProdutoId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SaldosEstoque]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[SaldosEstoque] WITH CHECK
    ADD CONSTRAINT [FK_SaldosEstoque_Produtos_ProdutoId]
        FOREIGN KEY ([ProdutoId]) REFERENCES [{{SCHEMA_NAME}}].[Produtos] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SaldosEstoque_Depositos_DepositoId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SaldosEstoque]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[SaldosEstoque] WITH CHECK
    ADD CONSTRAINT [FK_SaldosEstoque_Depositos_DepositoId]
        FOREIGN KEY ([DepositoId]) REFERENCES [{{SCHEMA_NAME}}].[Depositos] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovimentosEstoque_Produtos_ProdutoId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[MovimentosEstoque]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[MovimentosEstoque] WITH CHECK
    ADD CONSTRAINT [FK_MovimentosEstoque_Produtos_ProdutoId]
        FOREIGN KEY ([ProdutoId]) REFERENCES [{{SCHEMA_NAME}}].[Produtos] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovimentosEstoque_Depositos_DepositoId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[MovimentosEstoque]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[MovimentosEstoque] WITH CHECK
    ADD CONSTRAINT [FK_MovimentosEstoque_Depositos_DepositoId]
        FOREIGN KEY ([DepositoId]) REFERENCES [{{SCHEMA_NAME}}].[Depositos] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ImportacoesNotaEntrada_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[ImportacoesNotaEntrada] WITH CHECK
    ADD CONSTRAINT [FK_ImportacoesNotaEntrada_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ImportacoesNotaEntrada_Fornecedores_FornecedorId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[ImportacoesNotaEntrada] WITH CHECK
    ADD CONSTRAINT [FK_ImportacoesNotaEntrada_Fornecedores_FornecedorId]
        FOREIGN KEY ([FornecedorId]) REFERENCES [{{SCHEMA_NAME}}].[Fornecedores] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_ImportacoesNotaEntrada_Depositos_DepositoId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[ImportacoesNotaEntrada] WITH CHECK
    ADD CONSTRAINT [FK_ImportacoesNotaEntrada_Depositos_DepositoId]
        FOREIGN KEY ([DepositoId]) REFERENCES [{{SCHEMA_NAME}}].[Depositos] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SessoesAutenticacao_Usuarios_UsuarioId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SessoesAutenticacao]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[SessoesAutenticacao] WITH CHECK
    ADD CONSTRAINT [FK_SessoesAutenticacao_Usuarios_UsuarioId]
        FOREIGN KEY ([UsuarioId]) REFERENCES [{{SCHEMA_NAME}}].[Usuarios] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_SessoesAutenticacao_Empresas_EmpresaId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[SessoesAutenticacao]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[SessoesAutenticacao] WITH CHECK
    ADD CONSTRAINT [FK_SessoesAutenticacao_Empresas_EmpresaId]
        FOREIGN KEY ([EmpresaId]) REFERENCES [{{SCHEMA_NAME}}].[Empresas] ([Id]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_RefreshTokens_SessoesAutenticacao_SessionToken'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[RefreshTokens]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[RefreshTokens] WITH CHECK
    ADD CONSTRAINT [FK_RefreshTokens_SessoesAutenticacao_SessionToken]
        FOREIGN KEY ([SessionToken]) REFERENCES [{{SCHEMA_NAME}}].[SessoesAutenticacao] ([Token]);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_RefreshTokens_Usuarios_UsuarioId'
      AND parent_object_id = OBJECT_ID(N'[{{SCHEMA_NAME}}].[RefreshTokens]')
)
BEGIN
    ALTER TABLE [{{SCHEMA_NAME}}].[RefreshTokens] WITH CHECK
    ADD CONSTRAINT [FK_RefreshTokens_Usuarios_UsuarioId]
        FOREIGN KEY ([UsuarioId]) REFERENCES [{{SCHEMA_NAME}}].[Usuarios] ([Id]);
END;
