# Feature — Segurança e Cybersecurity

- Status: implementada em `feature/cybersecurity` (base `feat/soa-csharp-api`), **não integrada**; testes automatizados executados em memória; **IA real não validada**; PDF, capturas e vídeo pendentes
- Responsável sugerido: P4; apoio P1 e P3
- Backlog: F1–F6
- Documentação da entrega (fluxo, cenários, roteiro de vídeo): [entrega-cybersecurity.md](../entrega-cybersecurity.md)

## Objetivo e critérios de aceite

Proteger as fronteiras do produto e atender à atividade: parsing de PII, taint analysis, tradução de eventos por IA generativa e indicador “Risco Cidadão”.

- A API aplica acesso administrativo e separação por município (JWT existente; o painel segue a mesma regra).
- PII é detectada, sanitizada ou rejeitada conforme a **finalidade** do dado.
- Dado marcado não chega a LLM, resposta pública, persistência nem grafo sem tratamento nas rotas cobertas.
- Eventos ficam no PostgreSQL somente com metadados; a IA explica o evento, a regra de código classifica o risco.

## O que foi implementado

| Parte | Código |
|---|---|
| Parsing de PII (resultado estruturado: texto sanitizado, categorias, caminho, ação, marcas em memória) | `apps/api/Security/PrivacyFilter.cs` |
| Taint tracking e camada de sinks (`SanitizeAsync`, `VerifyAsync`, `RequireCleanAsync`, `Log`) | `apps/api/Security/SinkGuard.cs` |
| Auditoria no PostgreSQL | `Domain/EventoSeguranca.cs`, `Data/Migrations/*EventosSeguranca*` |
| Tradução por IA, regras de risco, worker, retenção | `apps/api/Services/RiskTranslation.cs` |
| API do painel | `apps/api/Controllers/SecurityController.cs` |
| Painel (HTML/CSS/JS estático) | `apps/api/wwwroot/auditoria/index.html` → `/auditoria/` |
| Pontos instrumentados | `ConversationService`, `AdminController`, `PublicationService`, `Program.cs` |

## Políticas por finalidade

| Finalidade | Política | Resultado |
|---|---|---|
| Conversa (pergunta e cada pergunta/resposta do histórico) | Remover PII e continuar | Texto com `[DADO REMOVIDO]`; evento `Sanitizado`, destino `Llm` |
| Resposta da LLM / resposta pública (resposta, serviços, fontes, canal) | Bloquear | Resposta segura (`informacaoSuficiente:false`); evento `Bloqueado`, destino `RespostaPublica` |
| Fontes (título, texto, URL) e rascunhos (todos os campos textuais) | Rejeitar antes de persistir | HTTP 422 com mensagem segura; evento `Bloqueado`, destino `Persistencia` |
| Publicação no grafo | Rejeitar antes de gravar | `ApiException` 422 antes de qualquer escrita; evento destino `Grafo` |
| Extração por LLM (texto da fonte) | Sanitizar | Evento `Sanitizado`, destino `Llm` |
| Login | **Sem substituição**: a senha original só é usada em `VerifyHashedPassword` | Nenhum evento, nada em log |
| Cadastro de usuário | **Sem substituição**: a senha original só vira hash | Idem; e-mail administrativo legítimo não é tratado como PII |

Não há substituição global de campos `senha` no JSON (quebraria login e cadastro). Há teste com senha parecida com PII.

## Taint analysis (escopo real)

`source → marcação → tratamento → verificação do sink`:

1. **Source**: `PrivacyFilter.Analyze` varre o campo e devolve `PiiScan` (sem o original).
2. **Marcação**: valores detectados viram `TaintMark` apenas **em memória, por requisição** (`SinkGuard` é *scoped*). `ToString()` não revela o conteúdo; o valor é interno ao tipo.
3. **Tratamento**: sanitização (conversa) ou rejeição (fontes/rascunhos). As marcas se **propagam**: o valor de uma senha citada na pergunta é removido também quando aparece solto no histórico.
4. **Sink**: `VerifyAsync`/`RequireCleanAsync` reanalisam o texto de saída com regex **e** procuram os valores marcados (inclusive reformatados, em outra caixa ou com dígitos separados). Se acharem, bloqueiam, registram só metadados e produzem resposta segura.

