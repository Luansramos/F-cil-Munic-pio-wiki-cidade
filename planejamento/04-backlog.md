# Backlog do Nexo Cidadão

Referência: [especificação](02-especificacao-mvp.md), [arquitetura](03-arquitetura-e-tecnologias.md) e [entregas por disciplina](06-entregas-por-disciplina.md).

**P0:** começar imediatamente; pode bloquear trabalho ou disciplina. **P1:** necessário para MVP ou entrega. **P2:** melhoria posterior ao núcleo funcional. P1 a P5 são responsáveis sugeridos, não nomes confirmados.

**Atualização de implementação (17/11/2026):** A4, B5, B6 (parte da API), D2 e D3 têm adaptador Azure OpenAI configurado para `gpt-5-mini`; uma chamada HTTP sintética com JSON Schema retornou HTTP 200 e 109 testes unitários sem as integrações dependentes de bancos passaram no WSL. O smoke de inferência ao vivo pelo SDK .NET ainda não foi executado. Telas administrativas, fontes reais de Poá e integração visual do Expo permanecem no escopo de suas frentes. Detalhes em [LLM Wiki e Azure OpenAI](../docs/features/llm-wiki.md).

## A. Base e contratos

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| A1 | P0 | P1 + todos | Criar repositório, pastas, README e integrantes; cada componente tem localização e instruções iniciais. | — |
| A2 | P0 | P1 + P3 + P5 | Definir esquemas das entidades e contratos de API; time consegue iniciar interfaces em paralelo. | A1 |
| A3 | P0 | P1 | Configurar PostgreSQL e Neo4j localmente; API consegue conectar e README descreve inicialização. | A1 |
| A4 | P0 | P3 + P1 | Escolher e configurar provedor de LLM no backend; chamada de desenvolvimento funciona sem chave no código. | A1 |
| A5 | P1 | P5 | Inserir ficha da CitizenTech no README: público, problema, município, funcionamento, indicador e ODS. | A1 |
| A6 | P1 | Todos | Registrar usos relevantes de IA em `prompts.md`. | A1 |

## B. Cadastro, fontes e revisão

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| B1 | P1 | P1 | Cadastrar e listar municípios; Poá e município fictício podem ser configurados. | A2, A3 |
| B2 | P1 | P1 | Implementar login e autorização; administrador só altera seu município. | A2, A3 |
| B3 | P1 | P1 | Salvar fonte com município, título, URL, texto e data de consulta. | A2, A3 |
| B4 | P1 | P5 | Criar formulário de fonte no painel e integrá-lo à API. | A2, B3 |
| B5 | P1 | P3 | Extrair proposta estruturada com LLM; retornar serviço, órgão, etapas, requisitos, canais e vínculos com a fonte. | A2, A4 |
| B6 | P1 | P1 + P3 | Validar formato e salvar proposta como rascunho; lacunas ficam explícitas. | B3, B5 |
| B7 | P1 | P5 | Criar tela de revisão; administrador edita campos e relações antes de publicar. | A2, B6 |
| B8 | P1 | P5 + P1 | Exibir serviço, fonte e histórico básico de versões no painel. | C3 |

Enquanto B5 não estiver pronta, usar uma resposta de exemplo seguindo o contrato de A2 para desenvolver a integração; substituir pelo fluxo real antes da demonstração.

## C. Grafo e publicação

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| C1 | P1 | P1 + P3 | Gravar e consultar entidades, relações e fontes no Neo4j. | A2, A3 |
| C2 | P1 | P1 | Restringir registros e consultas ao município e à versão ativa. | B1, C1 |
| C3 | P1 | P1 | Publicar rascunho aprovado com identificador de versão; serviço aparece no grafo. | B6, C1 |
| C4 | P1 | P1 | Tratar falha e repetição: versão anterior continua disponível e não há duplicatas. | C3 |
| C5 | P1 | P3 + P5 | Revisar e cadastrar fontes dos três serviços de Poá, com URL e data. | B3 |
| C6 | P1 | P5 + P1 | Configurar serviço do município fictício e demonstrar isolamento entre municípios. | B1, C2, C3 |

## D. Consulta e aplicativo

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| D1 | P1 | P1 + P3 | Buscar serviços publicados por município; encontrar candidatos para os três temas do piloto. | C2, C3 |
| D2 | P1 | P3 + P1 | Gerar resposta com contexto do grafo e links de fonte. | A4, D1 |
| D3 | P1 | P3 | Tratar falta de informação; resposta informa insuficiência sem inventar fatos. | D2 |
| D4 | P1 | P2 | Criar seleção de município e tela de conversa no Expo. | A2 |
| D5 | P1 | P2 + P1 | Integrar app à API e exibir resposta e fontes. | D2, D4 |
| D6 | P1 | P2 | Salvar histórico local com SQLite; histórico persiste após reiniciar o app. | D4 |
| D7 | P1 | P2 | Revisar acessibilidade básica, legibilidade, contraste e navegação. | D4 |
| D8 | P1 | P2 | Gravar vídeo de até 3 minutos com app funcionando. | D5, D6 |

## E. IA & Machine Learning

