# Consulta de serviços e integração com o aplicativo

**Status:** endpoints públicos implementados; ligação com a tela de conversa do Expo pendente.

**Backlog:** D1–D3 e contrato da D5.

## O que a API oferece

O aplicativo pode listar municípios, consultar os serviços publicados e enviar perguntas pelas rotas:

- `GET /api/v1/municipios`
- `GET /api/v1/municipios/{id}/servicos`
- `POST /api/v1/municipios/{id}/conversas`

A resposta inclui o texto, os serviços relacionados, as fontes e `informacaoSuficiente`. Se o serviço tiver um canal cadastrado, a API também informa `canalOficialUrl`.

A consulta recupera somente serviços da versão ativa do município escolhido. O texto enviado é tratado para não repassar alguns padrões de CPF, e-mail e senha ao respondedor. A API não grava perguntas nem histórico.

## Demonstração e testes

Em `Llm__Mode=azure`, a API envia a pergunta e o histórico sanitizados junto com os serviços publicados da cidade e versão ativa. O modelo só pode responder com esses dados; a API valida as citações de evidência e os IDs dos serviços antes de retornar a resposta. Fontes e canais continuam vindo do backend. Se a evidência faltar, a API responde com insuficiência. `Llm__Mode=demo` continua disponível para consultar deterministicamente conteúdo revisado sem chamar um provedor externo.

O adaptador e o comando de smoke do SDK estão descritos em [LLM Wiki e Azure OpenAI](llm-wiki.md). O smoke sintético não substitui o teste com as fontes reais de Poá nem a integração com a tela Expo.

O [workflow de 10/11/2026](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42902618547) passou com 41 testes e com o roteiro HTTP que retorna uma orientação sobre Cata Treco acompanhada da fonte. O roteiro também pergunta sobre atendimento aos sábados, que não está confirmado nas informações usadas.

## Para conectar com o Expo

O cliente do mobile monta caminhos como `/municipios`. Por isso, quando ele estiver apontando para a API real, a variável `EXPO_PUBLIC_API_URL` deve terminar em `/api/v1`, e `EXPO_PUBLIC_USAR_MOCK` deve estar em `false`.

O contrato foi conferido com `types/guardas.ts` e `services/api/apiReal.ts` na branch `feat/mobile-scaffold-theme`. O modo demonstrativo agora usa até três trocas de histórico para interpretar perguntas curtas de seguimento, mas só responde se a informação estiver publicada no município atual. A ausência de `null`, o CORS do Expo Web e os limites de entrada têm testes HTTP.

O app recebe respostas em texto simples, omite campos opcionais ausentes e não precisa de login. As decisões sobre histórico, canal oficial e limites ainda devem ser confirmadas com P1/P2/P3, conforme [contrato compartilhado](../contratos-de-integracao.md).

A integração precisa ser feita e testada pelo responsável pelo aplicativo. O funcionamento do endpoint no CI não comprova que a tela Expo já esteja consumindo a API.
