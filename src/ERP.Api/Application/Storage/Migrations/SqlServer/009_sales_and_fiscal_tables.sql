IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[PedidosVenda]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[PedidosVenda]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ClienteId] UNIQUEIDENTIFIER NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[NotasFiscais]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[NotasFiscais]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [PedidoVendaId] UNIQUEIDENTIFIER NOT NULL,
        [ClienteId] UNIQUEIDENTIFIER NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;
