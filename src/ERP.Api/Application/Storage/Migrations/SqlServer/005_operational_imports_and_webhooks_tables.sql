IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[ImportacoesNotaEntrada]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [FornecedorId] UNIQUEIDENTIFIER NOT NULL,
        [DepositoId] UNIQUEIDENTIFIER NOT NULL,
        [ChaveAcesso] NVARCHAR(100) NOT NULL,
        [ImportadaComSucesso] BIT NOT NULL,
        [ItensExternos] INT NOT NULL,
        [ItensPendentesConciliacao] INT NOT NULL,
        [MovimentosGerados] INT NOT NULL,
        [ProcessadaEm] DATETIMEOFFSET NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[WebhooksProcessados]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[WebhooksProcessados]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventoId] NVARCHAR(100) NOT NULL,
        [Origem] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(100) NOT NULL,
        [Mensagem] NVARCHAR(500) NOT NULL,
        [ProcessadoEm] DATETIMEOFFSET NOT NULL
    );
END;
