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

Enquanto não há LLM real, `Llm__Mode=demo` permite consultar o conteúdo publicado por uma rotina determinística. Ela usa apenas a descrição, as etapas e os requisitos revisados. Se a fonte não sustentar um detalhe da pergunta, a API informa que não há dados suficientes.

O [workflow de 09/11/2026](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42721825544) passou com 37 testes e com o roteiro HTTP que retorna uma orientação sobre Cata Treco acompanhada da fonte. O roteiro também pergunta sobre atendimento aos sábados, que não está confirmado nas informações usadas.

## Para conectar com o Expo

O cliente do mobile monta caminhos como `/municipios`. Por isso, quando ele estiver apontando para a API real, a variável `EXPO_PUBLIC_API_URL` deve terminar em `/api/v1`, e `EXPO_PUBLIC_USAR_MOCK` deve estar em `false`.

A integração precisa ser feita e testada pelo responsável pelo aplicativo. O funcionamento do endpoint no CI não comprova que a tela Expo já esteja consumindo a API.
