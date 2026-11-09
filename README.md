# Fácil Município — Wiki Cidade

Plataforma configurável para prefeituras organizarem informações oficiais de serviços públicos e oferecerem orientação conversacional aos cidadãos. Projeto acadêmico da Global Solution FIAP. O backend das disciplinas **SOA e C#** está implementado na branch de desenvolvimento; os demais componentes seguem em trabalhos separados.

O nome usado anteriormente nos documentos é **Nexo Cidadão**. A equipe pode padronizar o nome de produto posteriormente.

## Ficha da CitizenTech — apresentação comum

| Campo | Informação |
|---|---|
| Nome da solução | Fácil Município — Wiki Cidade |
| Cidade piloto | Poá, São Paulo; referências a serviços urbanos, sem parceria declarada com a prefeitura |
| Cidadãos beneficiados | Moradores que precisam encontrar orientações e canais de serviços urbanos |
| Problema | Informações públicas estão distribuídas e pode ser difícil localizar o serviço e o contato correto |
| Funcionamento | Administrador cadastra a fonte, revisa um serviço, publica no grafo e o cidadão consulta uma resposta com fonte |
| Componentes | API C#/.NET + PostgreSQL para revisão + Neo4j para conhecimento publicado; Expo e React previstos para as interfaces; LLM real pendente |
| Indicadores de impacto propostos | Proporção de respostas com fonte verificável e acerto das orientações em perguntas de avaliação |
| ODS 11 | Facilitar o acesso a serviços urbanos e a informação sobre a cidade |
| ODS 16 | Contribuir para transparência, acesso à informação pública e orientação cidadã |
| Equipe | **Integrantes ainda não confirmados neste repositório. Preencher nomes verdadeiros antes do envio.** |

Os indicadores são **propostos** e ainda não representam impacto medido. O cenário automatizado do backend não comprova a integração com a interface Expo ou com uma LLM real.

## Problema e proposta

O cidadão pode ter dificuldade para descobrir o serviço certo, compreender requisitos e encontrar o canal adequado. O produto ajuda a prefeitura a manter uma wiki de serviços e o cidadão a consultar seu conteúdo em linguagem natural.

O administrador cadastra uma fonte oficial com seu texto e URL. A LLM sugere entidades e relações; uma pessoa revisa e publica. O cidadão escolhe o município, pergunta e recebe uma orientação fundamentada com as fontes. Se a base não tiver informações suficientes, o sistema informa essa limitação.

Poá é a referência inicial para coleta de lixo, iluminação pública e Cata Treco. Usaremos fontes públicas; **não há parceria declarada com a prefeitura**. Um segundo município fictício demonstrará configuração e isolamento.

## Estado atual

O repositório contém planejamento, infraestrutura Linux e uma API ASP.NET Core desenvolvida em branch de feature. O backend implementa cadastros, autenticação, revisão, publicação versionada no Neo4j e contratos públicos; a integração com uma LLM real e o aplicativo completo são trabalhos separados. Confira sempre a branch e os testes antes de afirmar que houve integração ponta a ponta.

O primeiro marco é uma fatia completa: fonte → proposta → revisão → publicação no grafo → pergunta no aplicativo → resposta com fonte. O backend já tem uma demonstração automatizada com fonte oficial, **rascunho manual**, revisão, grafo e resposta REST com fonte; o aplicativo e a LLM real ainda dependem dos responsáveis pelas respectivas frentes.

## Arquitetura planejada

```text
Administrador ── Painel React ──┐
                                ├── API ASP.NET Core ── PostgreSQL
Cidadão ──────── App Expo ──────┘         ├──────────── Neo4j
                                          └──────────── Provedor de LLM
```

- **PostgreSQL + EF Core:** municípios, usuários, fontes, rascunhos e versões administrativas.
- **Neo4j:** conhecimento publicado, suas relações, município e versão.
- **API:** autorização, regras, publicação, consulta e integração com a LLM.
- **Painel React:** cadastro, revisão e publicação da wiki.
- **App Expo:** seleção de município, conversa, fontes e histórico local com SQLite.
- **LLM:** sugere estrutura e formula respostas com o contexto recuperado; não publica sem revisão humana nem executa consultas livres no grafo.

