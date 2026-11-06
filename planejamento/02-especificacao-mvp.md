# Especificação do MVP

## Objetivo

Demonstrar o fluxo: um administrador cadastra uma fonte oficial, a LLM sugere uma wiki estruturada, o administrador revisa e publica, e o cidadão consulta o conhecimento de seu município com indicação da fonte.

## Escopo e usuários

- Piloto: Poá, com coleta de lixo, iluminação pública e Cata Treco.
- Segundo município fictício: um serviço de demonstração para comprovar configuração e isolamento.
- Administrador municipal: cadastra, revisa e publica conhecimento do município autorizado.
- Cidadão: escolhe o município e consulta os serviços, sem conta obrigatória no MVP.
- Cadastro do segundo município e atribuição de administrador: realizados por operador da plataforma ou por configuração inicial de demonstração. Um administrador municipal não cria nem administra municípios de terceiros.

## Jornada administrativa

1. Acessar o painel e autenticar-se.
2. Cadastrar uma fonte: título, URL, texto relevante e data de consulta.
3. Solicitar que a LLM proponha serviço, órgão, etapas, requisitos, canais e relações com a fonte.
4. Revisar o rascunho, corrigir ou remover sugestões e registrar a revisão.
5. Publicar a versão aprovada e verificar o serviço na wiki.
6. Editar novamente e publicar outra versão, preservando o histórico básico.

O texto da fonte será colado pelo administrador. Importação automática de URL ou PDF é evolução opcional. Cada informação publicada deve manter referência à fonte que a sustenta; ausência de evidência deve permanecer explícita.

## Jornada do cidadão

1. Selecionar o município no aplicativo.
2. Fazer uma pergunta em linguagem natural.
3. Receber uma orientação baseada somente no conhecimento publicado daquele município, com os links das fontes e data de consulta ou revisão disponível.
4. Abrir o canal oficial para continuar o atendimento.
5. Consultar o histórico local no dispositivo.

Se a base não tiver evidência suficiente, o sistema informa a limitação. Não inventar prazos, requisitos, horários, disponibilidade ou canais. O MVP não registra solicitações, não acompanha protocolos municipais e não realiza agendamentos.

## Histórias e critérios de aceite

### Cadastro de fonte

Como administrador, quero incluir uma fonte oficial para que ela possa originar conhecimento revisável.

- A fonte tem município, título, URL, texto e data de consulta.
- A fonte não se torna conteúdo publicado automaticamente.

### Proposta de conhecimento

Como administrador, quero que a LLM sugira a estrutura de um serviço para reduzir o trabalho de organização.

- A proposta é salva como rascunho editável.
- Serviços, órgãos, etapas, requisitos e canais sugeridos ficam associados à fonte ou ao trecho de suporte.
- Uma sugestão sem evidência não é publicada como fato.

### Revisão e publicação

Como administrador, quero corrigir e publicar o conhecimento para disponibilizar uma versão confiável.

- Somente administrador autorizado para o município pode editar ou publicar.
- Uma falha de gravação no grafo mantém a versão anterior disponível.
- Uma nova publicação preserva o histórico básico de versões.

### Consulta

Como cidadão, quero descrever uma necessidade e receber orientação para continuar no canal oficial.

- A resposta utiliza somente conteúdo publicado do município escolhido.
- A resposta apresenta fontes oficiais correspondentes.
- Quando a base não sustenta a resposta, o assistente informa a insuficiência e, se conhecido, indica o canal oficial.

### Separação de municípios

Como prefeitura, quero que meus dados sejam separados dos de outras prefeituras.

- O acesso administrativo é restrito pelo servidor ao município autorizado.
- A consulta pública usa o identificador do município selecionado.
- Os cenários de demonstração verificam que conteúdo de um município não aparece no outro.

## Critérios de conclusão do MVP

1. Fonte pública cadastrada e proposta gerada pela LLM.
2. Administrador revisa e publica conteúdo com referência à fonte.
3. Os três serviços de Poá podem ser consultados no aplicativo.
4. Respostas mostram fontes e não inventam dados ausentes.
5. Uma atualização publicada aparece na consulta, preservando o histórico.
6. Um segundo município de demonstração funciona com dados separados.
7. O aplicativo guarda histórico local.
8. API documentada e execução descrita no README.

O cumprimento das disciplinas também exige as entregas e evidências de [entregas por disciplina](06-entregas-por-disciplina.md), além do funcionamento do produto.
