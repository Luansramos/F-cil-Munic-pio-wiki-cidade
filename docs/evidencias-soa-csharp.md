# Evidências — SOA e C#

Aqui estão os registros das verificações da API do Fácil Município. Os prints e os arquivos JSON foram obtidos da execução do GitHub Actions, não de telas simuladas do aplicativo. O Swagger também identifica a autenticação Bearer JWT nas rotas administrativas, mantendo públicas as consultas do cidadão.

## Resultado dos testes

- **Execução:** [GitHub Actions — 09/11/2026](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42722547143).
- **Resultado:** compilação e verificação de migrações concluídas; 37 testes aprovados, sem falhas.
- **Ambiente de testes:** PostgreSQL 17 e Neo4j 5.26 Community.
- **Arquivos:** resultados dos testes, OpenAPI do Swagger, respostas HTTP e capturas do navegador ficam nos artefatos da execução.

## Demonstração com uma fonte de Poá

O script [`scripts/demo_fluxo_http.py`](../scripts/demo_fluxo_http.py) usa uma [notícia da Prefeitura de Poá sobre o Cata Treco](https://poa.sp.gov.br/programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/) para testar o caminho da informação dentro do backend.

O roteiro faz login com um operador de teste, cadastra a fonte, cria e aprova um rascunho manual, publica o serviço no Neo4j e pergunta como solicitar a retirada de um sofá. Ele confere se a resposta menciona a orientação cadastrada e se apresenta o link da fonte e o canal oficial.

Também verifica que:
- um rascunho não aprovado não é disponibilizado;
- publicar novamente não cria outra versão;
- uma pergunta sem informação suficiente não recebe uma afirmação inventada;
- um município não recebe o conteúdo de outro.

O cenário terminou com sucesso, assim como os 37 testes automatizados. Seu resultado é salvo como `demo-fluxo-http.json`.

## Onde encontrar cada requisito

| Disciplina | Requisito | Código ou evidência |
|---|---|---|
| SOA | Domínio | `apps/api/Domain/Entities.cs` e `Services/PublicationService.cs` |
| SOA | Banco de dados | `apps/api/Data/AppDbContext.cs` |
| SOA | Migrações | `apps/api/Data/Migrations/` |
| SOA | Documentação automática | `/swagger` e `/swagger/v1/swagger.json` |
| SOA | Testes | `tests/Api.Tests/` e execução no GitHub Actions |
| C# | POO e interfaces | `Domain/` e `Services/Interfaces.cs` |
| C# | Web API | `Controllers/` |
| C# | EF Core e LINQ | `Data/AppDbContext.cs` e consultas dos serviços |
| C# | Programação assíncrona | `ConversationService.cs`, `PublicationService.cs` e controllers |
| C# | Organização e qualidade | Separação em camadas, tratamento de erros, validação e testes |

## O que as evidências não mostram

O roteiro usa uma rotina de resposta demonstrativa e cria o rascunho manualmente. Ele não demonstra extração por LLM real, interface React nem conversa realizada dentro do Expo. Essas integrações ainda precisam ser testadas pelo grupo.

As imagens do protótipo do aplicativo são referências de design e não devem ser usadas como prova de uma tela executada. As capturas da API podem ser usadas na apresentação de SOA e C#, com a indicação de que foram feitas no ambiente de CI.