A API é um monólito modular. A publicação entre bancos deve ter versão ativa, preservação da versão anterior em caso de falha e repetição segura. Consulte [arquitetura](planejamento/03-arquitetura-e-tecnologias.md) e [contratos](docs/contratos-de-integracao.md).

## Monorepo

```text
.
├── AGENTS.md                   # Guia canônico para agentes
├── CLAUDE.md                   # Entrada para Claude
├── .github/                    # Instruções Copilot e modelo de PR
├── planejamento/               # Visão, spec, arquitetura, backlog e disciplinas
├── docs/                       # Contratos, features e decisões
├── apps/                       # Planejado: api/, admin/, mobile/
├── ml/                         # Planejado: notebook e documentação de dados
├── infra/                      # Planejado: ambiente e scripts Linux
├── iot/                        # Planejado: circuito e evidências Wokwi
└── prompts.md                  # Registro de usos relevantes de IA
```

Os diretórios de implementação serão criados pelas respectivas features. A existência de várias pastas não implica microsserviços. Cada área terá suas dependências, mas compartilhará contratos e documentação na raiz.

## Documentos

- [Índice do planejamento](planejamento/README.md)
- [Visão do produto](planejamento/01-visao-do-produto.md)
- [Especificação do MVP](planejamento/02-especificacao-mvp.md)
- [Arquitetura e tecnologias](planejamento/03-arquitetura-e-tecnologias.md)
- [Backlog priorizado](planejamento/04-backlog.md)
- [Plano de execução](planejamento/05-plano-de-execucao.md)
- [Entregas por disciplina](planejamento/06-entregas-por-disciplina.md)
- [Contratos de integração](docs/contratos-de-integracao.md)
- [Features e modelo](docs/features/README.md)
- [Decisões arquiteturais](docs/decisoes/README.md)
- [Guia para agentes](AGENTS.md)

## Como o time trabalha com IA

O projeto é **AI-first**, com responsabilidade humana: agentes investigam, implementam e documentam; o integrante responsável entende a mudança, revisa decisões e verifica os resultados. Antes de trabalhar, cada agente lê [AGENTS.md](AGENTS.md), a spec, a arquitetura e a feature afetada.

Cada feature registra problema, escopo, contratos, dependências, critérios de aceite, verificações realizadas e limitações. Mudanças de contrato ou arquitetura são documentadas e coordenadas com os componentes afetados. Usos relevantes de IA são registrados em [prompts.md](prompts.md), sem segredos ou dados pessoais.

## Como começar

1. Leia a [spec](planejamento/02-especificacao-mvp.md) e o [backlog](planejamento/04-backlog.md).
2. Assuma uma tarefa e leia seu registro em [docs/features](docs/features/README.md).
3. Combine contratos e versões ainda pendentes com os responsáveis.
4. Implemente uma fatia pequena e atualize a feature com evidências reais.
5. Peça revisão a outro integrante antes de integrar à branch principal.

**Execução local da API:** consulte [apps/api/README.md](apps/api/README.md) para variáveis de ambiente, PostgreSQL, Neo4j, migrações, bootstrap do operador, endpoints e testes. Os comandos foram documentados; a validação depende do CI e do ambiente usado.

## Evidências de SOA e C#

O teste automatizado [demo_fluxo_http.py](scripts/demo_fluxo_http.py) registra a sequência HTTP fonte → rascunho manual → revisão → publicação no Neo4j → pergunta → resposta fundamentada; as evidências do GitHub Actions ficam documentadas em [evidências SOA/C#](docs/evidencias-soa-csharp.md). A especificação completa e os detalhes de execução constam em [apps/api/README.md](apps/api/README.md).

## Disciplinas

A solução apoia C#, SOA, Mobile, IA & ML, Cybersecurity, Operating Systems, Physical Computing e Testing. A LLM do produto não substitui o experimento de ML com dados públicos reais. Consulte [entregas por disciplina](planejamento/06-entregas-por-disciplina.md). Responsáveis P1–P5 e prazos no planejamento são sugestões a confirmar pelo grupo.
