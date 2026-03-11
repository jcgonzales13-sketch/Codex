# Backlog Tecnico

## Fase 1 - MVP Funcional

- [x] Remover template `WeatherForecast` da API.
- [x] Criar endpoints iniciais de negocio para Catalogo, Identity, Estoque, Vendas, Compras, Fiscal e Integracoes.
- [x] Adicionar cadastro operacional basico de clientes.
- [x] Adicionar cadastro operacional basico de depositos.
- [x] Adicionar cadastro operacional basico de empresas.
- [x] Adicionar cadastro operacional basico de fornecedores.
- [x] Adicionar tratamento centralizado de excecoes de dominio e recurso nao encontrado.
- [x] Disponibilizar armazenamento em memoria para uso imediato da API.
- [ ] Adicionar exemplos de requisicao e resposta por modulo.
- [ ] Cobrir a API com testes de integracao.

## Fase 2 - Camada de Aplicacao

- [x] Criar servico de aplicacao minimo para orquestrar os modulos.
- [x] Separar comandos, queries e DTOs por contexto.
- [x] Introduzir validacao de entrada dedicada.
- [x] Padronizar respostas HTTP e contratos de erro.

## Fase 3 - Persistencia

- [x] Introduzir provider de armazenamento configuravel (`InMemory` e `JsonFile`).
- [x] Introduzir provider `SqlServer` para persistencia local/configuravel.
- [ ] Escolher tecnologia de persistencia definitiva para producao.
- [ ] Criar modelos de armazenamento por modulo.
- [ ] Implementar repositórios concretos.
- [ ] Adicionar migrations e bootstrap inicial do banco.
- [ ] Remover dependencia do armazenamento em memoria para cenarios principais.
- [x] Introduzir abstracao de armazenamento para desacoplar a aplicacao da implementacao em memoria.
- [ ] Estabilizar a conectividade local da instancia SQL Server da maquina e o handshake de criptografia do cliente.

## Fase 4 - Fluxos Integrados

- [x] Encadear pedido aprovado -> reserva de estoque -> emissao de nota -> baixa de estoque -> faturamento do pedido.
- [x] Encadear compras -> importacao de nota -> conciliacao -> entrada em estoque.
- [x] Garantir idempotencia basica para repeticao segura de reserva, cancelamento e autorizacao de nota.
- [x] Adicionar rollback logico de aplicacao para evitar persistencia parcial em fluxos integrados.
- [ ] Garantir consistencia transacional entre modulos com infraestrutura de persistencia definitiva.
- [x] Adicionar eventos de integracao internos.
- [x] Expor consulta operacional para importacoes de compras e webhooks processados.

## Fase 5 - Seguranca

- [ ] Implementar autenticacao.
- [ ] Implementar autorizacao por perfis/permissoes.
- [ ] Associar operacoes ao contexto da empresa.
- [ ] Adicionar protecao para webhooks.

## Fase 6 - Qualidade Operacional

- [ ] Adicionar CI para build e testes.
- [ ] Ao montar o pipeline, lembrar de validar o comportamento do SDK com `MSBuildEnableWorkloadResolver=false` se o agente repetir o problema de workload resolver visto no ambiente local.
- [ ] Adicionar logging estruturado.
- [ ] Adicionar observabilidade, metricas e tracing.
- [x] Adicionar health checks reais para o provider de storage.
- [x] Expor historico operacional de movimentos de estoque.
- [x] Adicionar filtros e paginacao nas consultas principais da API.
- [ ] Documentar deploy e execucao por ambiente.
