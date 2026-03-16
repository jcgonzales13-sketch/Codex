# Codex

Projeto de ERP modular em .NET 9 com foco em regras de dominio por modulo e cobertura com testes unitarios.

O storage atual ja foi separado internamente por secoes de modulo, mantendo compatibilidade com snapshots legados enquanto prepara a migracao para persistencia definitiva por contexto funcional.

Os endpoints paginados tambem expõem metadados HTTP nos headers `X-Page`, `X-Page-Size`, `X-Total-Count` e `X-Total-Pages`, facilitando integracao com frontends e clientes externos.

A decisao atual do projeto para producao e usar `SqlServer` como persistencia principal, mantendo `JsonFile` para desenvolvimento local e cenarios efemeros.

A borda HTTP das entidades CRUD principais foi alinhada para um modelo mais RESTful:
- `POST` para criacao
- `GET /recurso/{id}` para leitura pontual
- `PUT /recurso/{id}` para atualizacao completa do cadastro
- `PATCH /recurso/{id}` para atualizacao parcial, preservando campos omitidos
- `DELETE /recurso/{id}` para inativacao logica onde o dominio usa soft delete
- prefixo opcional `/api/v1` para a superficie versionada de negocio
- `POST` permanece nas acoes de dominio, como `aprovar`, `reservar`, `autorizar`, `bloquear` e operacoes semelhantes

## Visao Geral

A solucao esta organizada por modulos de negocio independentes, com uma API minima para expor estado da aplicacao e capacidades disponiveis.

Modulos atuais:

- Empresas: cadastro, bloqueio, inativacao e validacao do contexto operacional.
- Fornecedores: cadastro, bloqueio, inativacao e vinculo operacional com compras.
- Catalogo: cadastro de produtos, variacoes e auditoria fiscal.
- Clientes: cadastro, bloqueio, inativacao e consulta operacional de clientes.
- Depositos: cadastro, ativacao, inativacao e validacao operacional de armazenagem.
- Compras: importacao de nota de entrada com conciliacao de itens externos e reflexo em estoque.
- Estoque: ajustes, reservas, baixas por faturamento e transferencias.
- Vendas: aprovacao e reserva de pedidos.
- Fiscal: autorizacao, rejeicao e cancelamento de notas fiscais com repeticao segura de operacoes criticas.
- Identity: cadastro de usuarios, perfis de acesso, senha, login, sessao, bloqueio e permissoes.
- Integracoes: processamento idempotente de webhooks.

## Estrutura

```text
src/
  ERP.Api
  ERP.BuildingBlocks
  ERP.Modules.Empresas
  ERP.Modules.Fornecedores
  ERP.Modules.Catalogo
  ERP.Modules.Clientes
  ERP.Modules.Depositos
  ERP.Modules.Compras
  ERP.Modules.Estoque
  ERP.Modules.Fiscal
  ERP.Modules.Identity
  ERP.Modules.Integracoes
  ERP.Modules.Vendas
tests/
  ERP.Api.IntegrationTests
  ERP.Modules.Empresas.UnitTests
  ERP.Modules.Fornecedores.UnitTests
  ERP.Modules.Catalogo.UnitTests
  ERP.Modules.Clientes.UnitTests
  ERP.Modules.Depositos.UnitTests
  ERP.Modules.Compras.UnitTests
  ERP.Modules.Estoque.UnitTests
  ERP.Modules.Fiscal.UnitTests
  ERP.Modules.Identity.UnitTests
  ERP.Modules.Integracoes.UnitTests
  ERP.Modules.Vendas.UnitTests
```

## Requisitos

- .NET SDK 9.0.101
- Git

O SDK fixado no projeto esta definido em [`global.json`](/c:/CodexProject/global.json).

## Como Executar

Restaurar dependencias:

```powershell
dotnet restore ERP.sln
```

Compilar a solucao:

```powershell
dotnet build ERP.sln
```

Executar a API:

```powershell
dotnet run --project .\src\ERP.Api\ERP.Api.csproj
```

Execucao local recomendada neste ambiente:

```powershell
$env:MSBuildEnableWorkloadResolver='false'
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project .\src\ERP.Api\ERP.Api.csproj
```

Os logs da API sao emitidos em JSON no console, com `scope`, `timestamp`, `X-Correlation-Id` e trilha minima de operacoes sensiveis para facilitar suporte, observabilidade e deploy em plataforma.

Por padrao, a API expoe endpoints minimos:

