# Entrega de Cybersecurity — documentação técnica (fonte para o PDF)

Disciplina: Cybersecurity (Global Solutions). Projeto: Fácil Município — Wiki Cidade. Branch: `feature/cybersecurity` (base `origin/feat/soa-csharp-api`).

Este arquivo é a **fonte para converter em PDF**. O PDF não foi gerado neste ambiente (ver [Pendências](#pendências-para-a-entrega)). Entrega ao professor: **um PDF** com esta documentação e as capturas **separado** do código-fonte (.zip ou link do GitHub), mais o vídeo do dashboard. Não misturar os dois.

> Todos os dados são simulados. Nenhuma chamada a provedor de IA real foi executada; nenhuma captura de tela ou vídeo foi produzida.

## 1. Relação dos requisitos com o código

| Requisito da GS | Onde está | Evidência automatizada |
|---|---|---|
| 1. Parsing de PII | `Security/PrivacyFilter.cs` (`Analyze` → `PiiScan`: texto sanitizado, categorias, caminho, ação, marcas em memória) | `SecurityTests`: `DetectsCategoriesAndFormats`, `NormalQuestionsAreNotFlagged`, `KnownLimitations_…`, `ScanObjectsDoNotLeakOriginalsThroughToString` |
| 2. Taint analysis | `Security/SinkGuard.cs` (`SanitizeAsync`, `VerifyAsync`, `RequireCleanAsync`, `Log`); uso em `ConversationService`, `AdminController`, `PublicationService` | `Conversation_TaintPropagatesAcrossFields`, `SinkGuard_BlocksTaintedValueWithoutTreatment_…`, `SinkGuard_LogIsBlocked…`, `Conversation_BlocksPiiGeneratedByTheLlm…`, `Logs_NeverContainOriginalValues_…`, `AdminRejectsPersonalData…` |
| 3. Tradução via IA | `Services/RiskTranslation.cs` (`IRiskTranslator`, `OpenAiCompatibleRiskTranslator`, `RiskTranslationProcessor`, `RiskTranslationWorker`, `RiskRules`) | `Translation_*` (provedor HTTP **simulado**) |
| 4. Dashboard / Risco Cidadão | `wwwroot/auditoria/index.html`, `Controllers/SecurityController.cs`, `RiskRules.Indicador` | `DashboardApi_*`, `RiskIndicator_*`, `DashboardPage_*` |
| Auditoria (entregável 2 do PDF do professor) | `Domain/EventoSeguranca.cs`, migração `EventosSeguranca` | `Conversation_Sanitizes…AuditsOnlyMetadata`, `Retention_*` |

Mapeamento para os três entregáveis do professor: **Source** = este repositório; **Auditoria** = tabela `EventosSeguranca` + seção 4 + testes; **Vídeo do Dashboard** = roteiro da seção 6 (pendente de gravação).

## 2. Fluxo source → tratamento → sink

```mermaid
flowchart LR
  subgraph Source
    Q[Pergunta e histórico\nconversa.*]
    F[Fonte: título/texto/URL\nfonte.*]
    D[Rascunho: campos textuais\nrascunho.*]
  end
  Q --> A1[PrivacyFilter.Analyze\nmarca valores em memória]
  F --> A1
  D --> A1
  A1 -->|Conversa: sanitiza e propaga marcas| T1[texto seguro]
  A1 -->|Fonte/rascunho: rejeita 422| X1[(não persiste)]
  T1 --> S1{{Sink LLM de conhecimento\nVerifyAsync}}
  S1 -->|ok| LLM[ILlmWiki]
  LLM --> S2{{Sink resposta pública\nresposta, serviços, fontes, canal}}
  S2 -->|ok| R[Resposta ao cidadão]
  S2 -->|PII da LLM ou valor marcado| B[Resposta segura\ninformacaoSuficiente=false]
  D -->|limpo| S3{{Sink persistência e grafo\nRequireCleanAsync}}
  S3 --> PG[(PostgreSQL / Neo4j)]
  A1 -. metadados .-> AUD[(EventosSeguranca)]
  S1 -. bloqueio .-> AUD
  S2 -. bloqueio .-> AUD
  S3 -. rejeição .-> AUD
  AUD --> W[Worker: 1 consumidor, lote 5, 3 tentativas]
  W --> IA[IRiskTranslator\nprompt só com metadados]
  IA --> W --> AUD
  AUD --> API[GET .../seguranca/painel\nJWT + município]
  API --> UI[/auditoria/\nRisco Cidadão]
```

Cada seta tracejada grava apenas metadados. Logs do módulo passam por `SinkGuard.Log`, que bloqueia a linha se ela contiver PII ou valor marcado.

## 3. Políticas e limitações

Resumidas em [seguranca.md](features/seguranca.md) (políticas por finalidade, escopo real da taint analysis, falsos positivos/negativos, retenção, regra do indicador). Pontos a repetir no PDF:

- Cobertura apenas dos caminhos instrumentados; **não** há proteção automática de todo `ILogger` nem da aplicação inteira.
- Regex e contexto não detectam qualquer dado pessoal possível.
- Proteger a API **não** remove a pergunta que o app Expo pode salvar localmente no SQLite; persistência mobile não foi alterada.
- Falha de auditoria não libera dado, mas perde o evento (sem fila).

## 4. Auditoria

Colunas e restrições em [seguranca.md](features/seguranca.md#auditoria-postgresql). Consulta sugerida para a captura (somente metadados):

```sql
SELECT "OcorridoEm", "Categoria", "Origem", "Destino", "Acao", "Gravidade", "Codigo", "TraducaoEstado"
FROM "EventosSeguranca" ORDER BY "OcorridoEm" DESC LIMIT 20;
```

## 5. Comandos e resultados reais

Ambiente: Windows 10, .NET SDK 10.0.404, **sem Docker** (PostgreSQL e Neo4j indisponíveis).

| Comando | Resultado |
|---|---|
| `dotnet test tests/Api.Tests` na base, antes das mudanças | 41 testes: 40 aprovados, 1 falha (`IntegrationTests`, exige `Jwt__Key`, PostgreSQL e Neo4j) |
| `dotnet test tests/Api.Tests` sem bancos, após os ajustes da revisão | 96 testes: 95 aprovados, 1 falha (`IntegrationTests`, sem PostgreSQL/Neo4j no ar) |
| `dotnet test tests/Api.Tests` com PostgreSQL 17.6 e Neo4j 5.26 locais (binários portáteis) | **96 testes, 96 aprovados**, incluindo `IntegrationTests` real e o trecho que bloqueia publicação de rascunho com e-mail |
| API real + `scripts/demo_fluxo_http.py` + consulta com CPF e saúde | `DEMO HTTP OK`; painel retornou 2 eventos sanitizados e indicador AMARELO (verificado por API, **não** no navegador) |
| `dotnet ef migrations has-pending-model-changes --project apps/api` (dotnet-ef 10.0.4, instalado fora do PATH) | “No changes have been made to the model since the last migration.” |

Os testes novos (55, incluindo casos de `Theory`) usam `WebApplicationFactory` com EF **InMemory**, grafo e LLM de conhecimento simulados e HTTP do tradutor simulado. Isso **não** substitui a execução com PostgreSQL/Neo4j: transações, `pg_advisory_xact_lock` e a publicação real continuam cobertos apenas pelo teste de integração (CI). Foi acrescentado a ele um trecho que tenta publicar um rascunho com e-mail e espera `ApiException`; esse trecho foi executado contra os bancos reais locais e passou.

Para repetir com bancos (README da API): `docker compose up -d`, variáveis de ambiente, `dotnet run --project apps/api -- --migrate`, `dotnet test tests/Api.Tests -c Release`.

Automatizado × manual: tudo da seção 5 é automatizado. São **manuais e ainda não feitos**: abrir `/auditoria/` num navegador, login, leitura do painel, chamada real ao provedor de IA, gravação do vídeo e capturas.

## 6. Cenários para capturas e roteiro do vídeo

Preparação (manual): bancos no ar, `--migrate`, `--bootstrap`, `Llm__Mode=demo` (modo demonstração, **não é LLM**; apenas repete texto revisado), um município com um serviço publicado (ex.: roteiro `scripts/demo_fluxo_http.py`). Para a cena de IA, definir `RiskTranslator__BaseUrl`, `__Model` e `__ApiKey` no ambiente do terminal (nunca no chat nem no repositório). Usar somente dados simulados.

| # | Captura | Como produzir | Esperado |
|---|---|---|---|
| C1 | Parsing | `POST /api/v1/municipios/{id}/conversas` com `{"pergunta":"Meu CPF é 123.456.789-00 e tenho diabetes"}` | 200; evento `Sanitizado` com categorias Cpf e Saude |
| C2 | Pergunta normal | `{"pergunta":"Como alterar minha senha?"}` | resposta normal, nenhum evento novo |
| C3 | Bloqueio na entrada de fonte | `POST /municipios/{id}/fontes` com texto contendo CPF simulado | 422 com mensagem segura; nada gravado em `Fontes` |
| C4 | Código-fonte | Trechos de `PrivacyFilter.Analyze`, `SinkGuard.VerifyAsync`, `ConversationService` | — |
| C5 | Teste de bloqueio de sink | `dotnet test --filter "FullyQualifiedName~Conversation_BlocksPiiGeneratedByTheLlm"` | testes aprovados (LLM simulada gera CPF; resposta segura) |
| C6 | Auditoria | Consulta SQL da seção 4 | só metadados, sem valores |
| C7 | Tradução | Painel após o worker rodar (≥ 10 s) | explicação “gerada por IA” **somente se** o provedor real foi executado; senão, mensagem padrão rotulada |
| C8 | Painel | `/auditoria/` logado como ADMIN | indicador, contagens, tabela |
| C9 | Isolamento | Token ADMIN do município A pedindo o município B | 403 |

**Roteiro do vídeo (≈ 3–4 min):** (1) mostrar o painel vazio com o aviso “não é garantia de segurança”; (2) enviar a pergunta normal (C2) e a com CPF + saúde (C1); (3) tentar cadastrar fonte com CPF (C3); (4) atualizar o painel: contagens, indicador AMARELO, lista com horário/categoria/origem/destino/ação; (5) mostrar a explicação — se houver provedor real, a legenda “Explicação gerada por IA”; se não houver, dizer explicitamente que é a mensagem padrão; (6) mostrar a tabela de auditoria sem valores originais; (7) citar os limites (caminhos instrumentados, SQLite do app).

## 7. Validação com IA real

**Não executada.** Não havia credencial de provedor. O adaptador `OpenAiCompatibleRiskTranslator` e o prompt foram testados somente com um `HttpMessageHandler` simulado (formato da requisição, ausência de valores originais no prompt, validação e tratamento de falhas). Formato da resposta, nome do modelo e comportamento reais do provedor estão **pendentes de teste**.

## Pendências para a entrega

1. (Feito localmente) PostgreSQL/Neo4j, `--migrate` e `dotnet test` completo. Repetir no CI.
2. Configurar um provedor de IA por variáveis de ambiente, confirmar o modelo na documentação oficial e executar C7 de verdade; só então registrar a validação.
3. Abrir o painel no navegador e fazer as capturas C1–C9 (sem dados reais).
4. Gravar o vídeo do dashboard.
5. Converter este documento em PDF **com as capturas reais** (por exemplo, exportar de um editor) e entregar separado do código (.zip ou link do GitHub) na tarefa do Teams.
6. Revisão por outro integrante antes do merge; nenhum commit, push, PR ou deploy foi feito.
7. Decisão da frente mobile sobre o histórico local no SQLite.
