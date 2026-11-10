# Feature — Aplicativo mobile do cidadão (Expo)

- Status: em desenvolvimento (tarefas 1+2, 3 e 4+5 commitadas em `09380e5` e tarefa 6 em `e6198e2`, sem PR; tarefa 7 em `fdb6a5c`, sem PR; tarefa 8 (Histórico) em `a9f36c7` e tarefa 9 (Preferências) em `c5e3cf8`, sem PR; tarefa 10 (acessibilidade) implementada no working tree, sem commit)
- Responsável e colaboradores: Matheus (mobile e Cybersecurity); API P1; LLM P3
- Itens do backlog / issue: D4, D5, D6, D7, D8 (apoio em D1–D3 para o contrato)
- Componentes e disciplinas afetados: `apps/mobile`; Mobile Development & IoT (integração com a API de SOA e C#)
- Última atualização: 09/11/2026
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

`apps/mobile` existe desde 07/11/2026 (branch `feature/mobile`) com o esqueleto do projeto, `theme/`, `types/` e `config/`, o componente `Icon`, a camada de API com dados mockados (`services/api/`), testes com Jest e a tarefa 4+5: componentes base, navegação por três abas com o Expo Router, contexto de município, hooks de carregamento e a tela de Início completa sobre o mock. A tarefa 6 acrescentou o banco local SQLite (`services/banco/`). A tarefa 7 ligou a tela de Conversa à API (mock por padrão) e ao banco: envio de perguntas com `historico`, gravação de cada troca, painel de fontes, aviso de informação insuficiente e reabertura de uma conversa salva pela URL. A tarefa 8 trocou a tela de Histórico provisória pela real (lista, filtro, reabrir, apagar com confirmação, faixa sem internet). A tarefa 9 trocou a tela de Preferências provisória pela real: tamanho do texto (A− e A+, aplicado ao app todo e salvo no banco), explicação de privacidade, apagar todo o histórico com confirmação e o bloco "Sobre o aplicativo". Com isso, as três abas e a Conversa estão funcionais sobre o mock. A API em C# existe em outra branch (`feat/soa-csharp-api`, de P1) e **não foi executada por este app**: o contrato que ela implementa foi só lido no código (ver "Riscos, pendências e próximos passos"). Os tipos em `types/` seguem a proposta deste documento e continuam a acordar com P1.

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
- Ícone e tela de abertura: ver "Fechamento do mobile (10/11/2026)". Os próprios substituíram os do template, mas só foram vistos como arquivos de imagem, não instalados em um aparelho.

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

#### Como a tarefa 7 implementou a Conversa (09/11/2026)

- Rota `conversa/[id]`: o `id` é gerado no app ao iniciar a conversa. A conversa só é gravada depois da primeira troca bem-sucedida. A pergunta sugerida de um serviço vem preenchida no campo, editável, e não é enviada sozinha.
- Limites da API (lidos em `feat/soa-csharp-api`, não executados): pergunta de 1 a 500 caracteres (campo com `maxLength` 500) e `historico` com no máximo 3 trocas, cada pergunta e cada resposta com no máximo 1000 caracteres. `montarHistorico` (`utils/historico.ts`) pega as 3 últimas trocas e corta com reticências o que passar de 1000.
- Erros por status HTTP: 429 vira `limite-de-uso` ("muitas perguntas em pouco tempo") e 422 vira `pergunta-invalida`; os demais status continuam `indisponivel`, e falha de rede, `sem-conexao`. A pergunta que falhou continua no campo.
- Se a gravação falhar, a conversa continua na tela com o aviso "Conversa não salva"; as trocas não gravadas são tentadas de novo, em ordem, no próximo envio.
- "Informação insuficiente": o app mostra o texto fixo do mockup e **não exibe o `resposta` devolvido pela API** nesse caso (ele só fica salvo). O botão "Abrir canal oficial" só aparece se `canalOficialUrl` for um link `https`.
- Links: só abrem URLs `https` sem usuário ou senha (`utils/links.ts`). Fonte sem link mostra "Esta fonte de exemplo não tem link". `dataConsulta` (`yyyy-MM-dd`) aparece como `dd/MM/yyyy`; valor fora desse formato é ignorado.
- O selo "Demonstração" e a nota "Exemplo de resposta para o protótipo." só aparecem com `EXPO_PUBLIC_USAR_MOCK=true`.
- Itens que ficaram para a revisão: ver "Revisão dos itens pendentes (10/11/2026)".

### Histórico

- Texto "Suas conversas salvas neste celular." e filtro por município.
- Cada item é uma conversa: título (a primeira pergunta, encurtada), data e hora da última troca, uma seta para abrir e um botão de apagar. Apagar uma conversa pede confirmação.
- Faixa "Disponível sem internet" exibida somente quando não houver conexão.
- Estado vazio quando não houver conversas.

#### Como a tarefa 8 implementou o Histórico (09/11/2026)

- Filtro de município: começa no município atual e tem a opção "Todos os municípios" (a lacuna 6 do mockup). Escolher um município no filtro **não** muda o município atual do app. Com "Todos", cada item mostra o município. O filtro vale só enquanto a tela está montada.
- Cada item mostra o título, a data da última troca (`formatarDataHistorico` em `utils/datas.ts`: "Hoje, 09:05", "Ontem, 16:20", "03 nov, 09:15" e, de outro ano, "31 dez 2025, 23:59") e abre a conversa por `/conversa/[id]`. A tela de Conversa lê o município e as trocas do banco.
- Apagar abre um `Modal` de confirmação ("Apagar conversa?") com "Apagar conversa" e "Cancelar". Usei `Modal` e não `Alert`, porque `Alert` não funciona na web. Se apagar falhar, aparece um aviso e a lista continua como estava.
- A lista é relida toda vez que a aba ganha foco (`useFocusEffect`), então uma conversa nova ou apagada aparece ao voltar.
- Faixa "Disponível sem internet" só quando `expo-network` informa `isConnected === false` (`hooks/useConexao.ts`). Dependência nova: `expo-network ~58.0.4`.
- Estados: carregando, erro com "Tentar novamente", vazio geral e vazio por município.
- Fora do que foi feito: "Apagar todo o histórico" (fica nas Preferências, tarefa 9) e a busca por texto.

### Preferências

- **Acessibilidade:** controle de tamanho do texto (A− e A+) com um texto de exemplo que reflete a mudança. O app respeita também a escala de fonte do sistema.
- **Privacidade:** "Como usamos seus dados" abre uma explicação curta (sheet ou modal) e "Apagar todo o histórico" pede confirmação antes de executar.
- **Sobre o aplicativo:** "Modo demonstrativo" como indicador somente de leitura, explicando que é um protótipo acadêmico, com conteúdo de fontes públicas e sem parceria com a prefeitura. A nota "Este aplicativo orienta. A solicitação é feita no canal oficial." fica visível.

#### Como a tarefa 9 implementou as Preferências (09/11/2026)

- **Tamanho do texto:** botões A− e A+ (com rótulos "Diminuir o tamanho do texto" e "Aumentar o tamanho do texto") movem o nível de 0 a 3. Cada botão fica desativado no limite. Uma legenda "Tamanho N de 4" mostra o nível (acréscimo meu, o mockup não tem). O nível vem de `PreferenciasProvider` (`contexts/PreferenciasContext.tsx`), é lido do banco antes de mostrar o app (para não piscar em tamanho errado) e gravado a cada mudança; se a gravação falhar, o nível vale só até fechar o app, sem aviso. `AppText` e o campo de pergunta da Conversa aplicam `escalarEstilo` ao nível. O contexto fica em `NivelTextoContext.ts`, separado, para o `AppText` não importar o banco (evita importação circular).
- **Fonte do sistema:** continua valendo, limitada a 2x por `LIMITE_ESCALA_SISTEMA` no `AppText` e no campo de pergunta. A escala do sistema se soma ao nível do app, então a combinação dos dois pode chegar a 3x.
- **Privacidade:** "Como usamos seus dados" abre um `Modal` com cinco parágrafos: dados só neste celular e sem conta; com o mock, nada sai do celular, e sem o mock, a pergunta e as últimas 3 trocas vão ao serviço configurado, com a frase de que o tratamento no servidor ainda não foi definido; não digitar CPF, senhas ou dados de saúde; os dados não são criptografados pelo app e `secure_delete` não garante que sejam irrecuperáveis; como apagar.
- **Apagar todo o histórico:** abre confirmação em `Modal` ("Apagar todo o histórico?"). Apaga as conversas e **preserva** as preferências (nível do texto e município). Mostra "Histórico apagado deste celular." e anuncia "Histórico apagado" ao leitor de tela; se falhar, mostra aviso.
- **Sobre o aplicativo:** o selo mostra "Ativo" com `EXPO_PUBLIC_USAR_MOCK=true` e "Desativado" no contrário. O texto **difere do planejado**: em vez de "conteúdo de fontes públicas", diz que é protótipo acadêmico, sem parceria com nenhuma prefeitura, e que no modo demonstrativo as respostas são exemplos e não vêm de fontes oficiais (porque o mock não usa fonte real). Sem o mock: "As respostas vêm da API configurada." O app não sabe se a API está em modo demonstrativo.

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
- **Estado de conexão.** Decidido: `expo-network` (~58.0.4). Os dois candidatos, `expo-network` e `@react-native-community/netinfo` (12.0.1), constam em `bundledNativeModules.json` do SDK 58 e portanto rodam no Expo Go. A escolha foi pelo módulo oficial da Expo, versionado junto com o SDK, cuja API (lida nos tipos do pacote 58.0.4) já traz `useNetworkState`, `addNetworkStateListener` e `getNetworkStateAsync`, o que cobre o hook de conexão. Instalado na tarefa 8, com `npx expo install expo-network` (usado por `hooks/useConexao.ts`).
- **Painel de fontes.** Usa `Modal` do React Native, sem biblioteca de bottom sheet, para manter controle de foco e leitor de tela e evitar dependência.
- **Parâmetros provisórios do tema**, a validar com o uso: níveis de texto 0 a 3 com multiplicadores 1, 1.15, 1.3 e 1.5 sobre os tamanhos do mockup (`theme/typography.ts`), e limite de 2x para a escala de fonte do sistema (`LIMITE_ESCALA_SISTEMA`, aplicado no `AppText` e no campo de pergunta). Os multiplicadores continuam provisórios: foram vistos só no Chrome.
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

### Revisão da tarefa 10 (09/11/2026)

Feita só na web (Chrome controlado por script) e por cálculo. Nenhum leitor de tela, celular ou emulador foi usado.

**Medido:**

- Contraste (fórmula do WCAG sobre os tokens de `theme/colors.ts`, não sobre pixels renderizados): todos os pares usados passam. O menor para texto é `primary` sobre `surfaceSelected`, 5,25:1 (mínimo 4,5:1). O menor para ícones e bordas de controles é `borderStrong` sobre `surfaceTint`, 3,26:1 (mínimo 3:1). Texto desativado (opacidade 0,5) não entra na conta.
- Rodada automática no DOM, em 393 e 320 px de largura, nos níveis de texto 1 e 4, com 10 telas e estados cada (Início, seletor, Conversa vazia, com resposta, painel de fontes, informação insuficiente, Histórico, confirmar apagar, Preferências, privacidade): zero controles sem nome, zero com menos de 44 px e zero elementos fora da tela, depois das correções abaixo.
- `Modal` (painel de fontes), na web: o foco entra no painel, fica preso dentro, `Escape` fecha e o foco volta ao botão "Ver fontes".

**Problemas encontrados e corrigidos:**

1. **Abas inativas focáveis na web.** O `Tab` do teclado parava nos controles de uma aba que não estava na tela (a biblioteca só marca `aria-hidden` e põe `zIndex: -1`). `AbaFocada` (`components/AbaFocada.tsx`), usado nas três rotas de aba, aplica `visibility: hidden` só na web. A ordem do `Tab` passou a ser só a aba atual e a barra de abas.
2. **Selo "Ativo" fora da tela** em Preferências, em 320 px no nível 4: a linha "Modo demonstrativo" agora quebra linha.
3. **Título "Conversa" quebrado no meio da palavra** ao lado do selo "Demonstração": o cabeçalho (`CabecalhoTela`) agora quebra o selo para a linha de baixo quando não cabe.
4. **Título "Preferências" quebrado no meio** no nível 4 em 320 px: `nivelParaVariante` limita só a variante `title` ao nível 3 de 4.
5. **Rótulo "Tamanho do texto" quebrado no meio** pelos botões A− e A+: a linha agora quebra e os botões descem.
6. **Rótulos da barra de abas vazavam da própria aba** no nível 4: a barra não escala com o nível do app (continua com a escala do sistema).
7. **Títulos das conversas cortados com reticências** no Histórico no nível 4 (o texto sumia): removi o limite de linhas.
8. **Texto de botão que quebra linha encostava nas bordas**: `Button` ganhou `paddingVertical`.
9. **Fundo do `Modal` era um segundo botão "Fechar"** repetido para teclado e leitor de tela: agora só responde a toque.

**Limites conhecidos:**

- Os multiplicadores dos níveis continuam provisórios. O título é limitado e a barra de abas não cresce, então o nível 4 não aumenta tudo na mesma proporção.
- No `Modal` da web, o `Tab` passa por um elemento sem nome dentro do diálogo (provavelmente a guarda de foco do `react-native-web`). Não foi investigado.
- A ferramenta que mede "sem nome" e tamanho conta só elementos visíveis. Ela não captura texto quebrado no meio de palavra; esses casos (itens 3 a 7) apareceram em capturas de tela vistas uma a uma, e só nas telas e estados listados acima.

### Revisão dos itens pendentes (10/11/2026)

**"Nova conversa" (removido).** Implementei um botão no cabeçalho, mas o usuário não pediu e mandou tirar; o código foi revertido. A Conversa continua sem essa ação (o mockup também não a desenha). Para outro assunto, o usuário volta ao Início e inicia uma conversa nova.

**Sem conexão na Conversa (feito, não testado de ponta a ponta).** Com `EXPO_PUBLIC_USAR_MOCK=false` e `useConexao() === false`, aparece o aviso "Sem internet" e o campo de envio fica desativado; ler a conversa e o Histórico continua possível. Com o mock ligado o aviso não aparece, porque o mock funciona sem rede. Esse caminho não foi exercitado: o modo real depende da API, que não foi executada, e não há teste automatizado de tela.

**Verificado:** Jest 15 suítes, 129 testes; `tsc --noEmit` sem erros; `npm run lint` (`expo lint`) sem saída.

**Não verificado:** celular e Android. `npx eslint .` direto (fora do script do projeto) aponta 2 erros `react-hooks/set-state-in-effect` em `useCarregamento.ts` e `useConversa.ts`, que já existiam antes desta revisão.

**Banco ocupado na web (diagnosticado e tratado).** Causa medida no Chrome: o `expo-sqlite` na web guarda o banco em um arquivo do navegador (OPFS) que só uma aba pode abrir por vez. Com o app aberto em uma aba, uma segunda aba falha com `createSyncAccessHandle ... Access Handles cannot be created if there is another open Access Handle`. Recarregar a mesma aba funcionou nos testes (4 de 4), então o caso do print provavelmente foi outra aba ou janela com o app aberto, mas isso é inferência: não consegui reproduzir o seu caso exato. Tentar abrir de novo na mesma página não resolve (a segunda tentativa deu `Invalid VFS state`), então não há retry automático. O que mudou: esse erro agora vira o tipo `em-uso`, com a mensagem "O aplicativo já está aberto em outra aba do navegador. Feche as outras abas e recarregue esta página." e o botão "Recarregar a página" (só na web). Testado: com a aba A aberta, a aba B mostra essa mensagem; depois de fechar A, o botão recarrega e o app abre. Só afeta a web; no celular o banco é nativo e não passa por esse caminho (não verificado em celular).

**Ainda aberto:** aviso do console "Blocked aria-hidden" no navegador do usuário, que não consegui reproduzir.

### Teste em aparelho (10/11/2026)

**iPhone com Expo Go (relato do usuário, não executado por mim).** O usuário leu o QR code do Metro em um iPhone e relatou que o app abriu e que enviar pergunta (mock), o Histórico e a mudança de preferências funcionaram. Depois de fechar o app e abrir de novo, o Histórico e as preferências continuaram funcionando (relato, sem detalhes). Não registrei: versão do iOS, versão do Expo Go, nível de texto, nem prints.

### Fechamento do mobile (10/11/2026)

Feito sem commit.

**Contrato da API.** Novo `services/api/__tests__/contratoApi.test.ts` (11 testes). Usa como entrada os exemplos de JSON de `docs/contratos-de-integracao.md` da branch `feat/soa-csharp-api` (leitura do documento, não de um servidor) e confere que `apiReal.ts` aceita: lista de municípios, lista de serviços (inclusive vazia), resposta suficiente com fonte `https` e `dataConsulta`, resposta de informação insuficiente, `canalOficialUrl` como texto; rejeita `null` em campo opcional; envia o corpo com `historico` e só os cabeçalhos `Accept` e `Content-Type`; e traduz 404, 422, 429 e 503. Isso prova que o app entende o formato **descrito** no contrato. **Não prova** que a API real devolve esse formato.

**App contra a API real: não foi possível.** A API (`apps/api`, ASP.NET Core 10) exige PostgreSQL e Neo4j (`Program.cs` usa `UseNpgsql` e um driver `bolt`; o modo `Llm:Mode=demo` só troca o respondedor, não elimina os bancos). Esta máquina tem o .NET SDK 10.0.403, mas não tem Docker nem PostgreSQL, e não os instalei. A API também não está em `feature/mobile`; li uma cópia de `origin/feat/soa-csharp-api`. Para rodar: levantar os dois bancos (o `docker compose` do repositório dela), aplicar `--migrate` e `--bootstrap`, publicar os 3 serviços do piloto, definir `EXPO_PUBLIC_USAR_MOCK=false` e `EXPO_PUBLIC_API_URL=http://<IP do PC>:<porta>/api/v1` (no iPhone, `localhost` não serve). Dois pontos desconhecidos: se a API aceita conexões de fora do PC (e o firewall do Windows) e se o Expo Go no iPhone aceita `http`.

**Ícone e tela de abertura.** Substituídos os arquivos do template por um balão de conversa branco com um prédio público (frontão e colunas) em verde `#0B6B5E`, a cor primária do tema: `icon.png` (1024, sem transparência), `android-icon-foreground.png`, `android-icon-background.png` (cor lisa), `android-icon-monochrome.png`, `splash-icon.png` e `favicon.png`. Gerados com um script Python (Pillow) que não foi guardado no repositório. No `app.json`, o fundo do ícone adaptativo do Android passou de `#E3F3EF` para `#0B6B5E` e `imageWidth` da abertura de 76 para 140. Visto: só as imagens `icon.png` e a monocromática, abertas como arquivo. Não visto: instalado na tela inicial do iPhone ou do Android, a tela de abertura em execução, o recorte do ícone adaptativo e o ícone da aba do navegador.

**`npm audit` (`apps/mobile`).** Total de 31 (1 baixa, 30 moderadas, nenhuma alta ou crítica); só dependências de produção (`--omit=dev`): 5 moderadas. As causas-raiz que o relatório nomeia são `glob` (cancelamento por `AbortSignal` permite percorrer o sistema de arquivos depois do tempo limite), `minimatch` (negação de serviço por amplificação de `globparts`) e `deepmerge` (poluição de protótipo em `mergeObject()`). Quase todo o resto é cadeia de `jest`, `eslint` e `expo` (CLI) que depende delas. O `npm audit fix --force` indica mudanças incompatíveis e não foi executado. Não avaliei se o app, em execução no aparelho, usa algum desses caminhos de código; as ferramentas de desenvolvimento rodam só no computador.

**Verificações de 10/11/2026** (`apps/mobile`): `tsc --noEmit` sem erros; `npm run lint` sem saída; Jest 16 suítes, 140 testes; `npx expo-doctor` 20/20; `npx expo export` para `android`, `ios` e `web`, código de saída 0 nos três.

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

- Jest: 16 suítes e 140 testes passando (10/11/2026, depois do fechamento do mobile). Os testes de banco rodam SQLite real em memória (`node:sqlite`, SQLite 3.50.4), não o `expo-sqlite` nativo.
- `npm run typecheck` e `npx expo lint` com código de saída 0 (repetidos em 09/11/2026); `npx expo-doctor` com 20/20 e `npx expo export` para `android`, `ios` e `web` com código de saída 0 (todos repetidos em 10/11/2026).
- `npx expo export` para Android, iOS e web, com código de saída 0 em 07/11/2026; depois da tarefa 7 só o da web foi repetido. Isso confirma só que o bundle compila.
- Chrome (web, `npm run web`, janela de 393 × 852, controlado pelo protocolo de depuração): telas de Início, seletor de município, lista de serviços, navegação pelas abas, estado de erro, papéis de acessibilidade e a persistência do banco local (abrir, reabrir o banco existente, recarregar a página, abrir uma aba nova). Em 09/11/2026, a Conversa com o mock: pergunta sugerida no campo, envio, resposta com serviço relacionado, painel de fontes, segunda pergunta com `historico` (o mock citou a troca anterior), resposta de informação insuficiente sem botão de canal (o mock não traz `canalOficialUrl`), botão de voltar e reabertura da conversa pela URL sem parâmetros, com as trocas lidas do SQLite. Ainda em 09/11/2026, o Histórico (perfil novo do Chrome, mock): lista filtrada pelo município atual, "Todos os municípios" com o nome do município em cada item, abrir uma conversa de outro município e voltar para `/historico`, confirmação de apagar, cancelar (a conversa continua), apagar (sai da lista) e a lista depois de recarregar a página. A faixa "Disponível sem internet" apareceu ao simular o modo offline pelo protocolo de depuração do Chrome (`Network.emulateNetworkConditions`) e sumiu ao voltar online. Ainda em 09/11/2026, as Preferências (perfil novo do Chrome, mock): A+ três vezes mudou o texto de exemplo de 17 px para 20, 22 e 26 px (altura de linha 24, 28, 31 e 36), A+ ficou desativado no nível 4 e A− no nível 1; o nível 3 continuou depois de abrir `/preferencias` em uma aba nova; o texto de Início, Histórico e Preferências aumentou (visto em captura de tela); o `Modal` de privacidade e a confirmação de apagar abriram; cancelar manteve a conversa; apagar tudo esvaziou o Histórico e manteve o nível do texto; A− três vezes voltou a 17 px.

**Não verificado:**

- **Android:** nenhuma execução bem-sucedida. Também não foram verificados o efeito de `allowBackup: false`, o `package` do `app.json` e o passo a passo do README.
- **iOS:** só o relato do usuário em um iPhone com Expo Go (ver "Teste em aparelho"); eu não executei nada em iOS. O backup do iOS não foi configurado.
- **Expo Go:** funcionou em um iPhone (relato do usuário, versão não registrada); não verificado em Android.
- **`expo-sqlite` nativo:** no iPhone o usuário relatou que histórico e preferências funcionaram (detalhes não registrados); no Android não foi verificado. Não verificados em nenhuma plataforma nativa: abertura, `PRAGMA`s, migração, `withTransactionAsync`, fila de gravações e persistência depois de reiniciar o app no aparelho. No Jest o motor é outro.
- **Web:** `crossOriginIsolated` é falso (detalhes na seção do banco); Safari e Firefox; hospedagem de produção.
- **Telas:** `Modal`, toque, teclado, áreas seguras, TalkBack e VoiceOver; a fonte do mockup (Inter) contra a do sistema; níveis de texto e escala de fonte do sistema; estado "carregando" e rolagem em telas pequenas; contraste final de `Notice` e botões.
- **Conversa (tarefa 7):** o hook `useConversa` e os componentes não têm teste de renderização (só `montarHistorico`, `links` e o mapeamento de status do cliente têm Jest). Não foram vistos no navegador: o estado "enviando" (o mock responde em 600 ms e o script só olhou depois), os erros de envio e de gravação, o botão "Abrir canal oficial" e o abrir de links `https`. Teclado e rolagem em celular, anúncio "Resposta recebida" para leitores de tela e `KeyboardAvoidingView` não foram verificados.
- **Histórico (tarefa 8):** o hook `useHistorico`, `useConexao` e os componentes não têm teste de renderização (só `formatarDataHistorico` tem Jest). A faixa sem internet só foi vista com o offline simulado do Chrome, não no sistema do celular. Não foram verificados: a lista com muitas conversas, o erro ao apagar e ao carregar, o `useFocusEffect` no aparelho, o `expo-network` nativo e TalkBack/VoiceOver nos botões de apagar.
- **Acessibilidade (tarefa 10):** nada foi testado com leitor de tela (TalkBack, VoiceOver, NVDA); só atributos no DOM da web. Os alvos de toque foram medidos em pixels CSS do Chrome, não em dp no Android. Não foram verificados: escala de fonte do sistema ampliada (o Chrome não a simula), larguras abaixo de 320 px, orientação horizontal (o app é só retrato), ordem de leitura no celular, anúncios "Resposta recebida" e "Histórico apagado", e o efeito de `AbaFocada` e do `tabIndex` no Android e no iOS (os dois só têm efeito na web, por desenho).
- **Preferências (tarefa 9):** `PreferenciasProvider`, `useApagarHistorico` e a tela não têm teste de renderização (só `escalarEstilo` tem Jest). Não foram verificados: os quatro níveis com a fonte do sistema ampliada (a combinação pode estourar o layout), o nível 4 nas telas de Conversa, Histórico e na barra de abas em celular pequeno, a falha ao gravar o nível, o erro ao apagar tudo, o `Modal` e os botões A−/A+ com TalkBack e VoiceOver, e o anúncio "Histórico apagado".
- **Web, banco ocupado ao recarregar:** em um teste, recarregar a página na mesma aba (`Page.navigate`) deixou a tela "Não foi possível abrir seus dados" por mais de 45 s, e "Tentar novamente" não resolveu; abrir a mesma URL em uma aba nova funcionou. Em outro teste a mesma recarga funcionou. A causa não foi investigada (suspeita: a aba antiga manter o acesso exclusivo ao arquivo do navegador). Só observado na web com o Chrome controlado por script; não se sabe se acontece em uso normal nem no celular.
- **API:** cliente real contra um servidor. O contrato da API em C# só foi lido, não executado: `dotnet`, PostgreSQL e Neo4j não foram rodados aqui, e o cliente real só foi testado com `fetch` simulado.
- **Outros:** testes de renderização de componentes e hooks (não há `@testing-library/react-native`); `npm audit`: ver "Fechamento do mobile (10/11/2026)".

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

- Contrato da API: a branch `feat/soa-csharp-api` (P1) implementa rotas em `/api/v1` com os mesmos campos que o app espera. Ao apontar o app para ela, `EXPO_PUBLIC_API_URL` precisa terminar em `/api/v1`. Leitura do código, sem execução. Divergências a combinar com P1 e P3: no modo `Llm__Mode=demo` a API recebe o `historico` mas não o usa (pergunta de seguimento sem palavra-chave volta como informação insuficiente); a API nasce sem municípios e serviços, então é preciso cadastrar e publicar o conteúdo; o limite de taxa é por IP (429); o app só reconhece os ids `coleta-de-lixo`, `iluminacao-publica` e `cata-treco` para ícones.
- A continuidade da conversa depende de a API e a LLM usarem o contexto enviado.
- O histórico enviado como contexto aumenta a superfície de PII e precisa passar pelo mesmo filtro da pergunta.
- Comunicação com a API em ambiente de desenvolvimento (URL, rede local) ainda não definida.
- Texto de privacidade depende da decisão sobre o provedor de LLM e do filtro de PII.
- A responsabilidade do app diverge entre este documento (Matheus), o backlog e `consulta-e-aplicativo.md` (P1/P2). A correção será combinada com o grupo.

### Plano de tarefas

Uma branch curta por tarefa, com PR pequeno revisado por outro integrante. Acessibilidade faz parte da definição de pronto de cada PR. Entrega no Teams em 19/11/2026, às 23h55.

| # | Branch | Entrega | Backlog | Estado |
|---|---|---|---|---|
| 1+2 | `feature/mobile` | `apps/mobile` com Expo, TypeScript e Expo Router; versões fixadas; `theme/`, `types/`, `config/` | D4 | Implementada, commit `09380e5`, sem PR |
| 3 | `feature/mobile` | Interface do cliente da API, mock com os três serviços do piloto que aceita e varia com `historico`, marcado como exemplo | D4, D5 | Implementada, commit `09380e5`, sem PR |
| 4+5 | `feature/mobile` | Componentes base, abas e Início com seletor de município, serviços e estados | D4 | Implementada, commit `09380e5`, sem PR. Histórico, Preferências e Conversa são telas provisórias |
| 6 | `feature/mobile` | SQLite com `PRAGMA foreign_keys = ON`, migração, repositório de conversas e tabela `preferencias` | D6 | Implementada, commit `e6198e2`, sem PR. Conversa já usa o repositório (tarefa 7); Histórico e Preferências ficam para a 8+9 |
| 7 | `feature/mobile` (feita nesta branch, não em `feat/mobile-conversa`) | Conversa com várias trocas, `historico`, painel de fontes em `Modal`, informação insuficiente | D4, D5 | Implementada no working tree, sem commit nem PR. O estado sem conexão entrou na revisão de 10/11/2026; "Nova conversa" foi descartada |
| 8 | `feature/mobile` (feita nesta branch) | Histórico (listar, filtrar, reabrir, apagar com confirmação, faixa sem internet) | D6, D7 | Implementada, commit `a9f36c7`, sem PR |
| 9 | `feature/mobile` (feita nesta branch) | Preferências (tamanho do texto, privacidade, apagar todo o histórico, sobre) | D6, D7 | Implementada, commit `c5e3cf8`, sem PR |
| 10 | `feature/mobile` (feita nesta branch) | Revisão final de acessibilidade | D7 | Implementada no working tree, sem commit nem PR. Só na web e por cálculo; falta leitor de tela e celular |
| 11 | `feat/mobile-docs-entrega` | README com a Ficha da CitizenTech, prints, vídeo | D8 | Planejada |

- As tarefas 1+2, 3 e 4+5 estão no commit `09380e5` (`feat: implement mobile structure`), na branch `feature/mobile`. Nenhum PR foi aberto.
- Próximo passo: a tarefa 11 (README da entrega, prints, vídeo) e rodar o app contra a API real quando os bancos dela estiverem de pé. O que está em "não verificado" (Android, leitores de tela, ícone instalado no aparelho) continua aberto.