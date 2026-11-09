# Evidências técnicas — SOA e C#

## Execução automatizada confirmada

- Data: 09/11/2026.
- Branch: `feat/soa-csharp-api`.
- Workflow GitHub Actions: [execução com sucesso](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42703817512).
- Resultado confirmado do comando `dotnet test tests/Api.Tests/Api.Tests.csproj -c Release` no CI: **14 testes aprovados, zero falhas**, com PostgreSQL 17 e Neo4j 5.26 Community em serviços reais da execução. `dotnet build` também passou.
- As migrações EF Core geradas a partir do modelo estão versionadas em `apps/api/Data/Migrations`. O workflow seguinte verifica se o modelo alterou sem migração e coleta artefatos HTTP em `api-test-results`.

## Cenário adicional: fonte oficial → revisão → grafo → resposta

O arquivo [`scripts/demo_fluxo_http.py`](../scripts/demo_fluxo_http.py) realiza uma prova via HTTP com PostgreSQL e Neo4j reais no GitHub Actions:

1. Faz login com conta **fictícia** criada no ambiente efêmero.
2. Cadastra fonte da Prefeitura de Poá: notícia sobre Cata Treco de 24/07/2025, com URL e data de consulta.
3. Cria rascunho **manual** e confirma que ele não estava publicado.
4. Revisa e aprova, publica no Neo4j e confirma a repetição segura.
5. Envia pelo endpoint público a pergunta sobre retirada de sofá.
6. Verifica serviço identificado, telefone informado, link de fonte e link do canal oficial.
7. Pergunta sobre sábado (não documentado na fonte utilizada), esperando informação insuficiente.
8. Verifica que outro município não recebe conteúdo de Poá.

O arquivo `test-results/demo-fluxo-http.json`, incluído nos artefatos após execução bem-sucedida, é um **registro real** das requisições. Não é screenshot do app mobile, não representa extração por LLM e não contém token ou senha.

**Observação:** o fluxo do backend já demonstra a passagem por fonte, revisão e grafo; a etapa de interface Expo depende do responsável pelo mobile. A fonte oficial utilizada foi encontrada em [publicação da Prefeitura de Poá](https://poa.sp.gov.br/programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/). O serviço é cadastrado em município de demonstração, sem representar operação municipal de produção.

## Requisitos comprovados pelo código e pelos testes

| Rubrica | Onde verificar |
|---|---|
| SOA — domínio | `apps/api/Domain/Entities.cs`, regras do `Rascunho` e `PublicationService` |
| SOA — banco | `apps/api/Data/AppDbContext.cs`, EF Core/Npgsql |
| SOA — migrações | `apps/api/Data/Migrations/` e verificação `dotnet ef migrations has-pending-model-changes` |
| SOA — documentação | Swagger disponível em `/swagger/v1/swagger.json` e `/swagger` |
| SOA — testes | `tests/Api.Tests/` e workflow GitHub Actions |
| C# — POO, interfaces e DI | `Domain/Entities.cs`, `Services/Interfaces.cs`, `Program.cs` |
| C# — Web API | `apps/api/Controllers/` |
| C# — EF Core + LINQ | `Data/AppDbContext.cs`, consultas e serviços |
| C# — async/await | Controllers, `ConversationService`, `PublicationService`, `Neo4jKnowledgeGraph` |
| C# — qualidade | separação de camadas, validação, tratamento de erros, controle de concorrência, testes |

## Limites da evidência

O resultado automático comprova funcionamento da API nos cenários testados, **não** comprova operação end-to-end com aplicativo Expo, painel React e provedor LLM real. Os screenshots fornecidos pela equipe representam **protótipo de interface**, e não devem ser apresentados como tela de API efetivamente executada. Para o material visual da disciplina, após subir a API localmente, registrar tela real do Swagger, chamada de endpoint, consulta a serviços publicados e retorno com fonte, usando dados públicos oficiais verificados ou conteúdo claramente fictício de demonstração. A equipe deve revisar estes registros antes de enviá-los no Teams.
