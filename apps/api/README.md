# API — SOA e C#

A API do Fácil Município foi desenvolvida em ASP.NET Core 10. Ela cuida das operações administrativas e permite que o aplicativo consulte os serviços publicados de cada cidade. É o mesmo backend usado nas entregas de SOA e C#.

## Organização

O código fica em `apps/api/`:

- `Domain/`: entidades e regras de revisão.
- `Data/`: `AppDbContext`, configurações do Entity Framework Core e migrações.
- `Controllers/`: endpoints REST para autenticação, administração e consulta pública.
- `Services/`: publicação no Neo4j, consulta e montagem das respostas.
- `Security/`: identificação do administrador e tratamento básico de dados sensíveis.
- `Contracts/`: objetos de requisição e resposta.

O PostgreSQL guarda municípios, usuários, fontes, rascunhos e versões. O Neo4j guarda os serviços publicados e os vínculos com as fontes. Antes de disponibilizar um serviço, o administrador precisa aprovar o rascunho. Se a publicação falhar, a versão anterior continua ativa.

A API usa interfaces, injeção de dependência, EF Core, LINQ, `async/await` e autenticação JWT nas rotas administrativas. As consultas do cidadão não exigem login.

## Como executar

Pré-requisitos: .NET SDK 10, Docker com Compose e Git.

1. Na raiz, copie `.env.example` para `.env` e substitua as senhas de exemplo por senhas locais. O arquivo `.env` não deve ser enviado ao GitHub.
2. Execute `docker compose up -d` para iniciar PostgreSQL e Neo4j.
3. Configure as variáveis da API no terminal:

```bash
export ConnectionStrings__Postgres="Host=localhost;Database=facil_municipio;Username=facil;Password=<SENHA_POSTGRES>"
export Neo4j__Uri="bolt://localhost:7687"
export Neo4j__User="neo4j"
export Neo4j__Password="<SENHA_NEO4J>"
export Jwt__Key="<CHAVE_ALEATORIA_COM_PELO_MENOS_32_BYTES>"
export Bootstrap__Email="operador@example.test"
export Bootstrap__Password="<SENHA_LOCAL_COM_12_OU_MAIS_CARACTERES>"
```

Os valores entre `< >` são marcadores, não senhas funcionais. Para gerar uma chave JWT no terminal, pode ser usado `openssl rand -hex 32`.

4. Aplique a migração, crie o operador inicial e inicie a API:

```bash
dotnet restore tests/Api.Tests/Api.Tests.csproj
dotnet run --project apps/api -- --migrate
dotnet run --project apps/api -- --bootstrap
dotnet run --project apps/api
```

O terminal informa a porta da API. O Swagger fica em `/swagger` e a rota de verificação em `/health`. Para testar rotas administrativas, faça o login em `/api/v1/auth/login`, copie o `token` da resposta e use o botão **Authorize** no Swagger. As rotas públicas não exigem JWT.

## Endpoints

Todas as rotas abaixo, exceto `/swagger` e `/health`, começam com `/api/v1`.

| Método | Rota | Uso |
|---|---|---|
| POST | `/auth/login` | Login do administrador |
| POST | `/municipios` | Cadastrar município (operador) |
| POST | `/usuarios` | Cadastrar usuário (operador) |
| POST | `/municipios/{id}/fontes` | Cadastrar fonte |
| GET | `/municipios/{id}/fontes` | Consultar fontes cadastradas |
| POST | `/fontes/{id}/rascunhos` | Criar rascunho manual |
| POST | `/fontes/{id}/extracoes` | Solicitar extração (LLM real ainda não configurada) |
| GET | `/rascunhos/{id}` | Consultar rascunho |
| PUT | `/rascunhos/{id}` | Revisar rascunho |
| POST | `/rascunhos/{id}/publicacao` | Publicar versão aprovada |
| GET | `/municipios/{id}/versoes` | Consultar versões |
| GET | `/municipios` | Listar municípios (pública) |
| GET | `/municipios/{id}/servicos` | Listar serviços publicados (pública) |
| POST | `/municipios/{id}/conversas` | Enviar pergunta (pública) |

A rota de conversa recebe, por exemplo:

```json
{
  "pergunta": "Como solicito a retirada de um sofá?",
  "historico": []
}
```

A resposta traz o texto de orientação, os serviços relacionados, as fontes, a indicação `informacaoSuficiente` e, quando cadastrado, o campo `canalOficialUrl`. A API não salva as perguntas dos cidadãos. O histórico fica sob responsabilidade do aplicativo. A API recebe até três trocas e elimina padrões básicos de CPF, e-mail e senha antes de encaminhar o contexto ao respondedor.

O contrato foi conferido com o app: os campos opcionais que estiverem vazios não são enviados como `null`; os IDs retornados são dos serviços publicados; a resposta usa texto simples e listas numeradas. O orçamento interno é de 12 segundos para respeitar os 15 segundos de timeout do app. Em desenvolvimento, CORS permite `http://localhost:8081`.

## Demonstração com Cata Treco

Para testar o fluxo de SOA e C#, criamos o roteiro `scripts/demo_fluxo_http.py`. Ele usa informações de uma [publicação da Prefeitura de Poá sobre o Cata Treco](https://poa.sp.gov.br/programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/).

O roteiro cadastra a fonte, cria um rascunho manual, faz a revisão, publica no Neo4j e envia uma pergunta à API. Depois verifica se a resposta contém o serviço, a orientação e o link da fonte. Também testa a falta de informação sobre atendimento aos sábados e a separação entre municípios.

Para repetir localmente, configure `Llm__Mode=demo` **antes de iniciar a API**. Com os bancos e o operador preparados, rode:

```bash
# Terminal 1
export Llm__Mode=demo
dotnet run --project apps/api -- --urls http://127.0.0.1:5055

# Terminal 2 (com Bootstrap__Email e Bootstrap__Password definidos)
python3 scripts/demo_fluxo_http.py
```

O modo `demo` não chama uma LLM. Ele apenas monta a resposta usando as informações que já foram revisadas e publicadas. A extração com LLM real e a integração com a tela do Expo ainda não foram concluídas.

## Testes e evidências

Com PostgreSQL e Neo4j configurados, execute:

```bash
dotnet test tests/Api.Tests/Api.Tests.csproj -c Release
```

O GitHub Actions executa esses testes com os bancos, valida as migrações e roda o roteiro HTTP. A [execução de 10/11/2026](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42902618547) passou com 41 testes aprovados e o cenário HTTP concluído.

As evidências e a relação com a rubrica estão em [docs/evidencias-soa-csharp.md](../../docs/evidencias-soa-csharp.md). A explicação da arquitetura e dos critérios da avaliação está no [relatório técnico](../../docs/entrega-soa-csharp.md). Os detalhes do contrato com o aplicativo estão em [docs/contratos-de-integracao.md](../../docs/contratos-de-integracao.md).

A API foi testada no ambiente automatizado. A equipe ainda precisa testar a integração com o Expo, adicionar os nomes dos integrantes no README principal e conferir as fontes antes da apresentação.
