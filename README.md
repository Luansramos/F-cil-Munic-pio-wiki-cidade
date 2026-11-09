# Fácil Município — Wiki Cidade

O Fácil Município é um projeto da Global Solution 2026 da FIAP. A ideia é facilitar a busca por informações de serviços municipais: o morador faz uma pergunta e recebe uma orientação com o link da fonte consultada.

O projeto começou com o nome Nexo Cidadão, que ainda aparece em alguns documentos de planejamento.

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
| Integrantes | A equipe precisa confirmar os nomes para o README final |

A Prefeitura de Poá é uma referência para o piloto, não uma parceira do projeto. Os indicadores são propostas para avaliar o protótipo; ainda não são resultados de impacto.

## Como a solução funciona

O administrador cadastra uma fonte com título, endereço, texto e data da consulta. Depois, revisa as informações do serviço e publica uma versão aprovada. No aplicativo, o cidadão escolhe o município, faz a pergunta e recebe uma resposta associada à fonte.

A arquitetura foi dividida assim:

```text
Painel administrativo (React) ──┐
                                ├── API ASP.NET Core ── PostgreSQL
Aplicativo do cidadão (Expo) ───┘          ├────── Neo4j
                                           └────── LLM (integração pendente)
```

- **PostgreSQL e EF Core:** municípios, usuários, fontes, rascunhos e versões.
- **Neo4j:** serviços publicados, vínculos entre as informações e suas fontes.
- **API C#:** cadastro, autorização, revisão, publicação e consulta.
- **Painel React:** interface prevista para cadastro e revisão.
- **Expo:** interface prevista para perguntas, fontes e histórico salvo no celular.

O piloto começa com Poá. Um segundo município fictício é usado nos testes para verificar se os dados não se misturam.

## O que já foi desenvolvido

A API de SOA e C# está na branch `feat/soa-csharp-api`, no [PR #2](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/pull/2), ainda sem merge na `main`.

Ela já permite cadastrar fontes, criar e revisar rascunhos manualmente, publicar serviços no Neo4j e consultá-los pelas rotas públicas. Há testes automatizados com PostgreSQL e Neo4j. Também foi executado um roteiro HTTP que faz o caminho de uma fonte municipal até uma resposta com o link correspondente.

Para a demonstração, usamos `Llm__Mode=demo`. Nesse modo a API organiza a resposta a partir do serviço revisado, sem chamar uma LLM. A extração por LLM real, o painel e a integração da conversa no Expo continuam com as frentes responsáveis. Portanto, o fluxo está validado **no backend**, mas ainda não no aplicativo completo.

As instruções para executar a API estão em [apps/api/README.md](apps/api/README.md). O [relatório de SOA e C#](docs/entrega-soa-csharp.md) relaciona cada critério do professor ao código. As capturas e os testes estão em [docs/evidencias-soa-csharp.md](docs/evidencias-soa-csharp.md).

## Estrutura do repositório

| Pasta ou arquivo | Conteúdo |
|---|---|
| `planejamento/` | Visão, especificação, arquitetura, backlog e plano |
| `docs/` | Contratos de integração, decisões e documentação das features |
| `apps/api/` | API ASP.NET Core (na branch de SOA/C#) |
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
- [Orientações para o grupo](AGENTS.md)

Cada integrante trabalha em uma branch, registra o que fez na documentação da feature e abre um PR para revisão de outro integrante. Alterações nos contratos devem ser combinadas com as outras frentes.

A FIAP permite ferramentas de IA como apoio. O uso é registrado em [prompts.md](prompts.md), e o grupo precisa revisar, testar e saber explicar o código antes da entrega.
