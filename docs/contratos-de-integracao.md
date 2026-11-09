# Contratos de integração — Fácil Município

**Estado:** contratos de API implementados na branch `feat/soa-csharp-api`; consumidores Expo/React precisam validar compatibilidade antes do merge. O prefixo implementado é **`/api/v1`**, ao contrário do rascunho inicial sem prefixo.

## Fronteiras

- **Painel → API:** JWT somente para administradores; operador pode configurar municípios e usuários. Todo acesso administrativo restringe `municipioId` no backend.
- **App → API:** três rotas públicas; municípios e serviços publicados; perguntas sem login. Não enviar credencial para o app.
- **API → PostgreSQL (EF Core):** municípios, usuários, fontes, rascunhos e versões ativas. Migração inicial versionada.
- **API → Neo4j:** serviço, fonte, etapas, requisitos, órgão e canal com chaves contendo município e versão.
- **API → LLM:** a interface `ILlmWiki` existe, mas o provedor de LLM real segue pendente; `Llm__Mode=demo` habilita um fallback determinístico explicitamente identificado.

## Endpoints implementados

| Método | Caminho | Autenticação |
|---|---|---|
| POST | `/api/v1/auth/login` | pública (login administrativo) |
| GET | `/api/v1/municipios` | pública |
| POST | `/api/v1/municipios` | OPERADOR |
| POST | `/api/v1/usuarios` | OPERADOR |
| POST | `/api/v1/municipios/{id}/fontes` | ADMIN do município ou OPERADOR |
| GET | `/api/v1/municipios/{id}/fontes` | ADMIN do município ou OPERADOR |
| POST | `/api/v1/fontes/{id}/extracoes` | ADMIN / OPERADOR; LLM ainda indisponível |
| POST | `/api/v1/fontes/{id}/rascunhos` | ADMIN / OPERADOR; criação manual |
| GET | `/api/v1/rascunhos/{id}` | ADMIN / OPERADOR |
| PUT | `/api/v1/rascunhos/{id}` | ADMIN / OPERADOR |
| POST | `/api/v1/rascunhos/{id}/publicacao` | ADMIN / OPERADOR |
| GET | `/api/v1/municipios/{id}/versoes` | ADMIN / OPERADOR |
| GET | `/api/v1/municipios/{id}/servicos` | pública |
| POST | `/api/v1/municipios/{id}/conversas` | pública |
| GET | `/swagger` e `/swagger/v1/swagger.json` | documentação |
| GET | `/health` | pública (liveness, não readiness de banco) |

## Contrato mobile

**GET** `/api/v1/municipios`: lista `{id, nome, uf}`.

**GET** `/api/v1/municipios/{id}/servicos`: lista de `{id,nome}`. Cidade sem publicação retorna `[]`; cidade inexistente retorna 404.

**POST** `/api/v1/municipios/{id}/conversas`:

```json
{
  "pergunta": "Como solicito a retirada de um sofá?",
  "historico": []
}
```

`pergunta` é obrigatória (1–500 caracteres); `historico` é opcional (até 3 trocas com `pergunta` e `resposta` limitadas a 1000 caracteres cada). JSON camelCase. Corpo inválido retorna 422, limite de taxa 429, indisponibilidade de grafo/LLM 503.

Resposta fundamentada (estrutura; os valores dependem do conhecimento efetivamente publicado):

```json
{
  "resposta": "Texto baseado no conteúdo revisado.",
  "servicosRelacionados": ["id-do-servico"],
  "fontes": [{"titulo":"Fonte revisada","url":"https://example.org/exemplo","dataConsulta":"2026-11-01"}],
  "informacaoSuficiente": true
}
```

Resposta insuficiente tem `informacaoSuficiente:false`, `servicosRelacionados:[]`, `fontes:[]`, uma orientação genérica sem inventar fatos e `canalOficialUrl` apenas quando cadastrado. A URL do exemplo acima **não** representa uma fonte oficial de Poá.

## Publicação e segurança

Fonte cadastrada não é publicada. Rascunho registra revisão e aprovação; a publicação usa `revisaoId` como versão imutável. PostgreSQL controla `versaoAtivaId`; Neo4j recebe uma cópia versionada das entidades. Uma falha antes do commit mantém a versão anterior; nova tentativa é idempotente. O endpoint público lê exclusivamente `municipioId` + versão ativa. A API não salva perguntas, históricos ou logs de conteúdo sensível; histórico é responsabilidade local do Expo. Nunca associar resultados ou fontes de outro município à resposta.

**Pendências para integração:** configurar provedor real de LLM, construir painel e Expo, cadastrar e validar links reais oficiais de Poá, executar fluxo real integrado, alinhar mudanças incompatíveis pelo PR.
