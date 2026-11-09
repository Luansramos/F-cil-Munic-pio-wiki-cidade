# Evidências técnicas — SOA e C#

## Execução automatizada confirmada

- Data: 09/11/2026.
- Branch: `feat/soa-csharp-api`.
- Workflow GitHub Actions: [execução com sucesso](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42703817512).
- Resultado confirmado do comando `dotnet test tests/Api.Tests/Api.Tests.csproj -c Release` no CI: **14 testes aprovados, zero falhas**, com PostgreSQL 17 e Neo4j 5.26 Community em serviços reais da execução. `dotnet build` também passou.
- As migrações EF Core geradas a partir do modelo estão versionadas em `apps/api/Data/Migrations`. O workflow seguinte verifica se o modelo alterou sem migração e coleta artefatos HTTP em `api-test-results`.

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
