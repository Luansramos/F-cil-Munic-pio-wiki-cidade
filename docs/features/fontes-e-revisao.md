# Cadastro de fontes e revisão

**Status:** endpoints administrativos implementados. A tela do painel e a extração por LLM ainda não foram integradas.

**Backlog:** B1–B8, com foco nas etapas de API.

## O que foi feito

O administrador pode cadastrar uma fonte com município, título, URL, texto e data de consulta. O cadastro não publica nada automaticamente.

A API permite criar um rascunho de serviço, consultar seu conteúdo e salvar alterações. Cada revisão gera um identificador próprio; se alguém tentar salvar uma revisão antiga, a API recusa a alteração. Somente rascunhos aprovados podem ser publicados.

Também existe a rota de extração `POST /api/v1/fontes/{id}/extracoes`, mas ainda não há um provedor de LLM configurado. Por isso, no cenário de demonstração o rascunho é criado manualmente.

## Verificação

O teste de integração em `tests/Api.Tests/IntegrationTests.cs` confere autorização, cadastro, revisão desatualizada e bloqueio de publicação antes da aprovação.

O roteiro `scripts/demo_fluxo_http.py` usa uma fonte pública de Poá sobre Cata Treco, cria e aprova um rascunho e publica o serviço. Foi executado com sucesso no [GitHub Actions](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42721825544).

## Pendente

Construir o formulário e a tela de revisão no painel, configurar a LLM real e verificar as informações dos demais serviços do piloto.
