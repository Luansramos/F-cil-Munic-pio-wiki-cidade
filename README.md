# Fácil Município — Wiki Cidade

O Fácil Município é um projeto da Global Solution 2026 da FIAP. A ideia é facilitar a busca por informações de serviços municipais: o morador faz uma pergunta e recebe uma orientação com o link da fonte consultada.

O projeto começou com o nome Nexo Cidadão, que ainda aparece em alguns documentos de planejamento.

## Entrega de Cybersecurity

Esta branch contém o módulo de privacidade da API e o painel de auditoria do Fácil Município.

- **Vídeo do dashboard:** [assistir à apresentação no YouTube](https://www.youtube.com/watch?v=qcuguRsH8UE).
- **Código-fonte:** disponível nesta branch, em `apps/api/`, com testes em `tests/Api.Tests/`.
- **Auditoria e evidências:** [capturas do painel, chamadas HTTP, consulta SQL e resultados dos testes](docs/evidencias-cybersecurity/README.md).
- **Políticas e limites:** [documentação de segurança](docs/features/seguranca.md).
- **Execução:** [instruções da API e configuração do tradutor de eventos](apps/api/README.md).
- **Documento de entrega:** `Facil_Municipio_Cybersecurity_Tecnico_Final.pdf`, enviado separadamente do código na tarefa da disciplina. O PDF de evidências presente em `docs/evidencias-cybersecurity/` é um material de apoio.

| Requisito | Implementação | Evidência |
|---|---|---|
| Parsing de PII | `PrivacyFilter`: CPF, RG contextual, e-mail, senha e saúde | Chamadas HTTP e testes com dados simulados |
| Taint Analysis | `SinkGuard`: marcação em memória por requisição e verificação das saídas instrumentadas | Sanitização de conversas, rejeição de fonte/rascunho com HTTP 422 e testes dos demais sinks |
| Auditoria | Tabela `EventosSeguranca` no PostgreSQL, com metadados e retenção padrão de 90 dias | Consulta SQL sem os valores pessoais originais |
| IA generativa | Tradução dos metadados dos eventos para linguagem simples | Chamada real ao Gemini com HTTP 200; modelo registrado na evidência |
| Dashboard | Indicador Risco Cidadão, contagens, eventos e explicações | Capturas e vídeo; autenticação JWT e restrição por município |

A execução registrada nas evidências terminou com **99 testes aprovados e nenhuma falha**, incluindo integração com PostgreSQL e Neo4j reais. As capturas mostram **12 eventos: 10 sanitizações e 2 bloqueios**. A IA explica os eventos; a classificação de gravidade e o indicador são definidos pelas regras do código.

A demonstração das consultas municipais usa `Llm__Mode=demo`: devolve conhecimento revisado e publicado, sem chamar uma LLM para responder ao cidadão. A chamada real de IA demonstrada em Cybersecurity é a tradução dos eventos de segurança.

### Abrir o painel

Siga o [README da API](apps/api/README.md) para configurar os bancos, aplicar as migrações, criar o operador e iniciar o servidor. Acesse `/auditoria/` na mesma origem da API e entre com o usuário administrativo criado. A tradução por IA é configurada no servidor por variáveis de ambiente; nenhuma chave deve ser colocada no painel ou no repositório.

### Limites da proteção

A detecção usa regex e contexto e pode ter falsos positivos e negativos. A proteção cobre os caminhos instrumentados, não toda a aplicação automaticamente. O histórico local do app mobile está fora do escopo deste módulo. Nenhum caminho atual produz proteção `Pendente`, portanto o indicador vermelho não aparece na demonstração; bloqueio bem-sucedido não significa vazamento confirmado.

## Ficha do projeto

| Item | Descrição |
|---|---|
| Cidade do piloto | Poá (SP) |
| Público | Moradores que precisam encontrar informações sobre serviços urbanos |
| Problema | É difícil saber qual serviço procurar e onde encontrar orientações confiáveis |
| Solução | Organizar fontes municipais em uma base revisada e permitir consultas em linguagem simples |
| Serviços escolhidos | Cata Treco, coleta de lixo e iluminação pública |
| Indicadores previstos | Respostas acompanhadas de fonte e acerto das orientações nos testes do grupo |
| ODS 11 | Facilitar o acesso a informações sobre serviços urbanos |
| ODS 16 | Incentivar o acesso à informação pública e a transparência |
| Integrantes | Identificados na seção abaixo |

A Prefeitura de Poá é uma referência para o piloto, não uma parceira do projeto. Os indicadores são propostas para avaliar o protótipo; ainda não são resultados de impacto.

## Integrantes

- Fernanda Rocha Menon — RM 554673
- Luiza Macena Dantas — RM 556237
- Luan Ramos Garcia de Souza — RM 558537
- Matheus Ricciotti — RM 556930
- Matheus Bortolotto — RM 555189

## Como a solução funciona

O administrador cadastra uma fonte com título, endereço, texto e data da consulta. Depois, revisa as informações do serviço e publica uma versão aprovada. No aplicativo, o cidadão escolhe o município, faz a pergunta e recebe uma resposta associada à fonte.

A arquitetura foi dividida assim:

```text
Painel administrativo (React) ──┐
                                ├── API ASP.NET Core ── PostgreSQL
Aplicativo do cidadão (Expo) ───┘          ├────── Neo4j
                                           └────── Azure OpenAI (SDK .NET)
```

- **PostgreSQL e EF Core:** municípios, usuários, fontes, rascunhos e versões.
- **Neo4j:** serviços publicados, vínculos entre as informações e suas fontes.
- **API C#:** cadastro, autorização, revisão, publicação e consulta.
- **Azure OpenAI:** sugere rascunhos sujeitos à revisão humana e responde com contexto publicado; a API valida evidências e associa as fontes.
- **Painel React:** interface prevista para cadastro e revisão.
- **Expo:** aplicativo do cidadão, desenvolvido na branch mobile.

O piloto começa com Poá. Um segundo município fictício é usado nos testes para verificar se os dados não se misturam.

## Plano de implantação

O plano do grupo é publicar a solução final na Vercel. O painel React pode usar o runtime correspondente ao framework; a API ASP.NET Core está planejada como função baseada em contêiner, com Dockerfile próprio. PostgreSQL e Neo4j precisam de serviços externos compatíveis. A Vercel documenta esse caminho de contêiner HTTP como Functions, mas o runtime de contêiner está em beta; compatibilidade, limites, custos e conexões dos bancos precisam ser validados antes da publicação. Este repositório ainda não representa um deploy concluído.

## O que já foi desenvolvido

A API de SOA e C# foi integrada à `main` pelo [PR #2](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/pull/2).

Ela já permite cadastrar fontes, criar e revisar rascunhos manualmente, publicar serviços no Neo4j e consultá-los pelas rotas públicas. Há testes automatizados com PostgreSQL e Neo4j. Também foi executado um roteiro HTTP que faz o caminho de uma fonte municipal até uma resposta com o link correspondente.

O backend tem o adaptador Azure OpenAI pela SDK oficial .NET, configurado para o deployment `gpt-5-mini` via endpoint OpenAI v1. O smoke pela SDK e um fluxo HTTP em ambiente isolado com Azure real e fonte oficial passaram; veja os limites e a necessidade de revisão editorial humana em [LLM Wiki e Azure OpenAI](docs/features/llm-wiki.md). A LLM sugere rascunhos sujeitos à revisão humana e, na conversa, recebe somente dados publicados do município; a API valida trechos de evidência e fornece os links das fontes. `Llm__Mode=demo` continua disponível sem chamadas externas. O painel e a integração da conversa no Expo ainda precisam ser conectados e verificados ponta a ponta.

O módulo de Cybersecurity acrescenta as verificações de privacidade, a auditoria e o painel descritos acima.

As instruções para executar a API estão em [apps/api/README.md](apps/api/README.md). O [relatório de SOA e C#](docs/entrega-soa-csharp.md) relaciona cada critério do professor ao código. As capturas e os testes estão em [docs/evidencias-soa-csharp.md](docs/evidencias-soa-csharp.md).

## Estrutura do repositório

| Pasta ou arquivo | Conteúdo |
|---|---|
| ml/ | Dados públicos, experimento acadêmico de ML, resultados e relatório; separado do runtime da API |
| `planejamento/` | Visão, especificação, arquitetura, backlog e plano |
| `docs/` | Contratos de integração, decisões e documentação das features |
| `apps/api/` | API ASP.NET Core e módulo de Cybersecurity nesta branch |
| `apps/mobile/` | Aplicativo Expo (na branch de desenvolvimento mobile) |
| `infra/` | Scripts e registros de infraestrutura Linux |
| `tests/Api.Tests/` | Testes da API |
| `scripts/` | Roteiro de demonstração HTTP |
| `prompts.md` | Registro do uso de ferramentas de IA |

As pastas de cada frente entram na `main` após seus respectivos PRs. A estrutura pode variar entre branches durante o desenvolvimento.

## Planejamento e documentação

- [Visão do produto](planejamento/01-visao-do-produto.md)
- [Especificação do MVP](planejamento/02-especificacao-mvp.md)
- [Arquitetura](planejamento/03-arquitetura-e-tecnologias.md)
- [Backlog](planejamento/04-backlog.md)
- [Plano de execução](planejamento/05-plano-de-execucao.md)
- [Entregas por disciplina](planejamento/06-entregas-por-disciplina.md)
- [Contratos da API](docs/contratos-de-integracao.md)
- [Documentação por feature](docs/features/README.md)
- [LLM Wiki e Azure OpenAI](docs/features/llm-wiki.md)
- [Experimento acadêmico de ML](docs/features/machine-learning.md)
- [Orientações para o grupo](AGENTS.md)

Cada integrante trabalha em uma branch, registra o que fez na documentação da feature e abre um PR para revisão de outro integrante. Alterações nos contratos devem ser combinadas com as outras frentes.

A FIAP permite ferramentas de IA como apoio. O uso é registrado em [prompts.md](prompts.md), e o grupo precisa revisar, testar e saber explicar o código antes da entrega.