Sinks cobertos: chamada à LLM de conhecimento, resposta pública da conversa, persistência de fontes/rascunhos, publicação no grafo e as linhas de log escritas por `SinkGuard.Log`.

**Limites reais** (não afirmar proteção maior):

- **Não** há proteção automática de todo `ILogger`, de toda a aplicação ou de código futuro. Apenas os caminhos acima foram instrumentados. Revisão dos logs existentes: `Program.cs` registra só status e tipo da exceção; nenhum log novo contém corpo HTTP, pergunta, histórico, senha, prompt ou mensagem de exceção.
- É uma análise em tempo de execução, por requisição e por texto; não é análise estática do código.
- A marca vive só na requisição: não há rastreamento entre requisições nem entre processos.
- Falha de auditoria **não libera** dado (a decisão vem antes e não depende do banco), mas o evento é perdido: há uma única tentativa com timeout de 3 s, sem fila.

## Detecção: cobertura, falsos positivos e negativos

Cobertos: CPF formatado e sem formatação (sem formatação exige dígitos verificadores válidos **ou** a palavra “CPF” antes, para não marcar telefones e protocolos), RG **somente com contexto** (“RG”, “identidade”, “registro geral” + número), e-mail, senha em campo (`senha=`, `password:`) e em frase (“minha senha é …”), saúde em frases pessoais (“tenho diabetes”, “fui diagnosticada com …”, `CID F32`).

Exemplos que **não** são tratados como dado pessoal: “Como alterar minha senha?”, “Onde fica o hospital municipal?”, “Como pedir a segunda via do RG?”, “Há atendimento para quem tem diabetes?”, “Meu telefone é 11999998888”.

Limitações documentadas (e verificadas em teste):

- Falso negativo: CPF por extenso (“um dois três…”) ou com espaços (`123 456 789 00`) não é detectado na entrada; só seria barrado na saída se o mesmo valor já tivesse sido marcado.
- Falso negativo: o valor de uma senha com menos de 6 caracteres é removido onde vem identificado (`senha=…`), mas **não** é rastreado em texto livre (evita bloquear respostas que contenham palavras comuns como “teste”).
- Falso negativo: saúde fora do vocabulário e das frases listadas; nomes, endereços, telefones e datas de nascimento não são detectados.
- Falso positivo: “minha senha é importante” é lido como senha; e-mail institucional em fonte/rascunho é rejeitado (a política é conservadora).
- Regex e regras de contexto não detectam qualquer dado pessoal possível.

## Auditoria (PostgreSQL)

Tabela `EventosSeguranca`: id, correlação aleatória da operação, município, data/hora UTC, categoria, origem (caminho controlado, ex. `conversa.historico[0].resposta`), destino, ação (`Sanitizado`/`Bloqueado`/`Pendente`), gravidade potencial, código técnico (`PII_SANITIZADA`, `SINK_BLOQUEADO`, `CONTEUDO_REJEITADO`), estado/tentativas/texto/data da tradução.

**Não** são salvos: pergunta ou histórico, valor ou trecho detectado, senha, token, corpo HTTP, hash de CPF ou outro identificador, IP e dispositivo. Teste serializa os eventos e confirma a ausência dos valores simulados.

- **Retenção**: 90 dias por padrão (`Security__RetentionDays`); o worker apaga em lotes de 500 a cada ~1 h.
- `Pendente` está no modelo (proteção ainda não concluída), mas **nenhum caminho atual o emite**; hoje o painel mostra 0 nessa contagem.
- O evento usa conexão própria: não é desfeito por rollback da transação da requisição.

## Tradução por IA

