# Codex

Projeto de ERP modular em .NET 9 com foco em regras de dominio por modulo e cobertura com testes unitarios.

## Visao Geral

A solucao esta organizada por modulos de negocio independentes, com uma API minima para expor estado da aplicacao e capacidades disponiveis.

Modulos atuais:

- Empresas: cadastro, bloqueio, inativacao e validacao do contexto operacional.
- Catalogo: cadastro de produtos, variacoes e auditoria fiscal.
- Clientes: cadastro, bloqueio, inativacao e consulta operacional de clientes.
- Depositos: cadastro, ativacao, inativacao e validacao operacional de armazenagem.
- Compras: importacao de nota de entrada com conciliacao de itens externos e reflexo em estoque.
- Estoque: ajustes, reservas, baixas por faturamento e transferencias.
- Vendas: aprovacao e reserva de pedidos.
- Fiscal: autorizacao, rejeicao e cancelamento de notas fiscais com repeticao segura de operacoes criticas.
- Identity: cadastro de usuarios, ativacao, bloqueio e permissoes.
- Integracoes: processamento idempotente de webhooks.

## Estrutura

```text
src/
  ERP.Api
  ERP.BuildingBlocks
  ERP.Modules.Empresas
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
  ERP.Modules.Empresas.UnitTests
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

Por padrao, a API expoe endpoints minimos:

- `GET /`
- `GET /health`
- `GET /health/ready`
- `GET /modules`
- `GET /system/storage`
- `GET /system/events`
- `GET /estoque/movimentos`
- `GET /empresas`
- `GET /catalogo/produtos`
- `GET /clientes`
- `GET /depositos`
- `GET /identity/usuarios`
- `GET /compras/importacoes-nota-entrada`
- `GET /vendas/pedidos`
- `GET /fiscal/notas`
- `GET /integracoes/webhooks`

## Como Rodar os Testes

Todos os testes:

```powershell
dotnet test ERP.sln
```

Somente um modulo:

```powershell
dotnet test .\tests\ERP.Modules.Estoque.UnitTests\ERP.Modules.Estoque.UnitTests.csproj
```

Observacao: no ambiente onde este repositorio foi preparado, o SDK .NET 9 pode falhar por interferencia do workload resolver. Se isso acontecer, execute os comandos com `MSBuildEnableWorkloadResolver=false`, por exemplo: `$env:MSBuildEnableWorkloadResolver='false'; dotnet test ERP.sln`.

## Endpoints da API

`GET /`

Retorna o estado online da aplicacao e os modulos carregados pela API.

`GET /health`

Retorna status de saude simples com timestamp UTC.

`GET /health/ready`

Executa o health check do provider de storage ativo.

`GET /modules`

Retorna a lista de modulos e suas capacidades principais.

`GET /system/storage`

Retorna o provider de armazenamento ativo e a contagem atual de entidades em memoria/persistencia.

`GET /system/events`

Retorna a trilha de eventos internos gerados pelas operacoes integradas entre modulos, com filtros opcionais por tipo e modulo de origem.

`GET /estoque/movimentos`

Retorna o historico operacional de movimentos de estoque, com filtro opcional por produto e deposito.

`GET /empresas`

Retorna empresas com filtros opcionais por status, termo e paginacao.

`GET /catalogo/produtos`

Retorna produtos com filtros opcionais por empresa, status ativo, termo e paginacao.

`GET /clientes`

Retorna clientes com filtros opcionais por empresa, status, termo e paginacao.

`GET /depositos`

Retorna depositos com filtros opcionais por empresa, status, termo e paginacao.

`GET /identity/usuarios`

Retorna usuarios com filtros opcionais por empresa, status, termo e paginacao.

`GET /compras/importacoes-nota-entrada`

Retorna o historico das importacoes de nota de entrada, com filtros por empresa, deposito, sucesso da importacao, chave e paginacao.

`GET /vendas/pedidos`

Retorna pedidos com filtros opcionais por status, cliente e paginacao.

`GET /fiscal/notas`

Retorna notas fiscais com filtros opcionais por status, cliente e paginacao.

`GET /integracoes/webhooks`

Retorna o historico operacional de webhooks processados, com filtros por origem, status, evento e paginacao.

Exemplos de consulta:

- `GET /catalogo/produtos?ativo=true&page=1&pageSize=20&termo=SKU`
- `GET /empresas?status=Ativa&page=1&pageSize=20&termo=Empresa`
- `GET /clientes?empresaId={empresaId}&status=Ativo&page=1&pageSize=20&termo=Cliente`
- `GET /depositos?empresaId={empresaId}&status=Ativo&page=1&pageSize=20&termo=DEP`
- `GET /identity/usuarios?status=Ativo&page=1&pageSize=20&termo=usuario`
- `GET /compras/importacoes-nota-entrada?empresaId={empresaId}&depositoId={depositoId}&importadaComSucesso=true&page=1&pageSize=20`
- `GET /estoque/saldos?produtoId={produtoId}&depositoId={depositoId}&page=1&pageSize=20`
- `GET /estoque/movimentos?produtoId={produtoId}&depositoId={depositoId}&page=1&pageSize=20`
- `GET /vendas/pedidos?status=Reservado&clienteId={clienteId}&page=1&pageSize=20`
- `GET /fiscal/notas?status=Autorizada&clienteId={clienteId}&page=1&pageSize=20`
- `GET /integracoes/webhooks?origem=marketplace&status=Processado&page=1&pageSize=20`
- `GET /system/events?type=vendas.pedido_reservado&page=1&pageSize=20`

## Persistencia Local

O projeto suporta provider configuravel em `Storage`:

- `InMemory`: desenvolvimento rapido sem persistencia.
- `JsonFile`: provider ativo para desenvolvimento local com persistencia em arquivo.
- `SqlServer`: persiste o estado da aplicacao em uma tabela no SQL Server.

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
  "StateTable": "ErpState"
}
```

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
