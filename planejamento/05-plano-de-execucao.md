# Plano de execução

O grupo deseja iniciar o desenvolvimento amanhã e concluir em menos de uma semana. Os dias abaixo são relativos ao início do desenvolvimento, não datas já confirmadas.

## Distribuição sugerida

| Pessoa | Frente principal | Disciplinas |
|---|---|---|
| P1 | API, domínio, PostgreSQL e integração do grafo | C# e SOA |
| P2 | App mobile e experiência do cidadão | Mobile Development & IoT |
| P3 | LLMWiki, fontes e experimento de ML | Inteligência Artificial & Machine Learning |
| P4 | Infraestrutura, segurança e IoT físico | Operating Systems, Cybersecurity e Physical Computing |
| P5 | Painel, requisitos, qualidade e evidências | Testing, Compliance & Quality Assurance |

Os integrantes e disponibilidade ainda não foram informados. P4 concentra três disciplinas: P1 apoia a integração de segurança e IoT com a API, e P5 apoia documentação e evidências. P3 começa a identificar os dados de ML no primeiro dia.

## Marcos

| Dia | Resultado esperado |
|---|---|
| 1 | Contratos de API e modelo compartilhados; bancos iniciados; telas e API em paralelo; dados de ML pesquisados; enunciado de Cybersecurity e viabilidade Wokwi verificados. |
| 2 | Um serviço percorre fonte → rascunho → publicação no grafo; painel e app usam o contrato combinado. |
| 3 | Primeira pergunta no app recebe resposta baseada no conhecimento publicado e mostra fonte. |
| 4 | Três serviços e segundo município configurados; segurança, Linux e IoT com demonstrações iniciais. |
| 5 | Integração, testes exigidos, documentação e evidências finalizadas. |
| 6 | Vídeos, revisão de cada disciplina e preparação dos pacotes e links. |

O marco crítico é o fluxo integrado do dia 3. Se atrasar, suspender melhorias opcionais e concentrar pessoas na integração, preservando o trabalho obrigatório de ML e segurança.

## Primeiras atividades em paralelo

- **P1:** definir contrato junto de P3/P5, subir API e bancos.
- **P2:** preparar app e telas com dados de exemplo no contrato combinado.
- **P3:** localizar dados reais de ML, avaliar acesso à LLM e estruturar extração da fonte.
- **P4:** conferir código-base de Cybersecurity, começar Linux e verificar comunicação possível do Wokwi.
- **P5:** registrar requisitos no Azure Boards e preparar painel com dados de exemplo.

## Dependências críticas

1. Contrato comum permite desenvolver API, painel, app e LLM em paralelo.
2. Uma fonte revisada e o acesso à LLM permitem criar o primeiro rascunho.
3. Publicação e consulta ao grafo permitem a primeira resposta fundamentada no app.
4. Dados públicos adequados são indispensáveis para o experimento de ML.
5. Código e instruções da atividade de Cybersecurity são necessários para completar o módulo pedido.
6. Comunicação Wokwi/aplicação precisa ser comprovada antes de gravar a demonstração final.

## Riscos e resposta

| Risco | Resposta planejada |
|---|---|
| Dados de ML inadequados ou indisponíveis | Priorizar a pesquisa no dia 1 e ajustar a pergunta ao conjunto real encontrado; não usar dados simulados. |
| Acesso à LLM indisponível | Decidir provedor e testar acesso no dia 1; desenvolver interfaces com resposta de exemplo enquanto o acesso é resolvido. |
| Falha entre PostgreSQL e Neo4j | Publicar com versão pendente, gravação repetível e ativação somente após sucesso. |
| Fontes incompletas | Registrar fonte e data; não gerar detalhes sem suporte. |
| P4 sobrecarregado | P1 apoia integrações; P5 ajuda nas evidências; ajustar tarefas após verificar disponibilidade real. |
| Wokwi sem integração viável | Registrar o resultado e ajustar a solução com base nas possibilidades reais e nos critérios da disciplina. |

## Acompanhamento

Usar o backlog com estados **A fazer → Em andamento → Em revisão → Concluído**. Uma tarefa só passa para concluída quando o responsável apresenta o resultado e satisfaz o critério de aceite. Manter bloqueios, decisões e evidências visíveis para o grupo.
