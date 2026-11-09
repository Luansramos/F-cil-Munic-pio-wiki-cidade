# Feature — Fundação e contratos

- Status: **implementação na branch `feat/soa-csharp-api`; verificação no GitHub Actions em andamento**
- Backlog: A2, A3, B1, B2, H3, H5

## Entrega SOA e C#

A API em `apps/api/` utiliza ASP.NET Core 10, EF Core PostgreSQL, interfaces/DI, async/await, JWT, Swagger e testes. A migração inicial está versionada em `apps/api/Data/Migrations`. Bancos locais em `compose.yaml`.

## Integração e segurança

Rotas públicas `/api/v1/municipios`, `/api/v1/municipios/{id}/servicos` e `/api/v1/municipios/{id}/conversas`, camelCase. Administrador utiliza bearer JWT e isolamento por município no backend. Contratos em [contratos](../contratos-de-integracao.md).

## Verificação e pendências

`dotnet build tests/Api.Tests/Api.Tests.csproj -c Release`; `dotnet test tests/Api.Tests/Api.Tests.csproj -c Release` com PostgreSQL/Neo4j configurados. Registrar resultado final de CI após execução. Provedor real de LLM e integração com consumidores ainda pendentes; não há segredos no repositório.
