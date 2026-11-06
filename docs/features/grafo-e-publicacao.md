# Feature — Grafo e publicação

- Status: planejada
- Responsáveis sugeridos: P1 e P3
- Backlog: C1–C6

## Objetivo e integração

Publicar serviços revisados em grafo com município, versão, entidades, relações e fontes. PostgreSQL controla versões administrativas e versão ativa; Neo4j contém conhecimento publicado. A gravação é repetível com IDs estáveis.

## Critérios de aceite

- Serviço e fonte são recuperáveis no grafo.
- Consultas filtram município e versão ativa.
- Falha preserva versão anterior; repetição não duplica conteúdo.

## Verificações e pendências

Não implementado nem verificado. Documentar recuperação após falha entre bancos e seus resultados.
