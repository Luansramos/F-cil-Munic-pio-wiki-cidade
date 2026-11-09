# Grafo e publicação

**Status:** implementado e testado na API.

**Backlog:** C1–C4 e H3.

## Implementação

Os dados administrativos ficam no PostgreSQL. Quando um rascunho é aprovado, `PublicationService` monta a nova versão do serviço e grava o conteúdo no Neo4j com município, versão e fonte.

A API só ativa a nova versão no PostgreSQL depois de conseguir gravar o grafo. Dessa forma, se a gravação falhar, a versão anterior continua disponível ao cidadão. O mesmo rascunho aprovado também pode ser publicado novamente sem gerar uma segunda versão.

No Neo4j, as consultas usam o município e a versão ativa. Isso evita que um cidadão receba serviços de outra cidade ou conteúdo ainda em revisão.

## Testes realizados

Os testes cobrem:

- bloqueio de rascunho não aprovado;
- repetição de publicação;
- manutenção da versão anterior após falha simulada no grafo;
- isolamento entre municípios;
- leitura do serviço e de sua fonte após publicação.

Esses testes passaram no [GitHub Actions](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/actions/runs/42718137116), com PostgreSQL e Neo4j. O cenário HTTP de Cata Treco também confirmou publicação e consulta.

## Limitação conhecida

Não há transação distribuída entre os bancos. Uma falha pode deixar uma versão órfã no Neo4j, mas ela não fica ativa nem aparece nas consultas públicas. Limpeza automática dessas versões fica para uma melhoria posterior.
