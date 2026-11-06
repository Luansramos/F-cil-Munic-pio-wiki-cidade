# Contratos de integração

**Estado: planejado.** Rotas e esquemas ainda não foram implementados. Mudanças devem ser coordenadas entre responsáveis por API, painel, app e wiki, e registradas na feature e neste documento.

## Fronteiras

- **Painel → API:** operações administrativas autenticadas; o servidor valida o município autorizado.
- **App → API:** municípios e pergunta por município; nenhuma credencial da LLM ou banco no cliente.
- **API → PostgreSQL:** usuários, municípios, fontes, rascunhos e versões administrativas.
- **API → Neo4j:** conhecimento publicado, limitado ao município e à versão ativa.
- **API → LLM:** texto para sugestão e contexto para resposta. Documentos são dados não confiáveis; a saída é validada e revisada antes da publicação.

## Fluxos

1. Fonte + texto → proposta estruturada → rascunho editável. Sugestões não são publicadas automaticamente.
2. Criar versão pendente → gravar grafo com IDs estáveis → ativar versão no PostgreSQL. Falhas preservam a versão anterior e permitem repetição segura.
3. Município + pergunta → serviços candidatos da versão ativa → contexto e fontes → resposta fundamentada ou aviso de informação insuficiente.

## Endpoints propostos

| Método e rota | Responsabilidade |
|---|---|
| `POST /auth/login` | Autenticação administrativa |
| `GET /municipios` | Listar municípios |
| `POST /municipios` | Criar município, restrito ao operador |
| `POST /municipios/{id}/fontes` | Cadastrar título, URL, texto e data |
| `POST /fontes/{id}/extracoes` | Criar proposta de conhecimento |
| `GET /rascunhos/{id}` | Consultar rascunho |
| `PUT /rascunhos/{id}` | Salvar revisão |
| `POST /rascunhos/{id}/publicacao` | Publicar versão aprovada |
| `GET /municipios/{id}/servicos` | Listar serviços publicados |
| `POST /municipios/{id}/conversas` | Consultar o conhecimento |

Os formatos definitivos de requisição, tipos, erros, paginação e autenticação precisam ser acordados antes da implementação paralela. A resposta de conversa deve conter orientação, identificadores de serviço, fontes identificáveis e indicação de informação suficiente.

Antes de mudar rota, campo ou regra, identifique consumidores, documente compatibilidade e coordene os responsáveis. Alterações incompatíveis devem incluir migração ou atualização dos consumidores.
