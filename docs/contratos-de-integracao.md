# Contratos de integração — Fácil Município

**Estado:** API e adaptador Azure OpenAI implementados; `gpt-5-mini` respondeu a uma chamada HTTP com JSON estruturado pelo endpoint OpenAI v1. O smoke pelo SDK .NET e a integração ponta a ponta com Expo/React ainda precisam de verificação. O prefixo implementado é **`/api/v1`**, ao contrário do rascunho inicial sem prefixo.

## Fronteiras

- **Painel → API:** JWT somente para administradores; operador pode configurar municípios e usuários. Todo acesso administrativo restringe `municipioId` no backend.
- **App → API:** três rotas públicas; municípios e serviços publicados; perguntas sem login. Não enviar credencial para o app.
- **API → PostgreSQL (EF Core):** municípios, usuários, fontes, rascunhos e versões ativas. Migração inicial versionada.
- **API → Neo4j:** serviço, fonte, etapas, requisitos, órgão e canal com chaves contendo município e versão.
- **API → LLM:** `ILlmWiki` usa Azure OpenAI via SDK oficial `OpenAI` para .NET e API v1. O deployment configurado é `gpt-5-mini`; `LLM_AZURE_DEPLOYMENT` deve corresponder ao nome ativo no Azure. `Llm__Mode=demo` mantém o respondedor determinístico sem chamadas externas.

## Endpoints implementados

| Método | Caminho | Autenticação |
|---|---|---|
| POST | `/api/v1/auth/login` | pública (login administrativo) |
| GET | `/api/v1/municipios` | pública |
| POST | `/api/v1/municipios` | OPERADOR |
| POST | `/api/v1/usuarios` | OPERADOR |
| POST | `/api/v1/municipios/{id}/fontes` | ADMIN do município ou OPERADOR |
| GET | `/api/v1/municipios/{id}/fontes` | ADMIN do município ou OPERADOR |
| POST | `/api/v1/fontes/{id}/extracoes` | ADMIN / OPERADOR; cria rascunho sugerido pela LLM para revisão (requer deployment ativo) |
| POST | `/api/v1/fontes/{id}/rascunhos` | ADMIN / OPERADOR; criação manual |
| GET | `/api/v1/rascunhos/{id}` | ADMIN / OPERADOR |
| PUT | `/api/v1/rascunhos/{id}` | ADMIN / OPERADOR |
| POST | `/api/v1/rascunhos/{id}/publicacao` | ADMIN / OPERADOR |
| GET | `/api/v1/municipios/{id}/versoes` | ADMIN / OPERADOR |
| GET | `/api/v1/municipios/{id}/servicos` | pública |
| POST | `/api/v1/municipios/{id}/conversas` | pública |
| GET | `/swagger` e `/swagger/v1/swagger.json` | documentação |
| GET | `/health` | pública (liveness, não readiness de banco) |

## Contrato mobile

**GET** `/api/v1/municipios`: lista `{id, nome, uf}`.

**GET** `/api/v1/municipios/{id}/servicos`: lista de `{id,nome}`. Cidade sem publicação retorna `[]`; cidade inexistente retorna 404.

**POST** `/api/v1/municipios/{id}/conversas`:

```json
{
  "pergunta": "Como solicito a retirada de um sofá?",
  "historico": []
}
```

`pergunta` é obrigatória (1–500 caracteres); `historico` é opcional (até 3 trocas com `pergunta` e `resposta` limitadas a 1000 caracteres cada). JSON camelCase. Corpo inválido retorna 422, limite de taxa 429, indisponibilidade de grafo/LLM 503.

Resposta fundamentada (estrutura; os valores dependem do conhecimento efetivamente publicado):

```json
{
  "resposta": "Texto baseado no conteúdo revisado.",
  "servicosRelacionados": ["id-do-servico"],
  "fontes": [{"titulo":"Fonte revisada","url":"https://example.org/exemplo","dataConsulta":"2026-11-01"}],
  "informacaoSuficiente": true
}
```

Resposta insuficiente tem `informacaoSuficiente:false`, `servicosRelacionados:[]`, `fontes:[]`, uma orientação genérica sem inventar fatos e `canalOficialUrl` apenas quando cadastrado. A URL do exemplo acima **não** representa uma fonte oficial de Poá.

## Publicação e segurança

Fonte cadastrada não é publicada. Rascunho registra revisão e aprovação; a publicação usa `revisaoId` como versão imutável. PostgreSQL controla `versaoAtivaId`; Neo4j recebe uma cópia versionada das entidades. Uma falha antes do commit mantém a versão anterior; nova tentativa é idempotente. O endpoint público lê exclusivamente `municipioId` + versão ativa. A API não salva perguntas, históricos ou logs de conteúdo sensível; histórico é responsabilidade local do Expo. Nunca associar resultados ou fontes de outro município à resposta.