- `GET /`
- `GET /health`
- `GET /healthz`
- `POST /healthz`
- `GET /health/ready`
- `GET /swagger`
- `GET /modules`
- `GET /api/v1/modules`
- `GET /system/storage`
- `GET /api/v1/system/storage`
- `GET /system/events`
- `GET /api/v1/system/events`
- `GET /system/metrics`
- `GET /api/v1/system/metrics`
- `GET /estoque/movimentos`
- `GET /empresas`
- `GET /empresas/{id}`
- `GET /fornecedores`
- `GET /fornecedores/{id}`
- `GET /catalogo/produtos`
- `GET /catalogo/produtos/{id}`
- `GET /clientes`
- `GET /clientes/{id}`
- `GET /depositos`
- `GET /depositos/{id}`
- `GET /identity/usuarios`
- `GET /identity/usuarios/{id}`
- `GET /identity/me`
- `GET /identity/permissoes`
- `GET /identity/perfis/padroes`
- `GET /identity/perfis`
- `GET /identity/perfis/{id}`
- `POST /identity/auth/login`
- `POST /identity/oauth/token`
- `POST /identity/oauth/refresh`
- `GET /compras/importacoes-nota-entrada`
- `GET /vendas/pedidos`
- `GET /vendas/pedidos/{id}`
- `GET /fiscal/notas`
- `GET /fiscal/notas/{id}`
- `GET /integracoes/webhooks`
- `GET /api/v1/health/ready`

## Como Rodar os Testes

Todos os testes:

```powershell
dotnet test ERP.sln
```

Somente integracao HTTP da API:

```powershell
dotnet test .\tests\ERP.Api.IntegrationTests\ERP.Api.IntegrationTests.csproj
```

Somente um modulo:

```powershell
dotnet test .\tests\ERP.Modules.Estoque.UnitTests\ERP.Modules.Estoque.UnitTests.csproj
```

Observacao: no ambiente onde este repositorio foi preparado, o SDK .NET 9 pode falhar por interferencia do workload resolver. Se isso acontecer, execute os comandos com `MSBuildEnableWorkloadResolver=false`, por exemplo: `$env:MSBuildEnableWorkloadResolver='false'; dotnet test ERP.sln`.

## Execucao por Ambiente

`Development`

- `ASPNETCORE_ENVIRONMENT=Development`
- `Storage__Provider=JsonFile`
- `Storage__FilePath=App_Data/erp-store.json`
- `Jwt__SigningKey` pode permanecer local, mas deve ser configurada explicitamente fora do codigo quando a API sair de um ambiente efemero

`Render`

- `ASPNETCORE_ENVIRONMENT=Production`
- `Storage__Provider=JsonFile` com persistent disk em `/data`, ou `InMemory` para ambiente descartavel
- `Storage__FilePath=/data/erp-store.json`
- `Jwt__SigningKey` obrigatoria e forte
- `WebhookSecurity__SharedSecret` obrigatoria quando houver integracoes externas com assinatura

`CI`

- o workflow usa `MSBuildEnableWorkloadResolver=false`
- executa `restore`, `build` e `test` em `ERP.sln`
- arquivo do pipeline: [ci.yml](/c:/CodexProject/.github/workflows/ci.yml)

`SqlServer`

- o provider aplica bootstrap e migrations iniciais automaticamente ao subir
- as migrations tambem estao versionadas em arquivos `.sql` sob [SqlServer](/c:/CodexProject/src/ERP.Api/Application/Storage/Migrations/SqlServer)
- os dados de snapshot continuam separados por secao em `Storage__StateTable`
- o provider SQL agora possui tabelas dedicadas para dados mestres, marcadores de idempotencia e agregados operacionais, reduzindo drasticamente a dependencia do `ErpState` para compatibilidade e fallback
- tabelas padrao:
  - `Storage__StateTable=ErpState`
  - `Storage__MigrationsTable=ErpMigrations`
  - `dbo.MovimentosEstoque`
  - `dbo.IntegrationEvents`
  - `dbo.ImportacoesNotaEntrada`
  - `dbo.WebhooksProcessados`
  - `dbo.SessoesAutenticacao`
  - `dbo.RefreshTokens`
  - `dbo.PedidosVenda`
  - `dbo.NotasFiscais`
  - `dbo.SaldosEstoque`
  - `dbo.Empresas`
  - `dbo.Fornecedores`
  - `dbo.Produtos`
  - `dbo.Clientes`
  - `dbo.Depositos`
  - `dbo.Usuarios`
  - `dbo.PerfisAcesso`
  - `dbo.ChavesImportadas`
  - `dbo.EventosWebhook`
