# Feature — Grafo e publicação

- Status: **API implementada na branch, testes de integração com PostgreSQL/Neo4j passaram em CI em 09/11/2026**
- Backlog: C1–C4, H3

## Escopo

`Neo4jKnowledgeGraph` utiliza chaves de serviço combinando município + versão + serviço; a consulta recebe explicitamente município/versão e recupera apenas nós publicados dessa versão. As etapas, requisitos, órgão e canal são vinculados à fonte revisada. A API grava primeiro no grafo, depois atualiza `versaoAtivaId` no PostgreSQL. `PublicationService` usa transação, bloqueio advisory por município e a revisão como identificador idempotente.

## Cenários de aceite

- Rascunho não aprovado não publica
- Repetição da mesma publicação não duplica nem reativa versão antiga
- Falha ao gravar no Neo4j não ativa uma versão quebrada
- A versão anterior segue visível até o commit
- Outro município não lê serviço de Poá

Os cenários são cobertos em `tests/Api.Tests/IntegrationTests.cs` e dependem da passagem efetiva do workflow para serem considerados verificados.

## Limitações

Não existe transação distribuída entre os bancos: tentativa falha pode deixar versão órfã no grafo, sem visibilidade pública. Monitoramento e limpeza futura de órfãos são evolução.