**Pendências para integração:** alinhar o endpoint e o deployment Azure no ambiente de execução deste repositório (o `.env` local não foi validado), extrair uma fonte oficial de Poá e fazer a revisão humana pela API, validar resposta insuficiente e isolamento municipal com bancos reais, construir painel e Expo, cadastrar os links oficiais e executar o fluxo ponta a ponta. A chamada sintética e os testes unitários não comprovam integração visual nem qualidade editorial das fontes do piloto.

## Auditoria de privacidade (Cybersecurity)

Contrato novo; **não altera** `POST /municipios/{id}/conversas` quanto a campos. Mudanças de comportamento:

- A pergunta e cada pergunta/resposta do histórico têm CPF, RG, e-mail, senha e informação pessoal de saúde substituídos por `[DADO REMOVIDO]` antes de chegar à LLM (o app não precisa mudar).
- Se a resposta da LLM contiver PII ou valor marcado, a API devolve 200 com `informacaoSuficiente:false`, listas vazias e a mensagem “Não foi possível exibir esta resposta com segurança…”; `canalOficialUrl` só quando cadastrado.
- Cadastro de fonte, criação/revisão de rascunho e publicação respondem **422** (`ProblemDetails.title` com mensagem segura, sem valores) quando o conteúdo parece conter dados pessoais.
- A API grava somente eventos de metadados; continua sem salvar pergunta ou histórico.

### `GET /api/v1/municipios/{id}/seguranca/painel?horas=24`

Requer JWT (`ADMIN` do próprio município ou `OPERADOR`). `horas`: 1 a 168 (padrão 24). Erros: 401 sem token; 403 outro município; 404 município inexistente (para quem tem acesso); 422 período inválido.

```json
{
  "periodoHoras": 24,
  "geradoEm": "2026-11-16T22:00:00+00:00",
  "indicador": { "nivel": "VERDE|AMARELO|VERMELHO", "rotulo": "Monitorar", "descricao": "…" },
  "resumo": { "total": 2, "sanitizados": 1, "bloqueados": 1, "pendentes": 0, "traducoesPendentes": 1 },
  "eventos": [{
    "id": "guid", "ocorridoEm": "…", "categoria": "Cpf|Rg|Email|Senha|Saude",
    "origem": "conversa.historico[0].resposta", "destino": "Llm|RespostaPublica|Persistencia|Grafo",
    "acao": "Sanitizado|Bloqueado|Pendente", "gravidade": "Baixa|Media|Alta", "codigo": "PII_SANITIZADA",
    "traducaoEstado": "Pendente|Traduzida|Falhou",
    "explicacao": "…", "origemExplicacao": "IA|PADRAO", "modeloIa": "modelo configurado (só quando origemExplicacao = IA)"
  }]
}
```

`origemExplicacao = PADRAO` indica mensagem padrão do sistema, não gerada por IA. Até 50 eventos, do mais recente ao mais antigo. A página `/auditoria/` é estática e consome somente este endpoint e `/auth/login`. Regra do indicador e retenção: [seguranca.md](features/seguranca.md).

## Conferência com a branch mobile

Referência conferida: `feat/mobile-scaffold-theme`, nos arquivos
`apps/mobile/types/contrato.ts`, `types/guardas.ts`,
`services/api/apiReal.ts`, `services/api/mock/responder.ts` e
`hooks/useConversa.ts`.

**Estado da combinação:** esta seção documenta o contrato implementado na API e
testado separadamente do cliente mobile. Os itens propostos por P2 (histórico,
canal oficial e limites) ainda precisam do acordo de P1/P3 antes do merge.
O aplicativo ainda não foi executado contra um servidor real.

### Formato de dados esperado pelo aplicativo

- `GET /api/v1/municipios`: `[{ "id": "poa", "nome": "Poá", "uf": "SP" }]`.
  A API lista **todos os municípios cadastrados**, inclusive os que ainda não
  têm publicação. Nesses casos, a consulta de serviços retorna `[]`.
- `GET /api/v1/municipios/{id}/servicos`: `[{ "id": "cata-treco", "nome": "Cata Treco" }]`.
  São somente serviços da versão ativa daquele município.
- `POST /api/v1/municipios/{id}/conversas`: corpo `{"pergunta":"E no domingo?","historico":[{"pergunta":"Como solicito a retirada de um sofá?","resposta":"Orientação anterior."}]}`.
  O histórico é opcional, com até **3 trocas**, e não é persistido na API.

Para os três endpoints: **sem autenticação**, JSON camelCase, UTF-8 e idioma
pt-BR nas mensagens. O JWT é exclusivo das funções administrativas.
Não enviar IDs de usuário ou dispositivo.

A API não envia campos opcionais com valor `null`: o ASP.NET Core usa
`JsonIgnoreCondition.WhenWritingNull`. O mobile rejeita `null` em
`fontes[].dataConsulta` e `canalOficialUrl`; esses campos são omitidos
quando não disponíveis. `fontes[].titulo` e `fontes[].url` são sempre strings.
Ao cadastrar uma fonte real, a API exige uma URL HTTPS.
O padrão escolhido para `dataConsulta` é `YYYY-MM-DD`.

Exemplo de resposta suficiente (valores ilustrativos):