- schema padrao: `dbo`
- configuracao de producao exemplo: [appsettings.Production.json](/c:/CodexProject/src/ERP.Api/appsettings.Production.json)
- decisao registrada em [adr-001-persistencia-producao.md](/c:/CodexProject/docs/adr-001-persistencia-producao.md)

## Autenticacao Basica

 A API agora suporta sessao simples por token via modulo `Identity`.
Tambem suporta emissao de JWT bearer via endpoint de token.

Fluxo minimo:

1. Criar o primeiro usuario da empresa.
2. Definir senha com `POST /identity/usuarios/{usuarioId}/senha`.
3. Fazer login em `POST /identity/auth/login`.
4. Enviar o token retornado no header `X-Session-Token` nas operacoes mutaveis protegidas.

Opcionalmente:

5. Solicitar um JWT em `POST /identity/oauth/token`.
6. Enviar `Authorization: Bearer {token}` nas operacoes mutaveis protegidas.
7. Quando o access token expirar, renovar em `POST /identity/oauth/refresh`.
8. Para `POST /integracoes/webhooks`, em chamadas externas sem sessao, enviar `X-Webhook-Signature` com a assinatura HMAC-SHA256 de `EventoId:Origem:Payload`.
9. Configurar `WebhookSecurity:SharedSecret` no ambiente para habilitar a validacao da assinatura.

Observacao: o primeiro usuario cadastrado para cada empresa recebe automaticamente o perfil padrao `Administrador`, e a resposta de cadastro retorna `bootstrapAdministrador = true`.

Exemplo resumido de resposta no bootstrap do primeiro usuario:

```json
{
  "success": true,
  "data": {
    "email": "alpha@empresa.com",
    "status": "Ativo",
    "bootstrapAdministrador": true
  },
  "error": null
}
```

Exemplo resumido de resposta em `POST /identity/oauth/token`:

```json
{
  "success": true,
  "data": {
    "accessToken": "...",
    "refreshToken": "...",
    "tokenType": "Bearer"
  },
  "error": null
}
```

Permissoes operacionais atuais:

- `ADMIN`
- `EMPRESAS_MANAGE`
- `CATALOGO_MANAGE`
- `CLIENTES_MANAGE`
- `FORNECEDORES_MANAGE`
- `DEPOSITOS_MANAGE`
- `IDENTITY_MANAGE`
- `ESTOQUE_MANAGE`
- `VENDAS_MANAGE`
- `COMPRAS_MANAGE`
- `FISCAL_MANAGE`
- `INTEGRACOES_MANAGE`

Esse catalogo tambem pode ser consultado via `GET /identity/permissoes`.

## Endpoints da API

`GET /`

Retorna o estado online da aplicacao e os modulos carregados pela API.

`GET /health`

Retorna status de saude simples com timestamp UTC.

`GET /healthz`

Alias simples de health check para deploy e monitoramento externo.

`POST /healthz`

Alias em `POST` para teste manual em ferramentas como Postman.

`GET /health/ready`

Executa o health check do provider de storage ativo.

`GET /swagger`

Abre a interface Swagger UI da API.

`GET /modules`

Retorna a lista de modulos e suas capacidades principais.

`GET /api/v1/modules`

Retorna a mesma lista de modulos pela superficie versionada v1.

`GET /system/storage`

Retorna o provider de armazenamento ativo e a contagem atual de entidades em memoria/persistencia.

`GET /api/v1/system/storage`

Retorna o diagnostico do storage pela superficie versionada v1.

`GET /system/events`

Retorna a trilha de eventos internos gerados pelas operacoes integradas entre modulos, com filtros opcionais por tipo e modulo de origem.

`GET /api/v1/system/events`

Retorna a mesma trilha de eventos internos pela superficie versionada v1.

`GET /system/metrics`

Retorna metricas operacionais em memoria sobre requests HTTP, falhas, operacoes de dominio e excecoes. Tambem ha `ActivitySource` e `Meter` internos para evoluir a observabilidade da API.

`GET /api/v1/system/metrics`

Retorna as mesmas metricas operacionais pela superficie versionada v1.

`GET /api/v1/health/ready`

Executa o readiness check pela superficie versionada v1.

`GET /estoque/movimentos`

Retorna o historico operacional de movimentos de estoque, com filtro opcional por produto e deposito.

`GET /empresas`

Retorna empresas com filtros opcionais por status, termo e paginacao.

