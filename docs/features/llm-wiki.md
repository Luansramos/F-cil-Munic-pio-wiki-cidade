# LLM Wiki e Azure OpenAI

- Status: integração Azure implementada; smoke do SDK aprovado com a configuração validada; fluxo da API com fonte oficial e bancos reais verificado em CI com dublê determinístico. A execução da rota HTTP usando Azure real permanece pendente.
- Backlog: A4, B5, B6 (integração da API), D2 e D3.
- Componentes: API ASP.NET Core, `ILlmWiki`, Azure OpenAI.

## Fluxos

### Ingestão

O endpoint administrativo `POST /api/v1/fontes/{id}/extracoes` envia o texto sanitizado da fonte ao deployment Azure OpenAI. O modelo retorna JSON Schema estrito com nome, descrição, órgão, etapas, requisitos, canal e termos. A API deriva o slug, valida limites e URLs com os contratos existentes e salva a proposta como rascunho. Nenhuma sugestão é publicada sem revisão humana.

### Consulta

A API envia ao modelo somente pergunta, histórico sanitizado e serviços publicados da cidade e versão ativa. O modelo precisa fornecer IDs válidos e trechos literais de evidência. A API valida ambos; se faltar evidência ou aparecer ID desconhecido, responde como insuficiente. Links de fontes e canal oficial são montados pelo backend a partir do conteúdo publicado.

O adaptador não recebe texto livre de Cypher, credenciais de banco nem operações de publicação. A pergunta, o histórico e o conteúdo de fonte são tratados como dados não confiáveis nas instruções do modelo. A revisão humana continua sendo a fronteira de publicação.

## Configuração

O backend usa a SDK oficial `OpenAI` para .NET com a API v1 do Azure OpenAI. `gpt-5-mini` oferece Chat Completions, Responses API e saídas estruturadas; este adaptador usa Chat Completions com JSON Schema estrito ([capacidades documentadas pela Microsoft](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/models-sold-directly-by-azure?pivots=azure-openai)). Em desenvolvimento, a API carrega `.env` da raiz do repositório por `DotNetEnv`; variáveis já definidas no processo têm prioridade. No plano de deploy da Vercel, configure essas variáveis como segredos do ambiente da função, sem arquivo `.env`.

```dotenv
LLM__MODE=azure
ENDPOINT_AZURE=https://<recurso>.services.ai.azure.com/openai/v1
LLM_KEY_AZURE=<chave-fornecida-pelo-Azure>
LLM_AZURE_DEPLOYMENT=gpt-5-mini
```

O código chama o endpoint OpenAI v1 (`/openai/v1`) com o nome do deployment `gpt-5-mini`; não recebe uma URI `azureml://`. Se o nome do deployment no recurso mudar, atualize `LLM_AZURE_DEPLOYMENT`. A chave permanece no backend e `.env` está ignorado pelo Git.

`LLM__MODE=demo` seleciona o respondedor determinístico sem chamadas externas. Sem modo e sem credenciais Azure, a integração permanece indisponível de forma explícita.

## Verificação

```bash
dotnet run --project apps/api -- --llm-smoke
```

O comando usa uma fonte e um serviço fictícios, faz uma chamada de extração e outra de resposta fundamentada e não imprime conteúdo nem credenciais. Ele não precisa dos bancos nem de chave JWT, mas requer deployment ativo, rede e cotas disponíveis. As chamadas podem gerar custo do Azure.

Em 17/11/2026, uma chamada HTTP direta a `/chat/completions`, usando `gpt-5-mini` e um esquema JSON sintético, retornou HTTP 200 e o campo esperado. Em 18/11/2026, a suíte `dotnet test tests/Api.Tests/Api.Tests.csproj --filter 'FullyQualifiedName!~IntegrationTests' --verbosity minimal` passou: 109 testes aprovados com .NET SDK 10.0.404. Os testes cobrem extração como proposta revisável, validação de evidências, resposta insuficiente, falhas do provedor e cancelamento; usam transporte simulado e não fazem inferência externa.

O smoke pela SDK foi executado em 18/11/2026. A tentativa com o `.env` local deste repositório falhou com HTTP 404 `DeploymentNotFound`, pois ele não define `LLM_AZURE_DEPLOYMENT` e o código usa `gpt-5-mini` por padrão. Depois, usando somente nas variáveis de ambiente do processo a configuração local indicada pelo usuário como válida — endpoint `https://city-wiki.services.ai.azure.com/openai/v1`, deployment `gpt-5-mini` e a chave correspondente — o comando terminou com código 0. O código só retorna 0 após validar extração estruturada não vazia e resposta fundamentada com evidência, serviço correspondente e texto não vazio. A chave não foi exibida, gravada ou commitada; o `.env` deste repositório permaneceu inalterado e sua configuração não foi validada. A fonte desse smoke é sintética: ele não substitui a validação da extração de fonte oficial de Poá, revisão humana, insuficiência de evidência, isolamento municipal ou fluxo HTTP com os bancos reais.

Em 18/11/2026, o CI do PR #3 (run 108) compilou a API, verificou as migrações e aprovou 111 testes com PostgreSQL e Neo4j reais. `LlmApiIntegrationTests` percorre a rota de extração com o dublê determinístico `TestLlm` e um excerto da [publicação oficial de Poá sobre o Cata Treco](https://poa.sp.gov.br/programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/): confirma que a proposta nasce como rascunho não publicado, que a publicação é bloqueada antes da revisão, e que após aprovação a API retorna resposta com serviço e fonte. O teste também verifica insuficiência, município sem acesso ao conhecimento de outra cidade e HTTP 503 quando o provedor falha. Esse teste valida o contrato e o fluxo da API; não chama Azure. A execução HTTP em modo `demo` no mesmo workflow também permanece determinística.

## Limites

- O filtro de privacidade atual remove padrões básicos de CPF, e-mail e senha; ele não é um mecanismo completo de anonimização.
- A verificação de evidência exige que o trecho citado exista literalmente nos campos factuais do serviço publicado. Ela não garante, sozinha, a qualidade editorial da resposta.
- O smoke Azure do SDK passou com fonte sintética, mas não valida chamada Azure através da rota HTTP nem a qualidade de uma extração real da página de Poá. A rota, a revisão humana, a resposta suficiente/insuficiente, o isolamento municipal e a falha do provedor foram exercitados por CI com dublê e bancos reais; não confundir esse teste com inferência Azure. O `.env` local deste repositório continua diferente da configuração que passou e não foi validado.
- Busca vetorial, atualização automática de páginas, resolução de contradições e lint de wiki continuam fora do escopo implementado.
