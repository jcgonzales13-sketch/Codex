IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[MovimentosEstoque]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[MovimentosEstoque]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProdutoId] UNIQUEIDENTIFIER NOT NULL,
        [DepositoId] UNIQUEIDENTIFIER NOT NULL,
        [Tipo] NVARCHAR(100) NOT NULL,
        [Quantidade] DECIMAL(18, 4) NOT NULL,
        [Motivo] NVARCHAR(200) NOT NULL,
        [DocumentoOrigem] NVARCHAR(200) NOT NULL,
        [SaldoAnterior] DECIMAL(18, 4) NOT NULL,
        [SaldoPosterior] DECIMAL(18, 4) NOT NULL,
        [DataHora] DATETIMEOFFSET NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[IntegrationEvents]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[IntegrationEvents]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [Type] NVARCHAR(150) NOT NULL,
        [SourceModule] NVARCHAR(100) NOT NULL,
        [AggregateId] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NOT NULL,
        [OccurredAt] DATETIMEOFFSET NOT NULL
    );
END;
