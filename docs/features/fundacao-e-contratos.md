# Fundação da API e contratos

**Status:** API implementada na branch `feat/soa-csharp-api`. Integração com Expo e painel administrativo ainda pendente.

**Backlog:** A2, A3, B1, B2, H3 e H5.

## Implementação

Criamos uma API ASP.NET Core 10 para atender às disciplinas de SOA e C#. Os cadastros administrativos usam EF Core com PostgreSQL; os serviços publicados ficam no Neo4j. A migração inicial está versionada em `apps/api/Data/Migrations/`.

O login administrativo usa JWT. A autorização é conferida no servidor e restringe o administrador ao município associado à sua conta. As rotas públicas são:

- `GET /api/v1/municipios`
- `GET /api/v1/municipios/{id}/servicos`
- `POST /api/v1/municipios/{id}/conversas`

As requisições e respostas usam JSON em camelCase. O contrato completo está em [contratos de integração](../contratos-de-integracao.md).

## Como verificamos

No [GitHub Actions](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42721825544), o projeto compilou, passou na checagem das migrações e concluiu 37 testes com PostgreSQL e Neo4j. O roteiro HTTP também passou, desde o cadastro de uma fonte até a resposta com seu link.

Comandos de desenvolvimento e configuração estão em [apps/api/README.md](../../apps/api/README.md). Os registros ficam em [evidências de SOA/C#](../evidencias-soa-csharp.md).

## Pendente

Combinar a URL da API com quem desenvolve o Expo e conectar a tela de conversa. A extração automática por LLM precisa ser implementada pela frente responsável.
