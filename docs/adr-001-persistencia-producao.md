# ADR 001 - Persistencia de Producao

## Status

Aceita.

## Contexto

A aplicacao possui tres caminhos de armazenamento:

- `InMemory` para uso efemero
- `JsonFile` para desenvolvimento local
- `SqlServer` para persistencia configuravel

O backlog ainda deixava em aberto a tecnologia de persistencia definitiva para producao.

## Decisao

Para esta solucao, a tecnologia de persistencia de producao passa a ser `SqlServer`.

Motivos:

- o provider `SqlServer` ja existe no codigo
- o projeto ja possui `Microsoft.Data.SqlClient`
- a solucao ja precisava de bootstrap e migrations para esse provider
- o modelo atual de snapshot por secoes pode ser mantido como etapa intermediaria antes de uma persistencia mais granular

## Consequencias

- `appsettings.Production.json` usa `Storage.Provider = SqlServer`
- o bootstrap do banco e aplicado automaticamente pelo provider
- a tabela de migrations passa a ser `ErpMigrations`
- o ambiente local ainda pode continuar em `JsonFile` enquanto a conectividade SQL da maquina nao estiver estavel

## Proximos Passos

- estabilizar a conexao SQL local
- decidir quando sair do snapshot por secoes para armazenamento relacional mais granular
- introduzir transacao real entre modulos sobre a persistencia definitiva
