IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[SessoesAutenticacao]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[SessoesAutenticacao]
    (
        [Token] NVARCHAR(200) NOT NULL PRIMARY KEY,
        [UsuarioId] UNIQUEIDENTIFIER NOT NULL,
        [EmpresaId] UNIQUEIDENTIFIER NOT NULL,
        [Email] NVARCHAR(200) NOT NULL,
        [CriadaEm] DATETIMEOFFSET NOT NULL,
        [ExpiraEm] DATETIMEOFFSET NOT NULL
    );
END;

IF OBJECT_ID(N'[{{SCHEMA_NAME}}].[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [{{SCHEMA_NAME}}].[RefreshTokens]
    (
        [Token] NVARCHAR(200) NOT NULL PRIMARY KEY,
        [SessionToken] NVARCHAR(200) NOT NULL,
        [UsuarioId] UNIQUEIDENTIFIER NOT NULL,
        [CriadoEm] DATETIMEOFFSET NOT NULL,
        [ExpiraEm] DATETIMEOFFSET NOT NULL
    );
END;