`PATCH /empresas/{id}`

Aplica alteracoes parciais no cadastro da empresa, mantendo os campos omitidos.

`DELETE /empresas/{id}`

Executa a inativacao logica da empresa usando semantica RESTful.

`GET /catalogo/produtos`

Retorna produtos com filtros opcionais por empresa, status ativo, termo e paginacao.

`GET /clientes`

Retorna clientes com filtros opcionais por empresa, status, termo e paginacao.

`PATCH /clientes/{id}`

Aplica alteracoes parciais no cadastro do cliente, preservando os campos omitidos.

`DELETE /clientes/{id}`

Executa a inativacao logica do cliente usando semantica RESTful.

`GET /fornecedores`

Retorna fornecedores com filtros opcionais por empresa, status, termo e paginacao.

`PATCH /fornecedores/{id}`

Aplica alteracoes parciais no cadastro do fornecedor, preservando os campos omitidos.

`DELETE /fornecedores/{id}`

Executa a inativacao logica do fornecedor usando semantica RESTful.

`GET /depositos`

Retorna depositos com filtros opcionais por empresa, status, termo e paginacao.

`PATCH /depositos/{id}`

Aplica alteracoes parciais no cadastro do deposito, preservando o nome atual quando o campo nao for enviado.

`DELETE /depositos/{id}`

Executa a inativacao logica do deposito usando semantica RESTful.

`GET /identity/usuarios`

Retorna usuarios com filtros opcionais por empresa, status, termo e paginacao.

`GET /identity/me`

Retorna a sessao autenticada atual resolvida a partir do `Authorization: Bearer` ou `X-Session-Token`.

`GET /identity/permissoes`

Retorna o catalogo de permissoes reconhecidas pela API para concessao e validacao de acesso.

`GET /identity/perfis/padroes`

Retorna o catalogo de perfis padrao disponibilizados pela aplicacao para novas empresas.

`GET /identity/perfis`

Retorna perfis de acesso por empresa, com filtros por termo e paginacao.

`PATCH /identity/perfis/{id}`

Permite alterar parcialmente nome e permissoes do perfil de acesso, preservando os campos omitidos.

`POST /identity/auth/login`

Realiza autenticacao por empresa, email e senha, retornando token de sessao.

`POST /identity/oauth/token`

Emite um JWT bearer assinado a partir das credenciais do usuario.

`POST /identity/oauth/refresh`

Renova a sessao JWT a partir de um refresh token valido.

`GET /compras/importacoes-nota-entrada`

Retorna o historico das importacoes de nota de entrada, com filtros por empresa, fornecedor, deposito, sucesso da importacao, chave e paginacao.

`GET /vendas/pedidos`

Retorna pedidos com filtros opcionais por status, cliente e paginacao.

`GET /fiscal/notas`

Retorna notas fiscais com filtros opcionais por status, cliente e paginacao.

`GET /integracoes/webhooks`

Retorna o historico operacional de webhooks processados, com filtros por origem, status, evento e paginacao.

`POST /integracoes/webhooks`

Recebe um webhook externo, aplica idempotencia e registra o resultado do processamento. O endpoint aceita sessao autenticada com permissao `INTEGRACOES_MANAGE` ou assinatura no header `X-Webhook-Signature`.

Exemplos de consulta:

- `GET /catalogo/produtos?ativo=true&page=1&pageSize=20&termo=SKU`
- `GET /empresas?status=Ativa&page=1&pageSize=20&termo=Empresa`
- `GET /fornecedores?empresaId={empresaId}&status=Ativo&page=1&pageSize=20&termo=Fornecedor`
- `GET /clientes?empresaId={empresaId}&status=Ativo&page=1&pageSize=20&termo=Cliente`
- `GET /depositos?empresaId={empresaId}&status=Ativo&page=1&pageSize=20&termo=DEP`
- `GET /identity/usuarios?status=Ativo&page=1&pageSize=20&termo=usuario`
- `GET /identity/permissoes`
- `GET /identity/perfis/padroes`
- `GET /identity/perfis?empresaId={empresaId}&page=1&pageSize=20&termo=estoque`
- `POST /identity/auth/login`
- `POST /identity/oauth/token`
- `POST /identity/oauth/refresh`
- `GET /compras/importacoes-nota-entrada?empresaId={empresaId}&fornecedorId={fornecedorId}&depositoId={depositoId}&importadaComSucesso=true&page=1&pageSize=20`
- `GET /estoque/saldos?produtoId={produtoId}&depositoId={depositoId}&page=1&pageSize=20`
- `GET /estoque/movimentos?produtoId={produtoId}&depositoId={depositoId}&page=1&pageSize=20`
- `GET /vendas/pedidos?status=Reservado&clienteId={clienteId}&page=1&pageSize=20`
- `GET /fiscal/notas?status=Autorizada&clienteId={clienteId}&page=1&pageSize=20`
- `GET /integracoes/webhooks?origem=marketplace&status=Processado&page=1&pageSize=20`
- `GET /system/events?type=vendas.pedido_reservado&page=1&pageSize=20`
- `GET /api/v1/system/events?type=vendas.pedido_reservado&page=1&pageSize=20`
- `GET /api/v1/system/metrics`

