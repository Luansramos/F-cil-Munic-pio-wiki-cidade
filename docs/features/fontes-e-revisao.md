# Feature — Fontes e revisão da wiki

- Status: **API implementada na branch, painel web e LLM real pendentes**
- Backlog: B1–B8

## O que foi implementado

Admin autorizado cadastra fonte com URL HTTPS, título, texto e data; fonte pertence a um único município. Pode gerar rascunho manual de serviço, consultar e editar rascunho com `revisaoEsperada` (controle de concorrência), marcar como aprovado e publicar somente após revisão. `ExtrairAsync` está isolado por interface; sem provedor de LLM retorna indisponibilidade, não simula extração como se fosse IA real.

## Critérios e verificação

Testar login, acesso de outro município (403), validação de URL, estado não aprovado (409), atualização com revisão vencida (409), texto de fonte não publicado automaticamente. Teste de integração está em `tests/Api.Tests/IntegrationTests.cs`, dependente dos bancos reais do CI.

## Pendências

Integração do painel, escolha do provedor de LLM e teste de extração com fonte pública realmente verificada de Poá.
