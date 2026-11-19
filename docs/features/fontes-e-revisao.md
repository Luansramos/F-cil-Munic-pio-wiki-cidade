# Cadastro de fontes e revisão

**Status:** endpoints administrativos e extração Azure OpenAI implementados. A tela do painel ainda não foi integrada.

**Backlog:** B1–B8, com foco nas etapas de API.

## O que foi feito

O administrador pode cadastrar uma fonte com município, título, URL, texto e data de consulta. O cadastro não publica nada automaticamente.

A API permite criar um rascunho de serviço, consultar seu conteúdo e salvar alterações. Cada revisão gera um identificador próprio; se alguém tentar salvar uma revisão antiga, a API recusa a alteração. Somente rascunhos aprovados podem ser publicados.

Também existe a rota de extração `POST /api/v1/fontes/{id}/extracoes`. Com o modo Azure configurado, ela solicita uma proposta estruturada ao modelo e salva o resultado como rascunho pendente de revisão humana. A API valida os campos antes de salvar. O modo `demo` não faz extração e o roteiro de SOA continua usando rascunho manual.

O adaptador, a configuração local e seus limites estão documentados em [LLM Wiki e Azure OpenAI](llm-wiki.md).

## Verificação

O teste de integração em `tests/Api.Tests/IntegrationTests.cs` confere autorização, cadastro, revisão desatualizada e bloqueio de publicação antes da aprovação.

O roteiro `scripts/demo_fluxo_http.py` usa uma fonte pública de Poá sobre Cata Treco, cria e aprova um rascunho e publica o serviço. Foi executado com sucesso no [GitHub Actions](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42721825544).

## Pendente

Construir o formulário e a tela de revisão no painel e verificar as informações dos demais serviços do piloto.
