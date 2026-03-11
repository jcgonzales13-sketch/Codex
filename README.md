# Codex

Projeto de ERP modular em .NET 9 com foco em regras de dominio por modulo e cobertura com testes unitarios.

## Visao Geral

A solucao esta organizada por modulos de negocio independentes, com uma API minima para expor estado da aplicacao e capacidades disponiveis.

Modulos atuais:

- Catalogo: cadastro de produtos, variacoes e auditoria fiscal.
- Compras: importacao de nota de entrada com conciliacao de itens externos.
- Estoque: ajustes, reservas, baixas por faturamento e transferencias.
- Vendas: aprovacao e reserva de pedidos.
- Fiscal: autorizacao, rejeicao e cancelamento de notas fiscais.
- Identity: cadastro de usuarios, ativacao, bloqueio e permissoes.
- Integracoes: processamento idempotente de webhooks.

## Estrutura

```text
src/
  ERP.Api
  ERP.BuildingBlocks
  ERP.Modules.Catalogo
  ERP.Modules.Compras
  ERP.Modules.Estoque
  ERP.Modules.Fiscal
  ERP.Modules.Identity
  ERP.Modules.Integracoes
  ERP.Modules.Vendas
tests/
  ERP.Modules.Catalogo.UnitTests
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
- `GET /modules`

## Como Rodar os Testes

Todos os testes:

```powershell
dotnet test ERP.sln
```

Somente um modulo:

```powershell
dotnet test .\tests\ERP.Modules.Estoque.UnitTests\ERP.Modules.Estoque.UnitTests.csproj
```

Observacao: no ambiente onde este repositorio foi preparado, o `dotnet test` chegou a falhar na etapa do runner/MSBuild sem erros de compilacao de codigo. Se isso acontecer na sua maquina, vale validar primeiro com `dotnet restore`, `dotnet build` e depois repetir o `dotnet test`.

## Endpoints da API

`GET /`

Retorna o estado online da aplicacao e os modulos carregados pela API.

`GET /health`

Retorna status de saude simples com timestamp UTC.

`GET /modules`

Retorna a lista de modulos e suas capacidades principais.

## Objetivo do Projeto

Este repositorio serve como base de estudo e evolucao para:

- modelagem de dominio por contexto funcional;
- testes unitarios por modulo;
- organizacao de uma solucao .NET modular;
- exposicao inicial de capacidades por uma API leve.

## Proximos Passos Sugeridos

- adicionar persistencia real por modulo;
- expor endpoints de negocio alem dos endpoints de diagnostico;
- configurar CI para `build` e `test`;
- melhorar observabilidade e tratamento de erros na API.
