IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[SaldosEstoque]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[SaldosEstoque]
    (
        [ProdutoId] UNIQUEIDENTIFIER NOT NULL,
        [DepositoId] UNIQUEIDENTIFIER NOT NULL,
        [PermiteSaldoNegativo] BIT NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_SaldosEstoque] PRIMARY KEY ([ProdutoId], [DepositoId])
    );
END;
