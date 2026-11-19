# LLM Wiki e Azure OpenAI

- Status: integração implementada e suíte unitária aprovada; o smoke Azure via SDK falha com HTTP 404 `DeploymentNotFound` para o nome configurado, então a aceitação de inferência ao vivo permanece pendente.
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

O smoke pelo SDK foi executado em 18/11/2026. A primeira execução apresentou `SocketException` no sandbox; a repetição fora dele alcançou o Azure e falhou na extração com HTTP 404 e código `DeploymentNotFound`. A saída segura não exibiu endpoint, chave ou conteúdo da fonte. O arquivo `.env` não define `LLM_AZURE_DEPLOYMENT`, então o código enviou `gpt-5-mini` como nome do deployment. Uma chamada GET a `/openai/v1/models` retornou HTTP 200, mas lista modelos disponíveis e não confirma um deployment ativo ([referência da API Azure OpenAI Models](https://learn.microsoft.com/en-us/rest/api/aifoundry/azureopenai/models)). Na sessão atual, a Azure CLI não conseguiu consultar a assinatura ou listar deployments. O nome de deployment e o recurso associados à requisição permanecem sem confirmação. Consulte o deployment no portal/gerenciamento Azure, configure seu nome exato e repita o smoke antes de aceitar a inferência ao vivo ([API de listagem de deployments](https://learn.microsoft.com/en-us/rest/api/aifoundry/accountmanagement/deployments/list?view=rest-aifoundry-accountmanagement-2025-06-01)).

## Limites

- O filtro de privacidade atual remove padrões básicos de CPF, e-mail e senha; ele não é um mecanismo completo de anonimização.
- A verificação de evidência exige que o trecho citado exista literalmente nos campos factuais do serviço publicado. Ela não garante, sozinha, a qualidade editorial da resposta.
- O smoke sintético não verifica as fontes reais de Poá, a tela do painel, o Expo nem o caminho HTTP completo entre os bancos e o aplicativo. O smoke Azure ao vivo retorna `DeploymentNotFound` até a configuração ser corrigida.
- Busca vetorial, atualização automática de páginas, resolução de contradições e lint de wiki continuam fora do escopo implementado.
