Implemente a entrega de Cybersecurity do projeto Fácil Município — Wiki Cidade.

REPOSITÓRIO
https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade

BASE
A implementação deve partir de feat/soa-csharp-api.
Crie uma branch local feature/cybersecurity, preservando alterações existentes.
Não faça push, commit, abra PR, realize merge ou deploy.

CONTEXTO
O projeto é uma CitizenTech acadêmica da FIAP para consulta de serviços
municipais, com API ASP.NET Core, PostgreSQL, Neo4j e aplicativo Expo.

A entrega de Cybersecurity exige:
1. Identificação e marcação de PII.
2. Taint Analysis: rastrear dados sensíveis da entrada até as saídas e
   bloquear uso inseguro.
3. Integração real com IA generativa para explicar eventos técnicos em
   linguagem acessível.
4. Dashboard com indicador “Risco Cidadão”.
5. Evidências e documentação para entrega em PDF separado do código,
   além de vídeo do dashboard.

Esta implementação não depende do treinamento de modelo da disciplina
de IA & Machine Learning nem da LLM da conversa estar pronta.

ANTES DE ALTERAR
- Leia AGENTS.md e eventuais instruções locais.
- Leia README, especificação do MVP, arquitetura, contratos, backlog e
  docs/features/seguranca.md.
- Confira o estado atual da branch e os consumidores.
- Inspecione PrivacyFilter, ConversationService, PublicController,
  AdminController, PublicationService, Program.cs, contratos, entidades,
  AppDbContext e testes.
- Verifique se já existe painel ou integração de IA aproveitável.
- Execute as verificações disponíveis para estabelecer o estado inicial.
- Não sobrescreva trabalho de outros integrantes.
- Registre o plano curto e prossiga com a implementação.

ESCOPO E ARQUITETURA
- Reutilize a API C#, PostgreSQL, JWT e autorização por município.
- Preserve Neo4j, revisão humana e publicação versionada.
- Não crie outra API ou microsserviço.
- Não altere o contrato mobile sem necessidade.
- Use poucas classes com responsabilidades claras e padrões existentes.
- Não crie interfaces, dependências ou arquivos sem utilidade concreta.
- Documente os limites reais da proteção.

1. PARSING DE PII

Evolua o PrivacyFilter existente para retornar um resultado estruturado:
- texto sanitizado;
- categorias detectadas;
- origem/caminho do campo;
- ação aplicada;
- informações necessárias para rastreamento durante a requisição.

Cubra:
- CPF formatado e não formatado;
- RG, usando contexto para reduzir falsos positivos;
- e-mail, preservando a proteção existente;
- senhas em campos identificados e padrões textuais;
- informações pessoais de saúde em exemplos contextuais documentados.

Não trate toda menção a saúde, hospital, documento ou senha como dado
pessoal efetivamente informado.
Exemplo: “Como alterar minha senha?” deve continuar sendo uma pergunta
normal; não contém, por si só, uma senha.

Regex e regras contextuais não detectam qualquer dado pessoal possível.
Documente limitações e exemplos de falsos positivos/negativos.

Nunca persista os valores originais detectados.
Evite ToString, serialização ou logging acidental dos objetos que
carregam conteúdo original.

2. ENTRADAS E POLÍTICAS

Instrumente:
- pergunta da conversa;
- pergunta e resposta de cada item do histórico;
- texto e demais campos relevantes das fontes;
- campos textuais de rascunhos e conteúdo destinado à publicação.

Analise a entrada da conversa antes dos retornos antecipados por ausência
de conhecimento publicado, sem registrar eventos para município inválido.

Defina políticas por finalidade:
- Conversa: remover informações pessoais desnecessárias e continuar com
  o conteúdo seguro quando possível.
- Fontes e rascunhos: rejeitar conteúdo pessoal indevido antes da
  persistência/publicação, com mensagem segura.
- Login: preservar a senha original somente para verificação.
- Cadastro de usuário: preservar a senha somente para geração do hash.

Não aplique substituição global de campos de senha no JSON:
isso quebraria autenticação e cadastro.
E-mail administrativo legítimo também precisa de política própria.

3. TAINT ANALYSIS E CONTROLE DAS SAÍDAS

Implemente rastreamento explícito nos caminhos cobertos:
source → marcação → tratamento → verificação do sink.

Destinos a proteger:
- logs da aplicação;
- chamadas à LLM;
- respostas HTTP públicas;
- persistência de conteúdo que não deve conter PII;
- publicação no grafo.

Preserve as marcações ao transformar os dados.
Não considere um simples método de substituição de strings como prova
completa de rastreamento.

Crie uma camada de proteção reutilizável que:
- impeça envio de dados marcados sem tratamento;
- permita conteúdo sanitizado conforme a política;
- registre a decisão usando somente metadados;
- produza uma resposta segura quando uma saída for bloqueada.

Verifique também a resposta da LLM, pois ela pode gerar PII que não
estava na entrada.
Considere todos os campos relevantes da resposta pública, e não apenas
o texto principal.

Revise os pontos reais de logging e tratamento de exceções.
Não adicione logs de corpos HTTP, perguntas, históricos, senhas,
prompts completos ou mensagens de exceção potencialmente sensíveis.

Não afirme proteção automática de todo ILogger ou de toda a aplicação.
Documente precisamente quais caminhos foram instrumentados.

4. AUDITORIA NO POSTGRESQL

Adicione entidade e migração para eventos de segurança.

Armazene somente dados como:
- identificador do evento;
- correlação aleatória da operação;
- município;
- data/hora UTC;
- categoria detectada;
- origem e destino em formato controlado;
- ação: sanitizado, bloqueado ou pendente;
- gravidade potencial;
- código técnico do evento;
- estado e resultado da tradução.

