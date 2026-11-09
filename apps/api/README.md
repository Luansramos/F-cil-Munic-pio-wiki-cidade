# Fácil Município — API para SOA e C#

API REST ASP.NET Core (.NET 10) para o mesmo projeto CitizenTech do painel React e do aplicativo Expo. **Não é um serviço separado por disciplina:** uma implementação demonstra os conteúdos de SOA e C#.

## O que está implementado

- POO: entidades `Municipio`, `Usuario`, `Fonte`, `Rascunho` e `Versao`; revisão aprovada por método de domínio.
- Interfaces/injeção de dependência: `IKnowledgeGraph`, `ILlmWiki`, `PublicationService`, `ConversationService`.
- EF Core + LINQ + PostgreSQL para entidades administrativas, migração inicial versionada e consultas sem tracking.
- Neo4j para serviços aprovados, etapas, requisitos, órgão, canal e vínculo à fonte.
- Publicação versionada: grava primeiro uma versão do grafo e só depois ativa a versão no PostgreSQL; a anterior continua legível se a operação falhar.
- ASP.NET Core Web API, autenticação JWT para administradores, autorização de município no servidor, Swagger e endpoints públicos sem autenticação.
- Operações assíncronas com `async/await`, `CancellationToken` e limite de tempo para respostas.
- Testes unitários e teste de integração concebido para PostgreSQL + Neo4j reais no GitHub Actions.

## Pré-requisitos

.NET SDK **10**, Docker com Compose, Git. Instale o EF CLI se for criar novas migrações (`dotnet tool install --global dotnet-ef --version 10.0.4`). O código usa PostgreSQL 17 e Neo4j 5.26 Community como referência de desenvolvimento.

## Configuração local

1. Na raiz: copie `.env.example` para `.env` e escolha senhas **locais fictícias**, exclusivas para o desenvolvimento. Nunca versione `.env`.
2. Execute `docker compose up -d` para iniciar bancos. PostgreSQL fica em `localhost:5432` e Neo4j Bolt em `localhost:7687`.
3. Defina as variáveis da API no terminal (exemplo com valores **substituídos** pelo ambiente):

```bash
export ConnectionStrings__Postgres="Host=localhost;Database=facil_municipio;Username=facil;Password=<SENHA_POSTGRES>"
export Neo4j__Uri="bolt://localhost:7687"
export Neo4j__User="neo4j"
export Neo4j__Password="<SENHA_NEO4J>"
export Jwt__Key="<CHAVE_ALEATORIA_DE_PELO_MENOS_32_BYTES>"
export Bootstrap__Email="operador@example.test"
export Bootstrap__Password="<SENHA_LOCAL_COM_12_OU_MAIS_CARACTERES>"
```

Os dados acima são apenas **marcadores de configuração**, não credenciais prontas. Em Bash, `openssl rand -hex 32` gera uma chave de desenvolvimento adequada para `Jwt__Key`.

4. Com os serviços prontos, na raiz do repositório:

```bash
dotnet restore tests/Api.Tests/Api.Tests.csproj
dotnet run --project apps/api -- --migrate
dotnet run --project apps/api -- --bootstrap
dotnet run --project apps/api
```

A API documenta rotas em `/swagger`, disponibiliza `/health` e utiliza prefixo **`/api/v1`**. A URL/porta local é mostrada pelo comando `dotnet run`.

Para executar a integração localmente, mantenha bancos ligados e execute `dotnet test tests/Api.Tests/Api.Tests.csproj`. Os testes precisam das variáveis de configuração acima; o cenário de CI configura seu próprio ambiente isolado. O teste cria apenas cidades, contas e serviços fictícios.

## Contratos principais (camelCase)

Públicos, **sem login**:
- `GET /api/v1/municipios`
- `GET /api/v1/municipios/{id}/servicos`
- `POST /api/v1/municipios/{id}/conversas` — envia `{"pergunta":"Como solicito a retirada de um sofá?","historico":[]}`.

Resposta: `resposta`, `servicosRelacionados` (IDs), `fontes` (`titulo`, `url`, `dataConsulta`), `informacaoSuficiente` e `canalOficialUrl` quando conhecido. O sistema **não persiste** pergunta ou histórico; o aplicativo deve guardar o histórico localmente.

Administrativos, requerem `Authorization: Bearer <token>`:
- `POST /api/v1/auth/login`
- `POST /api/v1/municipios` e `POST /api/v1/usuarios` (somente operador)
- `POST /api/v1/municipios/{id}/fontes`, `GET /api/v1/municipios/{id}/fontes`
- `POST /api/v1/fontes/{id}/rascunhos` (rascunho manual para integração)
- `POST /api/v1/fontes/{id}/extracoes` (depende de provedor real de LLM)
- `GET /api/v1/rascunhos/{id}`, `PUT /api/v1/rascunhos/{id}`
- `POST /api/v1/rascunhos/{id}/publicacao`
- `GET /api/v1/municipios/{id}/versoes`

O operador inicial deve ser criado com o argumento `--bootstrap`; admins comuns só acessam o município autorizado.

## Demonstração de consulta fundamentada

**Sem credenciais de provedor LLM:** a implementação padrão não inventa respostas e retorna indisponibilidade quando a base tem informações publicadas que precisariam de uma LLM. Para exibir a jornada de perguntas no protótipo sem depender de um serviço externo, configure `Llm__Mode=demo`: um respondedor **determinístico, não uma LLM**, seleciona o serviço a partir dos termos da pergunta e monta a orientação somente com descrição, etapas e requisitos publicados no grafo. Perguntas sobre detalhes não documentados (por exemplo, sábado ou lixo eletrônico) retornam `informacaoSuficiente: false`. As URLs devolvidas são recuperadas da fonte armazenada no grafo, nunca sugeridas pelo respondedor.

O modo demonstrativo **não** implementa extração por LLM: enquanto o provedor e a chave não forem definidos, cadastre a fonte, crie o rascunho manual, revise, aprove e publique. Para homologar a LLM real, integrar uma implementação de `ILlmWiki` e testar extração e geração sem fatos inventados.

## Verificação e pendências

Os testes automatizados abrangem revisão, privacidade básica, validação, isolamento entre municípios, autorização, publicação idempotente, rollback em falha do grafo, fontes e Swagger. Consulte a execução real do workflow `API SOA e CSharp` e as features atualizadas antes de marcar uma entrega como validada. Não foram incluídas fontes oficiais fictícias de Poá, chave de LLM, integração pronta com o Expo, nem evidências de tela inventadas.

## Rubrica das disciplinas

**SOA (10 pts):** estrutura de domínio (3), banco EF/Postgres (3), migrações (1,5), documentação automática (1,5), testes (1).

**C# (10 pts):** POO (3), Web API (2,5), EF Core + LINQ (1,5), `async/await` (1), qualidade (2).

Veja também [contratos compartilhados](../../docs/contratos-de-integracao.md) e [documentação das features](../../docs/features/README.md).