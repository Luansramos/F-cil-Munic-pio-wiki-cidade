# Feature — Aplicativo mobile do cidadão (Expo)

- Status: em desenvolvimento (tarefas 1+2, 3 e 4+5 commitadas em `09380e5`, sem PR; tarefa 6 implementada no working tree, sem commit)
- Responsável e colaboradores: Matheus (mobile e Cybersecurity); API P1; LLM P3
- Itens do backlog / issue: D4, D5, D6, D7, D8 (apoio em D1–D3 para o contrato)
- Componentes e disciplinas afetados: `apps/mobile`; Mobile Development & IoT (integração com a API de SOA e C#)
- Última atualização: 07/11/2026
- Observação: o backlog (`planejamento/04-backlog.md`) e `consulta-e-aplicativo.md` atribuem o app a P1/P2. O responsável pelo mobile é Matheus. A atualização desses documentos será combinada com o grupo.

## Problema e objetivo

O app é a porta de entrada do cidadão. Ele escolhe o município e conversa em linguagem natural: faz uma pergunta, recebe uma orientação baseada somente no conhecimento publicado daquele município, com as fontes e o canal oficial, e pode continuar perguntando com base no que já foi dito. Se a base não tiver evidência suficiente, o app informa isso em vez de inventar.

O app precisa ser simples e acessível para quem não é especialista e continuar útil com conectividade ruim, conforme o briefing da GS.

## Escopo e limites

Dentro do escopo:

- Navegação por três abas (Início, Histórico, Preferências) e uma tela de conversa aberta a partir do Início ou do Histórico.
- Seleção de município e conversa com continuidade: as perguntas seguintes consideram as trocas anteriores da mesma conversa.
- Painel de fontes dentro da conversa, com título, link e data quando houver.
- Histórico local em SQLite, com conversas completas, que persiste após reiniciar o app e funciona sem internet.
- Preferências: tamanho do texto, informações de privacidade, identificação do modo demonstrativo e apagar todo o histórico com confirmação.
- Estados de carregamento, erro, sem conexão, vazio e informação insuficiente.
- Acessibilidade básica.

Fora do escopo (conforme a especificação do MVP):

- Conta ou login do cidadão. O único login do produto é o do administrador, que não existe no app.
- Registrar solicitações, acompanhar protocolos ou agendar atendimento.
- Credenciais da LLM ou de banco no cliente.
- Qualquer integração real com sistemas da prefeitura.

## Estado atual do repositório

`apps/mobile` existe desde 07/11/2026 (branch `feat/mobile-scaffold-theme`) com o esqueleto do projeto, `theme/`, `types/` e `config/`, o componente `Icon`, a camada de API com dados mockados (`services/api/`), testes com Jest e a tarefa 4+5: componentes base, navegação por três abas com o Expo Router, contexto de município, hooks de carregamento e a tela de Início completa sobre o mock. Histórico, Preferências e Conversa existem só como telas provisórias. A tarefa 6 acrescentou o banco local SQLite (`services/banco/`), usado hoje só para guardar o município escolhido; os repositórios de conversas e do nível de texto existem e têm testes, mas nenhuma tela os usa ainda. Ainda não há conversa funcional, histórico real nem tela de preferências. A API não existe: nenhuma rota, esquema ou versão dela foi confirmada. Os tipos em `types/` seguem a proposta deste documento e continuam a acordar com P1.

### Versões fixadas (SDK 58)

Criado com `create-expo-app@5.0.0` (template padrão do SDK 58), Node v22.22.0 e npm 10.9.4. Versões instaladas, lidas de `apps/mobile/package.json`:

| Pacote | Versão |
|---|---|
| `expo` | ~58.0.23 |
| `expo-router` | ~58.0.30 |
| `expo-sqlite` | ~58.0.15 |
| `react` | 19.3.0 |
| `react-native` | 0.88.1 |
| `expo-constants`, `expo-linking`, `expo-splash-screen`, `expo-status-bar`, `expo-system-ui` | ~58.0.x (conforme `package.json`) |
| `react-native-safe-area-context` | ~5.11.1 |
| `react-native-screens` | ~4.30.1 |
| `@expo/vector-icons`, `expo-font` | ^15.0.2, ~58.0.11 (ícones; o pacote precisa do `expo-font`) |
| `react-dom`, `react-native-web` | 19.3.0, ^0.21.4 (alvo web de desenvolvimento) |
| `jest`, `jest-expo`, `@types/jest` (dev) | ~30.5.2, ~58.0.14, 30.0.0 |
| `typescript` | ~6.0.3 |
| `eslint` / `eslint-config-expo` | ^9.39.5 / ~58.0.4 |

Compatibilidade com o Expo Go: a API pública de versões da Expo (`api.expo.dev/v2/versions/latest`) lista o SDK 58 com Expo Go 58.0.7 (iOS) e 58.0.5 (Android). Essa consulta foi feita em 07/11/2026; a versão do Expo Go instalada no celular de teste ainda precisa ser conferida.

Node.js: o `package.json` declara em `engines` `^22.13.0 || ^24.3.0 || ^26.0.0 || >=27.0.0`, que é a faixa exigida por `expo@58.0.23` (lida com `npm view expo@58.0.23 engines.node`); `react-native@0.88.1` exige `^22.13.0 || ^24.3.0 || >= 26.0.0`. O mínimo é 22.13.0. O `node:sqlite` usado nos testes também depende dessa versão sem flag. Somente a 22.22.0 foi executada.

Adicionar bibliotecas sempre com `npx expo install <pacote>`, para resolver a versão compatível com o SDK.

### Ajustes feitos sobre o template

- Estrutura reorganizada para a raiz de `apps/mobile` (sem `src/`), conforme a arquitetura abaixo. O alias `@/` aponta para a raiz.
- Removidos itens que o template trazia e que o projeto não usa: `AGENTS.md` local (apontava para `src/app` e conflita com esta estrutura), `.claude/settings.json` (habilitava um plugin de terceiros), `.vscode/`, `LICENSE`, README genérico, script `reset-project` e as dependências `@expo/ui`, `expo-glass-effect`, `expo-symbols`, `expo-device`, `expo-image`, `expo-web-browser`, `react-native-gesture-handler`, `react-native-reanimated` e `react-native-worklets`. `expo-doctor` não apontou problema depois da remoção.
- `app.json`: nome "Fácil Município", slug `facil-municipio`, esquema `facilmunicipio`, `userInterfaceStyle` fixo em `light` (o mockup só tem tema claro e o contraste do escuro não foi verificado) e `reactCompiler` desativado, para reduzir o comportamento implícito.
- Os ícones e a tela de abertura ainda são os do template. Falta definir os próprios.

## Protótipo visual

O mockup está publicado como canvas de design em https://claude.ai/artifact/Ut2aU2sUC4QR3Fz1BJedd2 e exportado em `docs/features/img/` (PNG a 390 px, 1x). Ele é a referência de layout:

| Arquivo | Tela |
|---|---|
| `01 · Início@1x.png` | Início |
| `02 · Conversa (resposta com fonte)@1x.png` | Conversa com resposta e fonte |
| `03 · Painel de fontes (dentro da conversa)@1x.png` | Painel de fontes sobre a conversa |
| `04 · Conversa sem informação suficiente@1x.png` | Conversa com `informacaoSuficiente` falso |
| `05 · Histórico@1x.png` | Histórico, com a faixa "Disponível sem internet" |
| `06 · Preferências@1x.png` | Preferências |

Tokens extraídos do mockup para `theme/`: fundo `#F7FBFA`, texto `#12201D`, texto secundário `#3D4F4A`, verde principal `#0B6B5E`, perigo `#B42318`, as superfícies e bordas em tons de verde claro, o aviso âmbar da informação insuficiente (fundo `#FFF4E0`, borda `#E3B866`, ícone `#6B4300`, título `#4A2E00`, texto `#3A2500`), o fundo escuro do painel de fontes (`rgba(18, 32, 29, 0.55)`) e a alça do painel (`#B7CCC6`). Contrastes do âmbar medidos: título 11,46:1, texto 13,34:1 e ícone 7,94:1 sobre `#FFF4E0`. As bordas do âmbar e a alça (1,7:1) são só decoração, e o aviso também tem ícone e título. O mockup usa a fonte Inter; o app usa a fonte do sistema, para não depender de pacote de fontes. Os ícones do app vêm do `@expo/vector-icons` (item 9 abaixo).

Divergência registrada: a borda do campo de pergunta no mockup (`#8FB3AB`) tem contraste de 2,19:1 sobre o fundo, abaixo dos 3:1 que o WCAG 1.4.11 pede para componentes de interface. O token `borderStrong` usa `#5F8D82` (3,58:1 sobre o fundo, 3,74:1 sobre branco e 3,26:1 sobre `#E3F3EF`). O mesmo token vale para a borda do botão A− em Preferências. O texto das respostas no mockup é exemplo. Nenhum conteúdo dele deve ser tratado como informação real de Poá. O mockup mostra uma única troca por tela, mas a tela de conversa deve acomodar várias trocas em sequência.

### Divergências e lacunas do mockup

Conferido em 07/11/2026 contra este documento e o contrato. Itens marcados "decidir" não têm resposta e não foram implementados. Os demais registram a decisão tomada.

1. **Canal oficial sem origem no contrato (decidido para o mock, a acordar com P1).** O botão "Abrir canal oficial" da tela 04 precisa de uma URL, mas `RespostaConversa` não tinha campo para isso. `types/contrato.ts` ganhou o campo opcional `canalOficialUrl?: string`, que é compatível com o contrato original. O mock nunca o preenche (sem link): a interface deve tratar a ausência sem inventar URL e sem navegação. O nome e a existência do campo ainda dependem de P1.
2. **Cartão "Fonte oficial" (decidir).** O mockup mostra "Consultar orientação ↗" além de "Ver fontes". Assume-se que o link abre `fontes[0]`, mas o mockup não diz. No mock, as fontes não têm URL, então o cartão não terá destino por enquanto.
3. **Lista numerada na resposta (decidido para o mock).** `resposta` é uma string e o mock envia os passos como texto com quebras de linha (`1. ...`). A interface renderiza o texto recebido. Se o contrato ganhar estrutura de lista, isso muda.
4. **"Nova conversa" no cabeçalho.** A especificação desta feature pede a ação, mas o mockup não a desenha. Decidir se entra e onde.
5. **Estados sem desenho:** carregando, erro, sem conexão na Conversa e lista vazia no Histórico. Serão compostos com os componentes de aviso do mockup, a validar.
6. **Filtro de município no Histórico.** O mockup mostra um seletor com "Poá · SP". Decidir se existe a opção "Todos".
7. **Botões A− e A+.** No mockup o A+ aparece destacado. Com os níveis 0 a 3, o estado destacado deve indicar que o botão está desativado no limite, não "selecionado". Confirmar.
8. **Anotações do mockup que não são texto do app.** "Exemplo de resposta para o protótipo.", "O botão só aparece quando o canal oficial é conhecido." e "A data aparece só quando a fonte a informa." explicam regras do desenho e não são renderizadas. A primeira, se o grupo quiser sinalizar conteúdo de exemplo, já é coberta pelo selo "Demonstração".
9. **Ícones (decidido).** Os do mockup são traços no estilo Lucide. Em vez de SVG próprio, o app usa a família `MaterialCommunityIcons` do `@expo/vector-icons` (consta nos módulos empacotados do SDK 58). Os 20 glifos necessários, inclusive o sofá (`sofa-outline`), existem na família; foi conferido no mapa de glifos instalado. O componente `components/Icon.tsx` expõe nomes semânticos em português e é decorativo para leitores de tela (o rótulo fica no botão que o contém). Custo: a fonte da família tem 1,3 MB no bundle. Os traços não são idênticos aos do mockup.
10. **Datas no Histórico** ("Hoje, 10:32", "Ontem, 16:20", "03 nov, 09:15"): função pura em `utils/`, em pt-BR.
11. **URLs no painel de fontes** aparecem sem o esquema (`poa.sp.gov.br/...`). A exibição remove `https://`, mas o link abre a URL completa.

## Telas e navegação

Roteamento com Expo Router. Abas inferiores: **Início**, **Histórico** e **Preferências**. A tela de **Conversa** fica fora das abas, abre a partir do Início ou do Histórico e volta para a origem pela seta do cabeçalho.

### Início

- Título "Como podemos ajudar?" e texto de apoio.
- Seletor de município ("Seu município"). Ao tocar, abre uma lista vinda de `GET /municipios` (sheet ou modal, sem tela própria). A escolha é salva localmente.
- Botão "Iniciar conversa" abre uma conversa nova no município escolhido.
- Lista de serviços do município, vinda de `GET /municipios/{id}/servicos`. Tocar em um serviço abre uma conversa nova com a pergunta sugerida preenchida e editável, sem enviar sozinha. Não usar o rótulo "mais procurados", porque o contrato não tem dado de uso. Usar "Serviços disponíveis".
- Aviso fixo: "Você será orientado ao canal oficial."
- Estados: carregando, erro ao buscar municípios ou serviços, lista vazia.

### Conversa

- Cabeçalho com seta de voltar, título, município e selo "Demonstração". Ação "Nova conversa" no cabeçalho.
- Sequência de trocas: cada pergunta do cidadão seguida da resposta. A lista rola, e a mais recente fica visível ao enviar.
- Cada resposta mostra o texto, os serviços relacionados, o cartão de fonte oficial e "Ver fontes". O painel de fontes abre sobre a conversa (bottom sheet) e mostra título, link e data (quando houver) das fontes **daquela resposta**.
- Campo de pergunta com botão de enviar e limite de tamanho do texto. Texto auxiliar curto: não digite CPF, senhas ou dados de saúde.
- Quando `informacaoSuficiente` for falso, a resposta mostra aviso claro de que a base não tem informação suficiente e, se houver, o botão do canal oficial. A conversa continua possível.
- O município é fixo dentro de uma conversa. Para trocar de município, o usuário inicia uma nova conversa.
- Ao reabrir uma conversa do Histórico, todas as trocas aparecem e o campo de pergunta permite continuar.
- Estados: enviando, erro de rede com o texto digitado preservado, sem conexão (a conversa salva continua legível e o envio fica desativado com mensagem clara).

### Histórico

- Texto "Suas conversas salvas neste celular." e filtro por município.
- Cada item é uma conversa: título (a primeira pergunta, encurtada), data e hora da última troca, uma seta para abrir e um botão de apagar. Apagar uma conversa pede confirmação.
- Faixa "Disponível sem internet" exibida somente quando não houver conexão.
- Estado vazio quando não houver conversas.

### Preferências

- **Acessibilidade:** controle de tamanho do texto (A− e A+) com um texto de exemplo que reflete a mudança. O app respeita também a escala de fonte do sistema.
- **Privacidade:** "Como usamos seus dados" abre uma explicação curta (sheet ou modal) e "Apagar todo o histórico" pede confirmação antes de executar.
- **Sobre o aplicativo:** "Modo demonstrativo" como indicador somente de leitura, explicando que é um protótipo acadêmico, com conteúdo de fontes públicas e sem parceria com a prefeitura. A nota "Este aplicativo orienta. A solicitação é feita no canal oficial." fica visível.

## Fluxo e contratos de integração

Fluxo: escolher município → perguntar → o app envia a pergunta e as últimas trocas da conversa → API consulta o grafo do município e a LLM com esse contexto → app exibe a resposta com fontes → grava a troca na conversa local.

Rotas que o app consome (propostas em `docs/contratos-de-integracao.md`, ainda não implementadas):

| Método e rota | Uso no app |
|---|---|
| `GET /municipios` | Seletor de município |
| `GET /municipios/{id}/servicos` | Lista de serviços no Início |
| `POST /municipios/{id}/conversas` | Enviar pergunta (com contexto opcional) e receber a resposta |

Contrato mínimo da resposta, já definido na arquitetura: `resposta`, `servicosRelacionados`, `fontes` (título, URL e data) e `informacaoSuficiente`.

**Continuidade da conversa (proposta a acordar com P1 e P3 antes de implementar).** O envio ganha um campo opcional `historico`, com as últimas trocas (pergunta e resposta) da conversa atual. Pontos da proposta:

- A API continua sem guardar estado. O histórico completo fica só no celular.
- O campo é opcional, então o contrato original continua válido e o mock pode ignorá-lo.
- O app envia no máximo as últimas 3 trocas e corta textos longos, para limitar custo e tamanho do pedido.
- O contexto vale só dentro da mesma conversa e do mesmo município, para não misturar conhecimento de outro município.
- A API e a LLM precisam usar esse contexto para interpretar perguntas de seguimento (por exemplo "e se for domingo?"). Sem isso o campo não adianta.
- O filtro de PII (Cybersecurity) deve atuar também sobre `historico`, não só sobre a pergunta atual.

Formato proposto em TypeScript, **a acordar com P1 antes de implementar**:

```ts
type Municipio = { id: string; nome: string; uf: string };

type Servico = { id: string; nome: string };

type Fonte = { titulo: string; url: string; dataConsulta?: string };

type RespostaConversa = {
  resposta: string;
  servicosRelacionados: string[];
  fontes: Fonte[];
  informacaoSuficiente: boolean;
};

type TrocaResumida = { pergunta: string; resposta: string };

type EnvioPergunta = { pergunta: string; historico?: TrocaResumida[] };

type Troca = RespostaConversa & { id: string; pergunta: string; criadoEm: string };

type Conversa = {
  id: string;
  municipioId: string;
  titulo: string;
  trocas: Troca[];
  atualizadoEm: string;
};
```

Tipos de erro e paginação ainda não estão definidos.

## Arquitetura proposta (confirmar antes de codar)

Roteamento com Expo Router e organização por responsabilidade, seguindo a ordem que o professor usou nos trabalhos anteriores de mobile: TypeScript, hooks, services, componentização, organização e limpeza. Pastas na raiz de `apps/mobile`, ao lado de `app/`:

- `app/`: rotas do Expo Router. Cada arquivo é fino e apenas renderiza uma tela de `screens/`.
- `screens/`: composição das telas (Início, Conversa, Histórico, Preferências). Sem regra de negócio nem acesso direto a API ou banco.
- `components/`: peças reutilizáveis de interface, sem lógica de negócio.
- `hooks/`: lógica de estado e casos de uso das telas.
- `services/`: acesso externo (API e SQLite).
- `contexts/`: estado compartilhado (município escolhido e preferências).
- `theme/`: cores, espaçamentos, tipografia e escala de fonte.
- `types/`: tipos do contrato e do domínio.
- `config/`: URL da API e flag de mock, lidas de variáveis de ambiente.
- `utils/`: funções puras (formatação de data, corte de texto, montagem do `historico`).

A pasta `navigation/` do repositório do professor não entra, porque o Expo Router assume a navegação. Se o professor esperar essa pasta, confirmar com ele antes. Foi consultada apenas a listagem de pastas dos trabalhos anteriores, não o código nem o README.

### Princípio de reutilização

- Toda interface é montada a partir de componentes. Uma tela só compõe componentes e liga hooks; não repete markup nem estilo.
- Antes de criar um componente novo, procurar um existente que sirva. Se um componente quase serve, generalizar com propriedades em vez de duplicar.
- Estilos usam os tokens de `theme/`. Nada de cor, espaçamento ou tamanho de fonte solto nas telas.
- Regra de negócio fica em `hooks/`, acesso externo em `services/`, tipos em `types/`.

Componentes previstos, a validar ao implementar:

| Componente | Onde é reutilizado |
|---|---|
| Container de tela e cabeçalho | Todas as telas |
| Botão (primário, secundário, perigo) | Início, Conversa, Preferências |
| Linha com ícone, texto e seta | Serviços no Início, conversas no Histórico, opções em Preferências |
| Seção com título | Início, Preferências |
| Aviso informativo e faixa de estado (offline, erro, vazio) | Início, Conversa, Histórico, Preferências |
| Selo (Demonstração, Ativo) | Conversa, Preferências |
| Balão de mensagem (cidadão e assistente) | Conversa |
| Cartão de fonte e painel de fontes | Conversa |
| Campo de pergunta com envio | Conversa |
| Diálogo de confirmação | Apagar conversa, apagar tudo |
| Controle de tamanho do texto | Preferências |

### Hooks e services previstos

- `services/`: cliente da API com uma interface única e duas implementações (real e mock, com o mesmo contrato, escolhidas por `config/`); acesso ao SQLite com migração inicial; repositório de conversas; armazenamento de preferências.
- `hooks/`: `useMunicipios`, `useServicos`, `useConversa` (enviar pergunta, montar o `historico`, gravar a troca), `useHistorico` (listar, apagar uma, apagar tudo), `usePreferencias`, e um hook para o estado de conexão.
- `contexts/`: município escolhido e preferências (tamanho do texto), persistidos localmente.

### Banco local

Esquema versão 1 (`PRAGMA user_version`), implementado em `services/banco/migracoes.ts`:

- `conversas`: `id` (PK), `municipio_id`, `titulo`, `criado_em`, `atualizado_em` (ISO 8601 UTC), com índice em (`municipio_id`, `atualizado_em` DESC). Sem chave estrangeira para município, porque os municípios vêm da API e não são guardados.
- `trocas`: `id` (PK), `conversa_id` (FK para `conversas`, `ON DELETE CASCADE`), `ordem`, `pergunta`, `resposta`, `fontes_json`, `servicos_relacionados_json`, `informacao_suficiente` (0 ou 1, com `CHECK`), `canal_oficial_url` (nulo), `criado_em`, e `UNIQUE (conversa_id, ordem)`.
- `preferencias`: `chave` (PK) e `valor`. Chaves usadas: `municipio_id` e `nivel_texto` (0 a 3). Decidido: não usar `AsyncStorage`, para não adicionar dependência.

Apagar uma conversa remove as suas trocas; apagar tudo remove conversas e trocas e preserva as preferências.

Regras:

- Os repositórios recebem `Pick<SQLiteDatabase, 'execAsync' | 'runAsync' | 'getAllAsync' | 'getFirstAsync' | 'withTransactionAsync'>` do próprio `expo-sqlite`. Não há interface própria nem adaptador de produção. Nos testes, um adaptador sobre `node:sqlite` implementa esses mesmos cinco métodos.
- A abertura liga `PRAGMA foreign_keys = ON` e `PRAGMA secure_delete = ON` fora da transação de migração, e só depois migra. Se a migração falha, é desfeita por completo. Um banco de versão mais nova que a do app é recusado, sem alteração.
- Todas as gravações passam por uma fila compartilhada pelos dois repositórios, que executa uma por vez. É necessária porque `withTransactionAsync` não é exclusivo: consultas disparadas em outro ponto na mesma conexão entram na transação. `withExclusiveTransactionAsync` não foi usado porque não existe na web e abre outra conexão, na qual o `PRAGMA foreign_keys` não valeria.
- O id da conversa é gerado no app ao abrir uma conversa nova, mas a conversa só é gravada com a primeira troca bem-sucedida, na mesma transação da troca. Assim o Histórico nunca mostra conversas vazias nem conversas cuja primeira pergunta falhou. O município é fixo: gravar em uma conversa existente com outro município é erro.
- `secure_delete` sobrescreve o conteúdo apagado no arquivo. O texto de privacidade não deve prometer que os dados apagados sejam irrecuperáveis.
- Os dados ficam sem criptografia, protegidos apenas pelo isolamento do aplicativo pelo sistema operacional. SQLCipher exige build nativo próprio e não existe no Expo Go nem na web.
- `android.allowBackup: false` em `app.json`. A propriedade existe no esquema instalado (`@expo/config-types`), que a descreve como o que impede o backup e a restauração automáticos. Vale só para Android: o backup do iOS não foi configurado. Não foi verificado em Android.
- Falha ao abrir o banco bloqueia o app com "Não foi possível abrir seus dados" e "Tentar novamente".

### Decisões previstas

- **Mock primeiro.** O app é desenvolvido contra um mock que segue o contrato, incluindo aceitar o campo `historico`. Quando o `historico` vier preenchido, o mock varia a resposta, para que a interface de várias trocas seja testável. Quando a API real subir, troca-se só a URL. Os textos do mock devem ser identificados como exemplo. A flag de mock vem de `EXPO_PUBLIC_USAR_MOCK` (padrão: mock ligado) e a URL de `EXPO_PUBLIC_API_URL`, em `config/`. Copiar `apps/mobile/.env.example` para `.env` para alterar; o `.env` não é versionado.
- **Estado de conexão.** Decidido: `expo-network` (~58.0.4). Os dois candidatos, `expo-network` e `@react-native-community/netinfo` (12.0.1), constam em `bundledNativeModules.json` do SDK 58 e portanto rodam no Expo Go. A escolha foi pelo módulo oficial da Expo, versionado junto com o SDK, cuja API (lida nos tipos do pacote 58.0.4) já traz `useNetworkState`, `addNetworkStateListener` e `getNetworkStateAsync`, o que cobre o hook de conexão. Será instalado na tarefa que o usar, com `npx expo install expo-network`.
- **Painel de fontes.** Usa `Modal` do React Native, sem biblioteca de bottom sheet, para manter controle de foco e leitor de tela e evitar dependência.
- **Parâmetros provisórios do tema**, a validar com o uso: níveis de texto 0 a 3 com multiplicadores 1, 1.15, 1.3 e 1.5 sobre os tamanhos do mockup (`theme/typography.ts`), e limite de 2x para a escala de fonte do sistema (`LIMITE_ESCALA_SISTEMA`, ainda não aplicado a nenhum componente).
- **Contexto da conversa.** `TROCAS_NO_CONTEXTO = 3` em `config/limites.ts`, conforme a proposta de `historico`. O limite de caracteres da pergunta e o corte dos textos do contexto ainda não foram definidos.
- **Privacidade.** As perguntas podem conter dados sensíveis. O histórico fica apenas no dispositivo e nada de pergunta vai para log. O filtro de PII fica na API (Cybersecurity). O texto de privacidade deve ser revisado depois com a decisão sobre o provedor de LLM, para não prometer o que não se pode garantir.
- **Offline.** O histórico continua legível sem conexão. Sem conexão, o envio fica desativado com mensagem clara e o texto digitado não se perde.

## Acessibilidade

Definição de pronto de cada PR: todo componente e tela novos entram com rótulo e papel de acessibilidade, alvo de toque mínimo e apenas cores dos tokens de `theme/`, cujos contrastes já foram medidos. A tarefa 10 é a revisão final, não o primeiro contato com acessibilidade.

- Alvos de toque de pelo menos 44 pt (iOS) e 48 dp (Android), inclusive o botão de apagar e os ícones do cabeçalho.
- Contraste mínimo AA, com atenção aos textos pequenos em verde sobre fundo claro (como "Ver fontes" e o selo "Demonstração"). Verificar com uma ferramenta, não a olho.
- Rótulos e papéis de acessibilidade em ícones (voltar, enviar, apagar, seta) e na navegação por abas.
- A chegada de uma nova resposta deve ser anunciada ao leitor de tela.
- Painel de fontes: foco gerenciado ao abrir e ao fechar, botão de fechar e leitura correta por leitor de tela.
- Respeitar a escala de fonte do sistema e a escolha dentro do app, sem quebrar o layout nos tamanhos maiores.
- Não depender só da cor: a ação de apagar tem texto e ícone além do vermelho.
- Linguagem simples nas mensagens de erro e de estado.

## Dependências e decisões

- **Contrato da API** (A2, D2): necessário para trocar o mock pela API real, incluindo o campo `historico`.
- **Uso do contexto pela API e pela LLM:** depende de P1 e P3 aceitarem a proposta de `historico`. Se não aceitarem, a conversa continua exibindo várias trocas no app, mas cada resposta será dada sem contexto, e isso deve ser dito ao cidadão ou resolvido antes da entrega.
- **Quantas trocas enviar como contexto:** proposta de 3, a validar com P3.
- **URL da API de desenvolvimento:** pendente, depende de quem sobe a API.
- **Estrutura de pastas:** decidido seguir sem `navigation/`, porque o briefing da GS cita Expo Router. Se o professor pedir a pasta, ajustar.
- **Rubrica completa do Mobile:** está na tarefa do Teams. Conferir os pesos antes de priorizar.

## Critérios de aceite

- O usuário seleciona o município e faz uma pergunta.
- Uma pergunta de seguimento na mesma conversa é respondida levando em conta as trocas anteriores.
- Cada resposta mostra o texto, os serviços relacionados e as fontes, e o painel de fontes mostra título, link e data quando houver.
- Os três serviços do piloto (coleta de lixo, iluminação pública e Cata Treco) podem ser consultados.
- Conteúdo de outro município não aparece, e o município não muda dentro de uma conversa.
- Sem evidência suficiente, o usuário é informado e pode continuar a conversa.
- O histórico guarda conversas completas, persiste após reiniciar o app, funciona sem internet, permite reabrir e continuar uma conversa e apagar uma conversa ou tudo, ambos com confirmação.
- O tamanho do texto pode ser ajustado nas Preferências.
- O modo demonstrativo está identificado na Conversa e nas Preferências.
- O app tem estados de carregamento, erro, sem conexão e vazio.
- As telas são compostas por componentes reutilizáveis, sem markup nem estilo duplicado, com estilos vindos de `theme/`.
- Acessibilidade verificada: contraste, escala de fonte, rótulos de leitor de tela, alvos de toque, navegação.
- O app consome a API ou um mock e salva informações em banco de dados.

## Entregáveis da disciplina

Enviar o link do repositório no Teams até 19/11, às 23h55, por um único integrante. A nota de 0 a 10 considera seis critérios: telas e navegação, consumo de dados, banco de dados, acessibilidade, aderência ao problema e documentação. O repositório deve conter:

- O código do app organizado.
- O README com a Ficha da CitizenTech, as instruções para rodar o app e os prints das telas.
- O link do vídeo de até 3 minutos com o app rodando.

## Modo de trabalho para o agente

- Antes de escrever código, proponha a arquitetura e o plano e aguarde a confirmação do responsável.
- Entregue arquivos completos, nunca trechos ou diffs parciais.
- Não escreva comentários no código.
- Siga os padrões já existentes no repositório antes de inventar novos. Justifique qualquer dependência nova.
- Reutilize componentes: não duplique markup ou estilo, e mantenha lógica em hooks e acesso externo em services.
- Se uma ideia ou instrução não for válida, diga isso abertamente e explique o motivo, em vez de concordar por padrão.
- Não invente rotas, campos, fatos, resultados ou evidências. Informe o que não foi verificado.
- Não inclua segredos nem dados pessoais reais em código, commits, prompts, logs ou evidências. Use variáveis de ambiente e dados simulados.
- Use uma branch por tarefa, curta e específica (por exemplo `feat/mobile-abas-e-navegacao`), com PR pequeno e revisão de outro integrante.
- Ao concluir, atualize este documento, o registro de features e o `prompts.md`.

## Verificações e evidências

### Resumo: verificado e não verificado (07/11/2026)

**Verificado**, executado nesta máquina (Windows, Node 22.22.0, Chrome 156):

- Jest: 11 suítes e 84 testes passando. Os testes de banco rodam SQLite real em memória (`node:sqlite`, SQLite 3.50.4), não o `expo-sqlite` nativo.
- `npm run typecheck` e `npx expo lint` com código de saída 0; `npx expo-doctor` com 20/20.
- `npx expo export` para Android, iOS e web, com código de saída 0. Isso confirma só que o bundle compila.
- Chrome (web, `npm run web`, janela de 393 × 852, controlado pelo protocolo de depuração): telas de Início, seletor de município, lista de serviços, navegação pelas abas, estado de erro, papéis de acessibilidade e a persistência do banco local (abrir, reabrir o banco existente, recarregar a página, abrir uma aba nova).

**Não verificado:**

- **Android:** nenhuma execução (Expo Go, emulador ou aparelho). Também não foram verificados o efeito de `allowBackup: false`, o `package` do `app.json` e o passo a passo do README.
- **iOS:** nenhuma execução. O backup do iOS não foi configurado.
- **Expo Go**, em qualquer plataforma, inclusive a compatibilidade da versão instalada no celular com o SDK 58.
- **`expo-sqlite` nativo** (Android e iOS): abertura, `PRAGMA`s, migração, `withTransactionAsync`, fila de gravações e persistência depois de reiniciar o app no aparelho. No Jest o motor é outro.
- **Web:** `crossOriginIsolated` é falso (detalhes na seção do banco); Safari e Firefox; hospedagem de produção.
- **Telas:** `Modal`, toque, teclado, áreas seguras, TalkBack e VoiceOver; a fonte do mockup (Inter) contra a do sistema; níveis de texto e escala de fonte do sistema; estado "carregando" e rolagem em telas pequenas; contraste final de `Notice` e botões.
- **Conversas:** a gravação pela interface (a tela de Conversa ainda não existe). Os repositórios só foram exercitados pelo Jest.
- **API:** cliente real contra um servidor (a API não existe); só com `fetch` simulado.
- **Outros:** testes de renderização de componentes e hooks (não há `@testing-library/react-native`); as 5 vulnerabilidades moderadas que o `npm install` relata, não investigadas.

### Tarefa 1+2 — esqueleto, tema, tipos e configuração (07/11/2026)

Executado em `apps/mobile`:

| Comando | Resultado |
|---|---|
| `npx create-expo-app@5.0.0 apps/mobile --yes` | Projeto criado com o template padrão do SDK 58 e dependências instaladas |
| `npx expo install eslint eslint-config-expo -- --save-dev` | Instalado; `eslint-config-expo` foi corrigido manualmente para `devDependencies` |
| `npm run typecheck` (`tsc --noEmit`) | Código de saída 0 |
| `npx expo lint` | Código de saída 0 |
| `npx expo-doctor` | 20/20 verificações sem problemas |
| `npx expo export --platform android` | Código de saída 0; bundle Hermes gerado (2,9 MB). Valida o Metro, o alias `@/` e o Expo Router |

Contraste (fórmula WCAG, script local descartável): texto `#12201D` e `#3D4F4A` sobre fundo, branco e tons verdes: de 7,13:1 a 16,80:1; verde `#0B6B5E` sobre fundo, branco, `#E3F3EF` e `#D5EEE8`: de 5,25:1 a 6,40:1; branco sobre verde: 6,40:1; `#B42318` sobre branco e fundo: 6,57:1 e 6,30:1. Todos acima de 4,5:1. O único reprovado foi a borda do mockup, tratada acima. As bordas decorativas `border` (`#D6E6E1`, 1,24:1) servem só de contorno de cartões e não carregam informação.

### Camada de API e dados mockados (07/11/2026)

Código em `apps/mobile/services/api/`:

- `ApiClient.ts`: interface única com `listarMunicipios`, `listarServicos(municipioId)` e `enviarPergunta(municipioId, envio)`, nas rotas propostas em `docs/contratos-de-integracao.md`.
- `apiMock.ts` e `mock/`: implementação com dados locais. Sem rede, sem links.
- `apiReal.ts`: cliente `fetch` das mesmas rotas, com tempo limite de 15 s, `Accept` e `Content-Type` JSON e validação do formato das respostas. Nenhuma pergunta é registrada em log. **Não foi testado contra uma API real, porque ela não existe.**
- `erros.ts`: `ErroApi` com tipo `sem-conexao`, `indisponivel`, `configuracao` ou `desconhecido`. É uma convenção do app, não do contrato, que ainda não define erros.
- `index.ts`: exporta `api`, escolhido por `EXPO_PUBLIC_USAR_MOCK` (padrão `true`). A latência simulada vem de `EXPO_PUBLIC_MOCK_LATENCIA_MS` (padrão 600 ms), para a tela de carregando ser visível.

Dados do mock (`mock/dados.ts`), todos de exemplo e sem valor factual:

- Municípios `sao-paulo` (São Paulo · SP) e `exemplo` (Município Exemplo, UF `XX`, fictício). Os ids são do mock, não do contrato. Poá continua sendo a referência do piloto no restante do projeto; o mock usa São Paulo como município de exemplo por decisão do responsável pelo mobile, e o texto do mock não tem valor factual sobre nenhum município.
- Serviços: São Paulo oferece Coleta de lixo, Iluminação pública e Cata Treco; o município de exemplo oferece só os dois primeiros, o que permite testar o isolamento entre municípios (a mesma pergunta sobre sofá responde em São Paulo e informa insuficiência no município de exemplo).
- A resposta é escolhida por palavras-chave da pergunta, sem acento e sem diferença de maiúsculas. Todo texto começa com `(Exemplo)` e traz passos genéricos, sem horários, endereços ou prazos.
- Temas fora da cobertura (por exemplo "lixo eletrônico") e perguntas sem serviço reconhecido devolvem `informacaoSuficiente: false`, sem fontes nem serviços.
- Quando `historico` vem preenchido, a resposta muda: cita a quantidade de trocas de contexto e, numa pergunta de seguimento sem palavra-chave própria ("e se for domingo?"), usa o serviço da conversa. Sem histórico, a mesma pergunta informa insuficiência. O contexto não atravessa municípios.
- Fontes sem URL (`url: ''`), uma com `dataConsulta` (`2026-11-07`, ISO) e outra sem, para exercitar a data opcional. `canalOficialUrl` nunca é preenchido. A interface precisa tratar `url` vazia como "sem link".
- `servicosRelacionados` devolve **ids** de serviço (`cata-treco`), como diz `docs/contratos-de-integracao.md`. O nome exibido ("Cata Treco") deve vir de `listarServicos`. Se a API real devolver nomes, isso muda.

### Componentes, abas e Início (tarefa 4+5, 07/11/2026)

Implementado em `apps/mobile`:

- `components/`: `AppText` (todo texto passa por ele, com variantes do tema e limite de escala do sistema), `ContainerTela`, `CabecalhoTela`, `Button` (primário, secundário e perigo), `ListRow`, `SectionTitle` (simples e em faixa), `Notice` (info, tint e warning, com ação opcional), `Selo`, `Carregando`, `FolhaModal` (folha sobre a tela com `Modal`), `SeletorMunicipio` (reutilizável no filtro do Histórico), `BarraAbas` e `Icon`. As telas não repetem markup nem estilo e só usam tokens de `theme/`.
- `app/`: `(tabs)` com Início, Histórico e Preferências e a rota `conversa/[id]`. Os arquivos de rota só renderizam uma tela de `screens/`.
- `contexts/MunicipioContext.tsx`: lista de municípios (via mock ou API), município atual (o escolhido ou o primeiro da lista) e recarregar.
- `hooks/`: `useCarregamento` (estado carregando, erro ou pronto, com recarregar e descarte de resposta atrasada), `useMunicipios` e `useServicos`.
- `utils/`: `gerarId` (id da conversa gerado no app, sem dependência), `formatarMunicipio`, `perguntaSugerida`, `iconeDoServico`, `mensagemDeErro` (erro da API em linguagem simples, sem expor a mensagem técnica).
- Início: município, "Iniciar conversa" e serviços do município. Tocar em um serviço abre `conversa/[id]` com `municipioId` e `perguntaSugerida` como parâmetros. Estados de carregando, erro com "Tentar novamente" e lista vazia, tanto para municípios quanto para serviços.

Provisório nesta etapa, a substituir nas tarefas seguintes:

- Histórico e Preferências mostram só o título e um aviso. A Conversa mostra o cabeçalho (voltar, município, selo "Demonstração") e a pergunta sugerida recebida, sem enviar nada.
- O município escolhido é salvo na tabela `preferencias` (tarefa 6). Se a gravação da preferência falhar, o erro é ignorado sem aviso e a escolha vale só até o app fechar.
- `iconeDoServico` mapeia os três ids do mock; qualquer outro id usa o ícone de conversa. Quando a API real definir os ids, o mapa precisa ser revisto.
- `perguntaSugerida` monta "Quero saber sobre <serviço em minúsculas>." O texto exato não está no mockup nem no contrato.

#### Verificações (07/11/2026)

| Comando ou cenário | Resultado |
|---|---|
| `npm run typecheck` | Código de saída 0, com e sem o arquivo gerado `.expo/types/router.d.ts` (rotas tipadas) |
| `npx expo lint` | Código de saída 0 |
| `npx jest` | 4 suítes, 37 testes passando |
| `npx expo-doctor` | 20/20 verificações sem problemas |
| `npx expo export` para `android` e `web` | Código de saída 0 nos dois |
| Tela de Início no Chrome, com a janela emulada em 393 × 852 e o servidor `expo start --web` | Conferida por captura de tela contra `01 · Início@1x.png`: cabeçalho, título, seletor, botão, três serviços, aviso e barra de abas com o item ativo destacado |
| Fluxo no navegador (cliques reais) | Abrir o seletor mostra o município de exemplo da época (Poá, hoje São Paulo) marcado; escolher "Município Exemplo" troca a lista para dois serviços (sem Cata Treco); tocar em "Coleta de lixo" abre `/conversa/<id>?municipioId=exemplo&perguntaSugerida=...` com o município e a pergunta; "Voltar" retorna a `/`; as abas Histórico e Preferências abrem |
| Estado de erro, servidor com `EXPO_PUBLIC_USAR_MOCK=false` e sem URL | Aviso âmbar "Não foi possível carregar os municípios", mensagem de configuração e botão "Tentar novamente" |
| Papéis de acessibilidade na web | Botões e abas com rótulo; `tablist` presente; dois títulos (`heading`) na tela de Início |

Correções feitas durante a verificação: na web os ícones apareciam para leitores de tela como caracteres soltos, porque `accessibilityElementsHidden` e `importantForAccessibility` não viram atributos no navegador. O `Icon` agora usa `aria-hidden`, e a conferência no DOM mostrou 14 ícones, 0 expostos. Outro ajuste: `StyleSheet.absoluteFillObject` não existe no React Native 0.88 (usado posicionamento explícito).

Medição de layout: com a janela emulada em 393 px, todos os contêineres do texto de apoio medem 393 px e o texto quebra em "…da sua / cidade.".

Não verificado nesta etapa:

- Execução no Expo Go, em emulador ou em aparelho real. Toda a verificação visual foi no navegador (Chrome, Windows) via `expo start --web`. Comportamento de `Modal`, toque, teclado, áreas seguras e leitor de tela nativo (TalkBack e VoiceOver) é diferente no celular.
- A fonte: o mockup usa Inter e o app usa a fonte do sistema (no Windows, Segoe UI), então a tipografia difere do mockup.
- Escala de fonte do sistema e níveis de texto (A−, A+) com a interface montada. Os componentes já aplicam o limite de escala do sistema, mas nenhuma tela foi vista com fonte grande.
- Rolagem em telas pequenas e o estado "carregando", que dura 600 ms no mock e não foi capturado.
- Os hooks e os componentes não têm testes de renderização (não há `@testing-library/react-native`). Cobertos por teste só as funções puras de `utils/` e a camada de API.
- Contraste de `Notice` e dos botões foi derivado dos tokens já medidos, mas a combinação final na tela não foi medida com ferramenta.

### Banco local e persistência (tarefa 6, 07/11/2026)

Código em `apps/mobile/services/banco/` (`tipos`, `erros`, `fila`, `migracoes`, `abrirBanco`, `repositorioConversas`, `repositorioPreferencias`, `index`) e `contexts/BancoContext.tsx`. O `MunicipioContext` lê e grava o município escolhido. Também foram feitos: `types/guardas.ts` (validadores de formato, antes dentro de `apiReal.ts`), `utils/titulo.ts`, `metro.config.js`, `engines` no `package.json`, `android.allowBackup: false` no `app.json` e a troca do município de exemplo do mock.

| Comando ou cenário | Resultado |
|---|---|
| `npx expo install expo-sqlite` | `expo-sqlite ~58.0.15` em `dependencies`; o plugin `expo-sqlite` foi adicionado ao `app.json` |
| `npx jest` | 11 suítes, 84 testes passando. Cobrem: migração (banco novo, idempotência, versão futura recusada sem alterar nada, migração que falha desfeita por completo), chaves estrangeiras (com controle: sem o `PRAGMA` a restrição não é aplicada), conversa criada só com a primeira troca, ordem das trocas, rollback quando a troca falha, município fixo, gravações simultâneas com a fila (e controle: sem a fila colidem), leitura completa com fontes com e sem data e com e sem `canalOficialUrl`, filtro e ordenação, JSON corrompido, apagar uma conversa e apagar tudo (preservando preferências), mensagens de erro sem texto da pergunta, preferências (padrões, sobrescrita, valor inválido), ordem de abertura (PRAGMAs antes da migração), fechamento do banco em falha e abertura memorizada |
| `npm run typecheck` | Código de saída 0 (o adaptador de teste é aceito como `Pick<SQLiteDatabase, ...>` sem conversão de tipo) |
| `npx expo lint` | Código de saída 0 |
| `npx expo-doctor` | 20/20 verificações sem problemas |
| `npx expo export` para `android`, `ios` e `web` | Código de saída 0 nos três; a web inclui `wa-sqlite.wasm` (641 KB) |
| Chrome, `expo start --web` em porta própria (8083) com `--clear` | O app abre e a pasta `expo-sqlite` aparece no OPFS do navegador |
| Chrome: escolher "Município Exemplo", recarregar a página | Depois do reload o seletor continua em "Município Exemplo · XX" e a lista mostra os 2 serviços desse município |
| Chrome: aba nova, aberta depois de fechar a anterior | O seletor abre em "Município Exemplo · XX", lido do banco |

Achados registrados:

- O `node:sqlite` liga as chaves estrangeiras por padrão, e a documentação do `expo-sqlite` manda ligá-las a cada conexão. O adaptador de teste abre o banco com `enableForeignKeyConstraints: false`; sem isso, os testes de chave estrangeira passariam mesmo sem o `PRAGMA`.
- Na web, `crossOriginIsolated` é falso e `SharedArrayBuffer` não existe. O `metro.config.js` segue o guia do pacote (`.wasm` em `assetExts` e os cabeçalhos `Cross-Origin-Embedder-Policy: credentialless` e `Cross-Origin-Opener-Policy: same-origin`), e `curl` mostrou os cabeçalhos na resposta do bundle, mas não na do documento HTML `/`, que o servidor da Expo entrega por outro caminho. O app só usa a API assíncrona do `expo-sqlite`, e ela funcionou assim. As APIs síncronas, que o guia diz exigirem `SharedArrayBuffer`, não devem ser usadas na web sem resolver o cabeçalho do documento. Não resolvido.
- Na web, duas abas abertas ao mesmo tempo: a segunda mostrou "Não foi possível abrir seus dados" com "Tentar novamente". Foi observado por acidente durante a verificação (abas do próprio teste ficaram abertas) e some quando a primeira é fechada. A causa foi inferida; não foi investigada a fundo.
- Os cabeçalhos do Metro valem só para o servidor de desenvolvimento. Para hospedar a web em produção seria preciso enviá-los pelo servidor, o que não foi feito nem é entregável do projeto.

### Execução na web (07/11/2026)

A web é um alvo de desenvolvimento e de demonstração, não de entrega: o produto é um app mobile. Configuração: `react-dom` 19.3.0 e `react-native-web` ^0.21.4 (instalados com `npx expo install`), script `npm run web` (`expo start --web`) e bloco `web` em `app.json` (Metro, saída `single`). `expo run:android` e `expo run:ios` geram o app nativo e não se aplicam à web.

| Comando | Resultado |
|---|---|
| `npm run typecheck` | Código de saída 0 |
| `npx expo-doctor` | 20/20 verificações sem problemas |
| `npx expo export --platform web` | Código de saída 0; bundle de 1,1 MB com o texto da tela de Início |

`expo start --web` na porta 8081: `GET /` respondeu 200 e o bundle de desenvolvimento (4 MB) foi gerado com o texto da tela de Início.

### Verificações da camada de API, ícones e testes (07/11/2026)

| Comando | Resultado |
|---|---|
| `npx jest` | 3 suítes, 30 testes passando: mock (municípios, serviços, isolamento, cobertura dos três serviços, ausência de links, fonte com e sem data, continuidade com `historico`, validação), cliente real com `fetch` simulado (rotas, corpo do POST com `historico`, URL sem configuração, falha de rede, status de erro, formato inesperado, corpo que não é JSON) e `normalizarTexto` |
| `npm run typecheck` | Código de saída 0 (foi preciso declarar `"types": ["jest"]` em `tsconfig.json`: o TypeScript 6 não inclui `@types/*` automaticamente) |
| `npx expo lint` | Código de saída 0 |
| `npx expo-doctor` | 20/20 verificações sem problemas |
| `npx expo export --platform android` e `--platform web` | Código de saída 0 nos dois; ambos incluem a fonte do `MaterialCommunityIcons` (1,3 MB), então o `Icon` é resolvido nos dois alvos |

Limitações: os testes cobrem a lógica, não a renderização (não há `@testing-library/react-native`). O `Icon` só foi verificado por tipos e por bundle, não visto na tela. O cliente real só foi testado com `fetch` simulado.

O comportamento do `expo-sqlite` na web foi verificado na seção "Banco local e persistência". Os itens não verificados estão no "Resumo: verificado e não verificado".

Registrar aqui, nas próximas tarefas, os comandos e resultados reais, os prints das telas e o vídeo de até 3 minutos.

## Riscos, pendências e próximos passos

- Contrato da API ainda não acordado: o mock pode divergir da resposta real, principalmente no campo `historico`.
- A continuidade da conversa depende de a API e a LLM usarem o contexto enviado.
- O histórico enviado como contexto aumenta a superfície de PII e precisa passar pelo mesmo filtro da pergunta.
- Comunicação com a API em ambiente de desenvolvimento (URL, rede local) ainda não definida.
- Texto de privacidade depende da decisão sobre o provedor de LLM e do filtro de PII.
- A responsabilidade do app diverge entre este documento (Matheus), o backlog e `consulta-e-aplicativo.md` (P1/P2). A correção será combinada com o grupo.

### Plano de tarefas

Uma branch curta por tarefa, com PR pequeno revisado por outro integrante. Acessibilidade faz parte da definição de pronto de cada PR. Entrega no Teams em 19/11/2026, às 23h55.

| # | Branch | Entrega | Backlog | Estado |
|---|---|---|---|---|
| 1+2 | `feat/mobile-scaffold-theme` | `apps/mobile` com Expo, TypeScript e Expo Router; versões fixadas; `theme/`, `types/`, `config/` | D4 | Implementada, commit `09380e5`, sem PR |
| 3 | `feat/mobile-scaffold-theme` | Interface do cliente da API, mock com os três serviços do piloto que aceita e varia com `historico`, marcado como exemplo | D4, D5 | Implementada, commit `09380e5`, sem PR |
| 4+5 | `feat/mobile-scaffold-theme` | Componentes base, abas e Início com seletor de município, serviços e estados | D4 | Implementada, commit `09380e5`, sem PR. Histórico, Preferências e Conversa são telas provisórias |
| 6 | `feat/mobile-scaffold-theme` | SQLite com `PRAGMA foreign_keys = ON`, migração, repositório de conversas e tabela `preferencias` | D6 | Implementada no working tree, sem commit nem PR. Falta ligar os repositórios às telas (Conversa na tarefa 7, Histórico e Preferências na 8+9) |
| 7 | `feat/mobile-conversa` | Conversa com várias trocas, `historico`, painel de fontes em `Modal`, informação insuficiente | D4, D5 | Planejada |
| 8+9 | `feat/mobile-historico-preferencias` | Histórico (listar, filtrar, reabrir, apagar) e Preferências | D6, D7 | Planejada |
| 10 | `feat/mobile-acessibilidade` | Revisão final de acessibilidade | D7 | Planejada |
| 11 | `feat/mobile-docs-entrega` | README com a Ficha da CitizenTech, prints, vídeo | D8 | Planejada |

- As tarefas 1+2, 3 e 4+5 estão no commit `09380e5` (`feat: implement mobile structure`), na branch `feat/mobile-scaffold-theme`. Nenhum PR foi aberto.
- Próximo passo: tarefa 7 (conversa), que usa `salvarTroca` e o contexto de `historico`.