O experimento de ML é separado da LLMWiki. Dados simulados não são aceitos pela disciplina.

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| E1 | P0 | P3 | Localizar dados públicos reais acessíveis sobre demandas ou serviços urbanos; registrar fonte, período e formato. | — |
| E2 | P0 | P3 | Formular pergunta respondível pelos dados e escolher modelo aceito pelo briefing. | E1 |
| E3 | P1 | P3 | Preparar dados e dividir treino/teste; documentar processo no notebook. | E2 |
| E4 | P1 | P3 | Testar ao menos 10 combinações de 3 hiperparâmetros; validação cruzada só no treino. | E3 |
| E5 | P1 | P3 | Apresentar métricas e gráficos de treino/teste e discutir sobreajuste e significado para o cidadão. | E4 |
| E6 | P1 | P3 | Entregar `ml/` com notebook executado, `dados/README.md`, `requirements.txt`, relatório e vídeo de até 5 min. | E5 |

E1 foi atendido para uma análise exploratória de volume agregado com a série pública de Poá descrita abaixo. As páginas de serviço do app continuam insuficientes para avaliar ocorrências por serviço. Não fabricar dados para cumprir a disciplina. O briefing menciona tanto relatório de 2 a 4 páginas quanto 2 páginas; preparar 2 e conferir a tarefa oficial.

**Atualização (17/11/2026):** foi encontrada uma série pública mensal agregada de Poá para 2023-2024, reconciliada com 717 e 853 manifestações anuais. Notebook, avaliação temporal de 18 combinações, resultados e relatório de duas páginas foram executados. A execução usa uma implementação autocontida de Random Forest porque scikit-learn e acesso ao PyPI não estavam disponíveis; a aderência dessa adaptação à rubrica deve ser confirmada por P3/P5. O vídeo ainda precisa ser gravado e o pacote revisado.

## F. Cybersecurity

Estado em `feature/cybersecurity` (não integrada): F2 a F5 implementados no código e testados em memória; F1 resolvido pela especificação em `docs/cybersecurity.md`; F4 sem chamada real a provedor de IA; F5 sem captura e vídeo; F6 pendente de PDF e evidências reais. Detalhes em [entrega-cybersecurity.md](../docs/entrega-cybersecurity.md).

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| F1 | P0 | P4 | Identificar código-base e instruções da atividade no Teams; registrar o que precisa ser adaptado. | — |
| F2 | P1 | P4 + P1 | Implementar regras PII para exemplos simulados de CPF, saúde e senhas, conforme a atividade. | F1 |
| F3 | P1 | P4 | Demonstrar taint analysis e bloqueio de saída insegura no protótipo exigido. | F1, F2 |
| F4 | P1 | P4 + P3 | Usar LLM para traduzir uma falha técnica em alerta de risco ao cidadão. | F1 |
| F5 | P1 | P4 | Atualizar dashboard com indicador de Risco Cidadão; registrar captura e vídeo. | F1, F3, F4 |
| F6 | P1 | P4 | Preparar ZIP com código do módulo atualizado e evidências de auditoria. | F2–F5 |

Autenticação e separação por município do produto não substituem, por si só, os critérios e o módulo específicos de Cybersecurity.

## G. Operating Systems e Physical Computing

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| G1 | P1 | P4 | Criar provisionamento Linux de infraestrutura, rede e configurações. | A1 |
| G2 | P1 | P4 + P1 | Automatizar dependências e inicialização da aplicação escolhida na VM. | G1, aplicação executável |
| G3 | P1 | P4 | Monitorar CPU, memória e disco, gerar logs e configurar serviço tolerante a falhas. | G1 |
| G4 | P1 | P4 | Demonstrar falha, logs e recuperação em vídeo de até 7 minutos. | G2, G3 |
| G5 | P1 | P4 | Criar totem de atendimento com ESP32 no Wokwi e explicar sua função no Nexo. | A2 |
| G6 | P1 | P4 + P1 | Verificar comunicação real entre circuito e aplicação; se inviável, registrar limitação e ajustar a proposta da disciplina. | G5 |
| G7 | P1 | P4 | Preparar link Wokwi, vídeo de até 4 minutos e arquivo de integrantes. | G5, G6 |

## H. Testing, documentação e envio

| ID | Pri. | Responsável | Tarefa e critério de aceite | Depende de |
|---|---|---|---|---|
| H1 | P1 | P5 | Criar features e critérios de aceite no Azure Boards no padrão da disciplina. | A2 |
| H2 | P1 | P5 | Criar casos positivos e negativos com objetivo, preparação, entradas, passos e saídas. | H1 |
| H3 | P1 | P1 + P5 | Criar testes automatizados de regras relevantes da API, incluindo publicação e separação por município. | C2, C3 |
| H4 | P1 | P5 + responsáveis | Reunir evidências, links, capturas e documentos de cada disciplina. | Cada entrega |
| H5 | P1 | P1 + P5 | Finalizar README, integrantes, execução e documentação dos endpoints. | API funcional |
| H6 | P1 | P5 | Preparar PDF de Testing com link do Azure e capturas das features e casos de teste. | H1, H2 |
| H7 | P1 | Todos | Gravar pitch integrado de até 4 minutos. | Fluxo integrado |
| H8 | P1 | Responsável por matéria | Conferir permissões, arquivos e links e efetuar os envios. | Entregas prontas |

## P2 — melhorias opcionais

- Importar automaticamente conteúdo de PDF ou URL.
- Busca vetorial para complementar a busca inicial.
- Visualização gráfica interativa da wiki.
- Personalização visual avançada por prefeitura.
- Indicadores de perguntas sem resposta.