```json
{
  "resposta": "Cata Treco: orientações para retirada de móveis.\n\nEtapas:\n1. Consulte o canal oficial.",
  "servicosRelacionados": ["cata-treco"],
  "fontes": [
    {"titulo": "Fonte de exemplo", "url": "https://example.org/servico", "dataConsulta": "2026-11-10"}
  ],
  "informacaoSuficiente": true
}
```

A resposta é **texto puro**. Em listas, são usados números e `\\n`, sem
formatação Markdown. Todos os IDs em `servicosRelacionados` têm de estar na
lista de serviços publicados do **mesmo município**. Os IDs previstos pelo
aplicativo são `coleta-de-lixo`, `iluminacao-publica` e `cata-treco`.
A API aceita IDs estáveis definidos na revisão, mas o responsável pelo
cadastro deve usar essas grafias para manter os ícones existentes.

Quando faltar evidência, a API deve responder HTTP 200:

```json
{
  "resposta": "Não encontrei informação suficiente na base publicada deste município. Consulte o canal oficial para confirmar a orientação.",
  "servicosRelacionados": [],
  "fontes": [],
  "informacaoSuficiente": false
}
```

Se houver URL oficial cadastrada, `canalOficialUrl` pode aparecer como
string HTTPS (nunca `null`). O botão do aplicativo pode ficar oculto
quando o campo não vier.

### Histórico e pergunta de seguimento

A API filtra CPF, e-mail e alguns padrões de senha da pergunta e dos
dois campos de cada troca de histórico, antes do respondedor. Não
armazena perguntas nem histórico. A seleção de conhecimento considera
somente o município/versão da **URL atual**, ainda que o usuário envie
conteúdo de outro município no histórico.

No modo `demo` sem LLM, perguntas curtas como “E no domingo?” podem
usar o último serviço identificado no histórico para escolher o
contexto. Mesmo assim, a resposta só é considerada suficiente se
domingo estiver documentado no serviço revisado. Sem evidência, é
retornada a resposta de insuficiência. Perguntas com assunto novo
e explícito não herdam automaticamente o serviço anterior.

Não existe sessão ou identificador de conversa no servidor; portanto,
a API não consegue provar que o histórico enviado foi gerado naquele
município. `useConversa` deve enviar apenas as últimas trocas da
conversa local pertencente à cidade escolhida. Histórico não é
autorização nem fonte de fatos.

### Limites e erros

| Situação | Comportamento |
|---|---|
| Pergunta vazia ou acima de 500 caracteres | HTTP 422 |
| Mais de 3 trocas de histórico | HTTP 422 |
| Pergunta/resposta do histórico acima de 1000 caracteres | HTTP 422 |
| Município inexistente | HTTP 404 |
| Município cadastrado mas sem publicação | HTTP 200 com informação insuficiente |
| Limite de consultas | HTTP 429 |
| Provedor indisponível ou grafo com falha | HTTP 503 |
| Consulta suficiente e fundamentada | HTTP 200 |

Há limitação de **10 perguntas por minuto por IP** e **5 tentativas de login por
minuto por IP**. Não há contagem por aparelho.
O limite interno da conversa é de **12 segundos**, abaixo dos
**15 segundos** de timeout do app; não há streaming ou retry automático.

### Ambiente e integração pendente

Para o mobile usar esta API, definir no `apps/mobile/.env`:

```text
EXPO_PUBLIC_USAR_MOCK=false
EXPO_PUBLIC_API_URL=http://localhost:5055/api/v1
```

O exemplo é para **Expo Web na mesma máquina** (CORS permite
`http://localhost:8081`). No celular físico, substituir `localhost`
pelo IP de rede do computador; no emulador Android, em geral
`10.0.2.2`. Verificar acesso HTTP e configuração do Expo no dispositivo
antes de afirmar que funciona. O mobile não faz retry automático.

**Verificação feita na API:** teste HTTP do CORS, serialização compatível
com `guardas.ts`, limites de entrada, isolamento municipal e seguimento
contextual no respondedor de demonstração. O adaptador Azure compila, e uma
chamada HTTP direta com `gpt-5-mini` e JSON Schema retornou HTTP 200 em
17/11/2026. Em 17/11/2026, 109 testes unitários sem integrações dependentes
de bancos passaram no WSL; os testes do SDK usam transporte HTTP simulado. Em
18/11/2026, o smoke pelo SDK terminou com código 0 usando o endpoint e
deployment validados pelo usuário, passados somente por variáveis de ambiente
do processo. O comando validou extração estruturada e resposta com evidência
sintética; nenhuma chave ou resposta foi registrada. A tentativa anterior com
o `.env` deste repositório falhou com `DeploymentNotFound`; esse arquivo
permaneceu inalterado e não foi validado. **Ainda pendente:** validar pela API
uma fonte oficial de Poá e a revisão humana, resposta insuficiente e
isolamento com bancos reais, testar o `apiReal.ts` de ponta a ponta com a tela
Expo e validar o acordo P1/P2/P3 para os campos opcionais.
