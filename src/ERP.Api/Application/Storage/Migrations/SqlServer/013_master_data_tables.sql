IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Empresas]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Empresas]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [Documento] NVARCHAR(50) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Fornecedores]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Fornecedores]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Documento] NVARCHAR(50) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Produtos]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Produtos]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Sku] NVARCHAR(100) NOT NULL,
        [Ativo] BIT NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Clientes]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Clientes]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Documento] NVARCHAR(50) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Depositos]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Depositos]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Codigo] NVARCHAR(50) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[Usuarios]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[Usuarios]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Email] NVARCHAR(200) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[PerfisAcesso]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[PerfisAcesso]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Nome] NVARCHAR(150) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[ChavesImportadas]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[ChavesImportadas]
    (
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [ChaveAcesso] NVARCHAR(100) NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_ChavesImportadas] PRIMARY KEY ([EmpresaId], [ChaveAcesso])
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[EventosWebhook]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[EventosWebhook]
    (
        [EventoId] NVARCHAR(100) NOT NULL PRIMARY KEY,
        [UpdatedAt] DATETIME2 NOT NULL
    );
END;