Não salve:
- pergunta ou histórico original;
- valor detectado ou trecho do documento;
- senha, token ou corpo HTTP;
- hash de CPF ou outro identificador pessoal;
- IP ou identificador de dispositivo sem necessidade.

Use campos estruturados e valores controlados.
Defina retenção e comportamento em caso de falha da auditoria.
Falha de auditoria não pode liberar dado que deveria ser bloqueado.
Não crie filas ilimitadas nem processamento sem controle de repetição.

5. TRADUÇÃO COM IA GENERATIVA

Crie uma integração própria, por exemplo IRiskTranslator.
Não altere a finalidade de ILlmWiki, responsável pelo conhecimento municipal.

Se houver infraestrutura compatível de IA no repositório, reutilize-a.
Caso contrário, implemente um provedor real por API com HttpClient,
configuração por ambiente, timeout e tratamento de falhas.
Explique no README a configuração necessária.
Não fixe modelo sem conferir a documentação oficial atual do provedor.

O prompt deve receber apenas metadados controlados do evento.
Não envie conteúdo original nem mensagens arbitrárias de exceção.

Solicite:
- português brasileiro;
- no máximo duas frases;
- linguagem acessível;
- descrição fiel da ação;
- nenhuma alegação de ataque, acesso indevido ou vazamento não comprovado.

A IA explica o evento; a classificação de risco vem de regras do código.

Execute a tradução fora do caminho crítico da conversa, respeitando o
orçamento atual de 12 segundos.
Use processamento limitado e evite duplicar traduções.

Se o provedor estiver indisponível:
- mantenha o evento;
- marque tradução pendente ou falhou;
- mostre mensagem padrão identificada como tal;
- não apresente fallback como resposta de IA.

Não solicite que o usuário cole uma chave no chat.
Sem credencial disponível, conclua a implementação e os testes com
provedor simulado e registre que a chamada real continua pendente.
Não declare o requisito de IA real validado sem executá-lo.

6. DASHBOARD

Reutilize um painel existente se houver.
Caso contrário, crie uma única página HTML/CSS/JavaScript servida pela
API, sem introduzir um projeto frontend complexo.

Inclua:
- título “Auditoria de Privacidade — Fácil Município”;
- indicador “Risco Cidadão”;
- resumo de eventos;
- contagem de sanitizações, bloqueios e pendências;
- lista com horário, categoria, origem, destino e ação;
- explicação gerada pela IA;
- identificação de tradução pendente ou mensagem padrão;
- estados de carregamento, vazio e erro;
- interface acessível, com texto além das cores.

Defina e documente o período e a regra do indicador.
Separe gravidade potencial de situação da proteção.
Não contabilize bloqueio bem-sucedido como vazamento confirmado.
Ausência de eventos não deve ser anunciada como garantia de segurança.

Proteja as APIs do painel com JWT e autorização por município:
- ADMIN consulta somente seu município;
- OPERADOR segue a política administrativa existente;
- validação deve ocorrer no backend.

Reutilize o login existente.
Não coloque tokens em URL nem persista credenciais no localStorage.
Se a página estática for pública, ela não pode embutir eventos ou dados
privados; estes devem vir apenas das APIs autorizadas.

7. TESTES E EVIDÊNCIAS

Adicione testes relevantes para:
- categorias e formatos de PII;
- perguntas normais sem falsos bloqueios;
- PII nos dois lados do histórico;
- conteúdo sanitizado recebido pela LLM;
- bloqueio real de sink quando dado marcado está sem tratamento;
- captura de log demonstrando ausência do valor original;
- resposta da LLM com PII;
- proteção antes de persistência e publicação;
- login e hash de senha preservados;
- auditoria e prompt de tradução sem valores originais;
- acesso não autenticado e isolamento municipal;
- indisponibilidade da tradução;
- comportamento normal das conversas.

Demonstre tentativas inseguras com testes, fakes ou ferramentas locais
restritas a desenvolvimento.
Não crie endpoint público de vazamento ou modo inseguro em produção.

Use somente dados simulados.
Diferencie testes automatizados de verificações manuais.
Execute os testes existentes e os novos quando o ambiente permitir.
Não invente resultados, prints ou chamadas reais.

8. DOCUMENTAÇÃO E ENTREGA

Atualize:
- docs/features/seguranca.md;
- contratos dos novos endpoints;
- README com execução, migração, variáveis e dashboard;
- prompts.md;
- backlog e status pertinentes.

Prepare uma documentação técnica contendo:
- relação dos quatro requisitos com o código;
- diagrama do fluxo source → tratamento → sink;
- políticas e limitações;
- comandos de teste e resultados reais;
- cenários para capturas;
- roteiro de vídeo com parsing, bloqueio, tradução e dashboard;
- pendências para entrega.

Não gere prints fictícios.
Se não for possível produzir o PDF no ambiente, entregue a documentação
fonte pronta para conversão e informe essa pendência.

LIMITE DO MOBILE
Mantenha explícito que proteger a API não remove a pergunta original
que o aplicativo pode salvar localmente no SQLite.
Não altere a persistência mobile nesta tarefa; documente essa fronteira.

AO FINAL
Informe:
1. O que foi implementado.
2. Arquivos e integrações afetados.
3. Como executar e demonstrar.
4. Testes executados e resultados.
5. O que foi validado com IA real.
6. Limitações e pendências para PDF, capturas e vídeo.

Revise o diff final e não faça push, PR, merge ou deploy.