# Feature — Consulta e aplicativo do cidadão

- Status: **API pública criada; Expo ainda depende de integração do time**
- Backlog: D1–D3 e contratos de D5

## Contrato de leitura

`GET /api/v1/municipios`, `GET /api/v1/municipios/{id}/servicos` e `POST /api/v1/municipios/{id}/conversas` (sem autenticação) estão na API. O app deve salvar histórico somente no dispositivo. Resposta devolve fontes realmente guardadas no grafo; na falta de evidência informa insuficiência.

`ConversationService` restringe o grafo à cidade/versão ativa, higieniza texto transitório para CPF/e-mail/senha, aplica timeout, valida IDs que o respondedor tenta citar e retorna fontes do grafo, nunca do texto gerado.

## Modo de demonstração

`Llm__Mode=demo` ativa `DemoGroundedWiki`, um algoritmo determinístico que usa apenas texto revisado. Não é LLM real e não implementa `ExtrairAsync`. Foi pensado para os protótipos de tela que mostram status **Demonstração**. Perguntas com detalhes específicos ausentes na evidência retornam `informacaoSuficiente=false`.

## Verificação e pendências

Testes unitários do respondedor e teste HTTP/Neo4j: 14 testes automatizados passaram no CI de 09/11/2026; integração visual com Expo, LLM real e histórico SQLite permanecem com as respectivas features.

## Ajuste do contrato de integração (09/11/2026)

Quando há um único canal do serviço publicado, a API retorna `canalOficialUrl` a partir do serviço revisado no grafo, inclusive no modo demonstrativo. O teste real por HTTP com uma fonte de Poá validou esse campo, o texto fundamentado e a URL da fonte. Para o mobile, definir `EXPO_PUBLIC_API_URL` com sufixo **`/api/v1`** e `EXPO_PUBLIC_USAR_MOCK=false` quando houver backend acessível; isso **ainda não foi executado no app**.
