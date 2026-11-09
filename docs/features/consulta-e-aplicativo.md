# Feature — Consulta e aplicativo do cidadão

- Status: **API pública criada; Expo ainda depende de integração do time**
- Backlog: D1–D3 e contratos de D5

## Contrato de leitura

`GET /api/v1/municipios`, `GET /api/v1/municipios/{id}/servicos` e `POST /api/v1/municipios/{id}/conversas` (sem autenticação) estão na API. O app deve salvar histórico somente no dispositivo. Resposta devolve fontes realmente guardadas no grafo; na falta de evidência informa insuficiência.

`ConversationService` restringe o grafo à cidade/versão ativa, higieniza texto transitório para CPF/e-mail/senha, aplica timeout, valida IDs que o respondedor tenta citar e retorna fontes do grafo, nunca do texto gerado.

## Modo de demonstração

`Llm__Mode=demo` ativa `DemoGroundedWiki`, um algoritmo determinístico que usa apenas texto revisado. Não é LLM real e não implementa `ExtrairAsync`. Foi pensado para os protótipos de tela que mostram status **Demonstração**. Perguntas com detalhes específicos ausentes na evidência retornam `informacaoSuficiente=false`.

## Verificação e pendências

Testes unitários do respondedor e teste HTTP/Neo4j: 14 testes automatizados passaram no CI de 09/11/2026; integração visual com Expo, fontes reais do piloto, LLM real e histórico SQLite permanecem com as respectivas features.