## Persistencia Local

O projeto suporta provider configuravel em `Storage`:

- `InMemory`: desenvolvimento rapido sem persistencia.
- `JsonFile`: provider ativo para desenvolvimento local com persistencia em arquivo.
- `SqlServer`: persiste o estado da aplicacao por secoes no SQL Server e ja usa tabelas dedicadas para eventos de integracao e movimentos de estoque.

Exemplo em `appsettings`:

```json
"Storage": {
  "Provider": "JsonFile",
  "FilePath": "App_Data/erp-store.json"
}
```

Exemplo com SQL Server local:

```json
"Storage": {
  "Provider": "SqlServer",
  "ConnectionString": "Server=GONZALES;Database=CodexErp;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;",
  "Schema": "dbo",
  "StateTable": "ErpState",
  "MigrationsTable": "ErpMigrations"
}
```

## Deploy no Render

O repositorio agora inclui [`Dockerfile`](/c:/CodexProject/Dockerfile) e [`.dockerignore`](/c:/CodexProject/.dockerignore) para publicar a API no plano free do Render via container.

O arquivo [`global.json`](/c:/CodexProject/global.json) foi configurado com `rollForward: latestFeature` para aceitar SDKs `9.0.x` mais novos no ambiente de build do Render, evitando falha quando o host tiver uma feature band diferente da maquina local.

Configuracao recomendada no Render:

- Environment: `Docker`
- Dockerfile Path: `./Dockerfile`
- Health Check Path: `/health`
- Disk: adicionar um persistent disk montado em `/data` se quiser manter o provider `JsonFile` entre reinicios

Variaveis uteis:

- `Storage__Provider=JsonFile`
- `Storage__FilePath=/data/erp-store.json`
- `Jwt__Issuer=CodexErp`
- `Jwt__Audience=CodexErpClients`
- `Jwt__SigningKey=<segredo-forte>`
- `WebhookSecurity__SharedSecret=<segredo-do-webhook>`
- ou `Storage__Provider=InMemory` para ambiente efemero

O container ja respeita a variavel `PORT` do Render e usa `ForwardedHeaders` para funcionar corretamente atras do proxy HTTPS da plataforma.

Para o primeiro deploy no Render, a sequencia recomendada e:

1. publicar a imagem a partir do `Dockerfile`
2. configurar `PORT`, `ASPNETCORE_ENVIRONMENT`, `Storage__*`, `Jwt__*` e `WebhookSecurity__SharedSecret`
3. validar `/healthz`, `/health/ready` e `/swagger`
4. criar a primeira empresa e o primeiro usuario administrador

Observacao importante sobre esta maquina:

- a instancia local `MSSQLSERVER` esta em execucao;
- `TCP/IP` e `Named Pipes` estavam desabilitados no SQL Server Network Configuration;
- a conectividade de rede evoluiu, mas o ambiente local ainda apresenta bloqueio de handshake/criptografia no cliente SQL;
- por isso o provider ativo do projeto voltou temporariamente para `JsonFile`;
- o provider `SqlServer` permanece no codigo e pode ser retomado quando a infra local estiver estavel.

## Objetivo do Projeto

Este repositorio serve como base de estudo e evolucao para:

- modelagem de dominio por contexto funcional;
- testes unitarios por modulo;
- organizacao de uma solucao .NET modular;
- exposicao inicial de capacidades por uma API leve.
- orquestracao entre compras, estoque, vendas e fiscal.
- repeticao segura de operacoes integradas mais sensiveis.

## Proximos Passos Sugeridos

- adicionar persistencia real por modulo;
- continuar substituindo identificadores soltos por cadastros operacionais reais;
- configurar CI para `build` e `test`;
- melhorar observabilidade e tratamento de erros na API.
