# Arquitetura e tecnologias

## Princípio

O projeto será um monólito modular: uma única API em C# com módulos para municípios, identidade e acesso, fontes, wiki, publicação e conversa. O painel e o aplicativo consomem a mesma API.

```text
Administrador → Painel React ─┐
                              ├→ API ASP.NET Core → PostgreSQL
Cidadão → App Expo ──────────┘        │          → Neo4j
                                      └──────────→ Provedor de LLM
```

## Stack

| Componente | Decisão |
|---|---|
| API | C# e ASP.NET Core Web API |
| Dados administrativos | PostgreSQL e Entity Framework Core |
| Conhecimento publicado | Neo4j e driver oficial .NET |
| Painel | React, TypeScript e Vite |
| Aplicativo | React Native, TypeScript, Expo e Expo Router |
| Histórico local do app | Expo SQLite |
| Autenticação administrativa | JWT emitido pela API |
| Documentação de API | Swagger ou equivalente |
| Provedor de LLM | Azure OpenAI pela SDK oficial `OpenAI` para .NET, API v1 |
| Desenvolvimento local | Docker Compose para PostgreSQL e Neo4j |
| Infraestrutura da disciplina | Linux, Bash, systemd e logs |
| ML acadêmico | Python, notebook e biblioteca adequada ao modelo escolhido |
| Physical Computing | ESP32 no Wokwi |

Versões compatíveis devem ser fixadas e registradas no repositório no começo do desenvolvimento. A API encapsula o provedor de LLM atrás da interface `ILlmWiki`. O deployment escolhido para a integração é `gpt-5-mini`, chamado pela API v1 (`/openai/v1`). Para inferência, a API recebe o nome do deployment configurado no Azure; o nome de catálogo ou uma URI `azureml://` não substitui um deployment ativo. A chave permanece no backend e fora do repositório.

## Plano de implantação

O plano atual é publicar a solução final na Vercel. O painel usa o runtime compatível com React; a API ASP.NET Core deve ser empacotada em um contêiner HTTP para Vercel Functions, com `Dockerfile.vercel`. PostgreSQL e Neo4j serão serviços externos, configurados por variáveis/segredos do ambiente. O Compose continua sendo a forma de desenvolvimento local e não é o artefato de produção. O runtime de contêiner da Vercel está em beta; validar build, porta `$PORT`, limites de execução, regiões, conectividade e custos antes de considerar o deploy concluído. O aplicativo Expo tem seu próprio processo de build e distribuição.

## Fonte de verdade e publicação

- **PostgreSQL:** municípios, usuários, fontes, rascunhos e histórico administrativo de versões.
- **Neo4j:** entidades e relações das versões publicadas para consulta.
- Cada entidade e relação pertence a um município e a uma versão. A API restringe consultas ao município selecionado e à versão ativa.

Publicação:

1. Criar uma versão pendente no PostgreSQL com identificador estável.
2. Gravar de forma repetível as entidades e relações da versão no Neo4j.
3. Após o sucesso, ativar a versão no PostgreSQL.
4. A consulta usa a versão ativa. Em falha, manter a versão anterior e permitir nova tentativa sem duplicar conteúdo.

Não existe transação única entre os dois bancos nessa proposta. A implementação precisa tornar a repetição idempotente e testar falhas entre as etapas. Versões antigas no grafo não devem aparecer em consultas da versão ativa.

## Esquema conceitual

```text
Município  —OFERECE→          Serviço
Serviço    —TEM_RESPONSÁVEL→  Órgão
Serviço    —POSSUI_ETAPA→     Etapa
Serviço    —POSSUI_REQUISITO→ Requisito
Serviço    —USA_CANAL→        Canal
Informação —TEM_FONTE→        Fonte
```

Campos mínimos: identificador, município, versão, nome ou descrição e referência à fonte quando houver fato publicado. Fonte: título, URL, texto, data de consulta e identificação do trecho de suporte, quando disponível.

## LLMWiki

**Ingestão:** fonte fornecida pelo administrador → LLM sugere estrutura → validação do formato → rascunho → revisão humana → publicação no grafo.

**Consulta:** município e pergunta → API encontra candidatos publicados → API recupera relações e fontes do grafo → LLM formula a resposta com o contexto → API entrega resposta e fontes. A LLM não executa Cypher livre nem altera o grafo diretamente.

A implementação atual usa saídas JSON Schema estritas. Na conversa, a API só aceita IDs de serviços existentes na versão ativa e exige trechos literais de evidência para todos os serviços citados; os links e canais são montados a partir dos registros publicados, não do texto gerado pelo modelo. A revisão humana da ingestão continua obrigatória.

Com poucos serviços, iniciar com busca por nomes, descrições e termos relacionados; avaliar busca vetorial somente após o fluxo funcionar. Uma resposta sem evidência suficiente deve assumir essa limitação.

## Contratos iniciais de API

| Método | Rota | Finalidade |
|---|---|---|
| POST | `/auth/login` | Autenticação do administrador |
| GET | `/municipios` | Listar municípios |
| POST | `/municipios` | Criar município, restrito ao operador da plataforma |
| POST | `/municipios/{id}/fontes` | Registrar fonte e texto |
| POST | `/fontes/{id}/extracoes` | Gerar proposta de conhecimento |
| GET | `/rascunhos/{id}` | Consultar proposta |
| PUT | `/rascunhos/{id}` | Corrigir proposta |
| POST | `/rascunhos/{id}/publicacao` | Publicar versão |
| GET | `/municipios/{id}/servicos` | Listar serviços publicados |
| POST | `/municipios/{id}/conversas` | Perguntar e receber resposta com fontes |

Contrato mínimo de resposta de conversa: `resposta`, `servicosRelacionados`, `fontes` com título, URL e data, e `informacaoSuficiente`. Definir os esquemas de requisição e resposta em conjunto antes da implementação paralela.

## Limites e riscos

- Dois bancos aumentam trabalho e exigem tratamento explícito de falhas de publicação.
- O deployment Azure, sua disponibilidade regional, cotas e custo devem ser confirmados no ambiente de cada execução.
- As páginas públicas de Poá podem não conter todos os detalhes desejados; responder somente com evidência.
- Não usar dados pessoais reais no protótipo ou nas evidências de segurança.
- O totem ESP32 e sua comunicação com a API precisam ser demonstrados no Wokwi; o desenho de comunicação ainda não foi comprovado.
- LLMWiki não substitui o experimento de ML exigido pela disciplina.