- `IRiskTranslator` é separado de `ILlmWiki` (conhecimento municipal intacto). Implementação: `OpenAiCompatibleRiskTranslator`, `HttpClient` com timeout de 8 s para qualquer provedor com endpoint `POST {BaseUrl}/chat/completions` (OpenAI, Gemini em modo compatível etc.).
- Variáveis: `RiskTranslator__BaseUrl` (https), `RiskTranslator__Model`, `RiskTranslator__ApiKey`. **Nenhum modelo é fixado**: confirme o identificador atual na documentação oficial do provedor. Opcionais: `RiskTranslator__PollSeconds` (mín. 5, padrão 10), `RiskTranslator__WorkerEnabled`.
- O prompt recebe só metadados controlados (rótulo da categoria, origem e destino em rótulos fixos, ação, código, gravidade). Pede pt-BR, no máximo duas frases, linguagem simples, descrição fiel da ação e nenhuma alegação de ataque, acesso indevido ou vazamento.
- A saída da IA também é verificada: ≤ 400 caracteres, ≤ 2 frases, sem PII e sem termos como “ataque”, “invasão”, “hacker”, “vazou”. Reprovada conta como tentativa falha.
- Fora do caminho crítico: a conversa só grava o evento (`Pendente`); um único worker processa lotes de 5, no máximo 3 tentativas por evento (depois, `Falhou`) e reaproveita a tradução de eventos idênticos para não duplicar chamadas. Sem credencial, os eventos ficam `Pendente` sem gastar tentativas.
- Sem tradução, o painel mostra a **mensagem padrão do sistema**, rotulada “Mensagem padrão do sistema (tradução por IA pendente/falhou)” — nunca como resposta de IA.
- Em mais de uma instância da API não há trava distribuída: duas podem traduzir o mesmo evento (o texto aceito é equivalente).

## Painel e indicador “Risco Cidadão”

- `GET /api/v1/municipios/{id}/seguranca/painel?horas=24` (JWT; ver [contratos](../contratos-de-integracao.md)); página em `/auditoria/`. A página é estática e pública, **sem dados embutidos**; o login reutiliza `POST /auth/login`; o token fica só em memória (sem `localStorage`, URL ou cookie); o conteúdo é inserido com `textContent`.
- ADMIN consulta apenas o próprio município (403 nos demais); OPERADOR segue a política existente; a validação é no backend.
- **Período**: 24 h por padrão, 1–168 h.
- **Regra** (gravidade potencial é do tipo de dado: senha/saúde alta, CPF/RG média, e-mail baixa; a situação da proteção é a ação):
  - VERMELHO: existe evento `Pendente` de gravidade alta (proteção não confirmada);
  - AMARELO: existe evento `Pendente`, ou evento tratado (sanitizado/bloqueado) de gravidade média/alta;
  - VERDE: nenhum evento, ou apenas eventos tratados de gravidade baixa.
- Bloqueio bem-sucedido nunca é contado como vazamento confirmado. “Sem eventos” aparece como “Sem eventos registrados” e o texto afirma que **não é garantia de segurança**.
- Estados de carregamento, vazio e erro; ícone + texto (não só cor); semântica de tabela, foco visível e `aria-live`.

## Limite do mobile

Proteger a API **não** remove a pergunta original que o aplicativo Expo pode salvar localmente no SQLite do dispositivo. Esta entrega não altera a persistência mobile; a decisão de reter, mascarar ou apagar esse histórico local é uma pendência da frente mobile.

## Verificações

Executadas (ver [entrega-cybersecurity.md](../entrega-cybersecurity.md) para comandos e resultados): testes automatizados com API real em memória (EF InMemory), grafo e LLM de conhecimento simulados, transporte HTTP do tradutor simulado. **Não executados neste ambiente**: o teste de integração com PostgreSQL e Neo4j (requer Docker), a publicação real no grafo com bloqueio (coberta por trecho novo desse teste, só roda no CI), qualquer chamada a provedor de IA real, a página no navegador, PDF, capturas e vídeo.
