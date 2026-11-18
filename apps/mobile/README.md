# App mobile do cidadão

App Expo (SDK 58) do Fácil Município. Protótipo acadêmico para a Global Solution CitizenTech 2026. O escopo, a arquitetura, as decisões e as verificações estão em [docs/features/mobile-app.md](../../docs/features/mobile-app.md).

## Ficha da CitizenTech

| Item | Descrição |
|---|---|
| Nome | Fácil Município — Wiki Cidade |
| Cidade e bairro | Poá (SP) |
| Cidadão beneficiado | Moradores que precisam encontrar informações sobre serviços urbanos |
| Problema, em uma frase | É difícil saber qual serviço procurar e onde encontrar orientações confiáveis |
| Como funciona | O cidadão escolhe o município, faz uma pergunta em linguagem simples e recebe uma orientação baseada só no conhecimento revisado e publicado, com a fonte consultada |
| O que cada parte faz | **App Expo (esta pasta):** perguntas, fontes e histórico salvo no celular. **API em C# (`apps/api` na `main`):** cadastro, revisão, publicação e consulta, com PostgreSQL e Neo4j. Demais partes: ver o [README da raiz na main](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/blob/main/README.md) |
| Link da entrega Mobile | [apps/mobile na branch feature/mobile](https://github.com/Luansramos/F-cil-Munic-pio-wiki-cidade/tree/feature/mobile/apps/mobile) |
| Indicador de impacto (proposta) | Respostas acompanhadas de fonte e acerto das orientações nos testes do grupo. Ainda não são resultados de impacto |
| ODS conectados | ODS 11 (facilitar o acesso a informações sobre serviços urbanos) e ODS 16 (acesso à informação pública e transparência) |
| Integrantes | Fernanda Rocha Menon (RM 554673), Luiza Macena Dantas (RM 556237), Luan Ramos Garcia de Souza (RM 558537), Matheus Ricciotti (RM 556930), Matheus Bortolotto (RM 555189) |

A Prefeitura de Poá é uma referência para o piloto, não uma parceira do projeto.

## Atendimento aos critérios de Mobile

| Critério | Implementação na entrega | Onde conferir |
|---|---|---|
| Telas e navegação | Quatro telas: Início, Conversa, Histórico e Preferências. Abas para as áreas principais, abertura e retomada de conversas e painel de fontes. | Prints, vídeo, `app/` e `screens/` |
| Consumo de dados | Cliente de API com seleção entre mock local e API HTTP em C#. Consulta municípios, serviços e respostas; trata carregamento, falhas e ausência de informação suficiente. | Prints da integração e `services/api/` |
| Banco de dados | SQLite local com conversas, trocas e preferências; permite reabrir o histórico e excluir conversas. | Tela Histórico e `services/banco/` |
| Acessibilidade | Ampliação persistida do texto, rótulos de acessibilidade, anúncios de estado e mensagens de orientação e erro. | Preferências, prints de texto maior e componentes das telas |
| Aderência ao problema | O morador descreve sua necessidade em linguagem simples e recebe orientação por município, com fontes e indicação de quando não há conhecimento suficiente. | Ficha CitizenTech, Conversa e painel de fontes |
| Documentação | Ficha, integrantes, requisitos, configuração, instruções de execução, prints e vídeo de até três minutos. | Este README |

Os recursos de acessibilidade estão implementados; não há evidência de avaliação com VoiceOver ou TalkBack. O impacto social descrito na ficha é uma hipótese a avaliar com usuários.

## Telas e jornada

1. **Início:** escolher o município, consultar os serviços disponíveis e iniciar uma conversa.
2. **Conversa:** enviar a pergunta, ler a orientação e consultar suas fontes. Sem cobertura suficiente, o app apresenta uma orientação para procurar o canal oficial.
3. **Histórico:** consultar e retomar conversas salvas, filtrar por município e excluir registros com confirmação.
4. **Preferências:** ajustar o tamanho do texto, consultar informações de privacidade e apagar o histórico.

O mock permite demonstrar a jornada sem depender do backend. Novas respostas da API exigem conexão; o histórico e as preferências ficam armazenados localmente. O aplicativo orienta o cidadão, sem protocolar solicitações junto à prefeitura.

## Prints das telas

As capturas foram feitas em 17/11/2026 no **navegador** (Chrome, tela de 393 × 852, escala 2x), com o app compilado para a web. **Não são capturas de um celular físico.** Todos os dados são de exemplo ou públicos; nenhuma pergunta contém dado pessoal.

### Modo demonstrativo (`EXPO_PUBLIC_USAR_MOCK=true`)

Respostas de exemplo, marcadas com "(Exemplo)" e com o selo "Demonstração". Os serviços e as orientações associados a "São Paulo" são exemplos fictícios do protótipo, sem vínculo com o atendimento oficial da cidade.

| 1. Início | 2. Resposta com fonte | 3. Fontes da resposta | 4. Sem informação suficiente |
|---|---|---|---|
| <img src="../../docs/features/img/app/01-inicio.png" width="210" alt="Tela Início com o município São Paulo, o botão Iniciar conversa e três serviços"> | <img src="../../docs/features/img/app/02-conversa-resposta-com-fonte.png" width="210" alt="Conversa com a resposta de exemplo sobre o Cata Treco e o link Ver fontes"> | <img src="../../docs/features/img/app/03-painel-de-fontes.png" width="210" alt="Painel Fontes desta resposta com duas fontes de exemplo"> | <img src="../../docs/features/img/app/04-conversa-sem-informacao-suficiente.png" width="210" alt="Aviso Não encontrei informação suficiente para uma pergunta sem cobertura"> |

| 5. Histórico (salvo no celular) | 6. Preferências | 7. Texto maior | 8. Início com texto maior |
|---|---|---|---|
| <img src="../../docs/features/img/app/05-historico.png" width="210" alt="Tela Histórico com a conversa salva e o botão de apagar"> | <img src="../../docs/features/img/app/06-preferencias.png" width="210" alt="Tela Preferências com tamanho do texto, privacidade e modo demonstrativo"> | <img src="../../docs/features/img/app/07-preferencias-texto-maior.png" width="210" alt="Preferências com o tamanho do texto no nível 3 de 4"> | <img src="../../docs/features/img/app/08-inicio-texto-maior.png" width="210" alt="Tela Início com o texto aumentado"> |

### Com a API em C# (`EXPO_PUBLIC_USAR_MOCK=false`)

O app lê municípios, serviços e respostas da API real em C# rodando no mesmo computador, em modo demonstração do conhecimento (`Llm__Mode=demo`: devolve o texto já revisado e publicado, **sem LLM**). O conteúdo de Poá vem do roteiro `scripts/demo_fluxo_http.py` da API, a partir de uma página pública da Prefeitura de Poá.

| 1. Município sem serviços | 2. Escolha do município | 3. Poá com serviço publicado |
|---|---|---|
| <img src="../../docs/features/img/app/api-01-inicio-municipio-sem-servicos.png" width="210" alt="Início do município fictício sem serviços, com o aviso de que ainda não há serviços disponíveis"> | <img src="../../docs/features/img/app/api-02-escolha-do-municipio.png" width="210" alt="Janela Escolha o município com Município fictício e Poá"> | <img src="../../docs/features/img/app/api-03-inicio-poa-com-servico.png" width="210" alt="Início de Poá com o serviço Cata Treco"> |

| 4. Resposta da API | 5. Fonte oficial | 6. Sem informação suficiente |
|---|---|---|
| <img src="../../docs/features/img/app/api-04-conversa-resposta-da-api.png" width="210" alt="Resposta sobre o Cata Treco de Poá com etapas e link da fonte oficial"> | <img src="../../docs/features/img/app/api-05-painel-de-fontes-da-api.png" width="210" alt="Painel de fontes com o título, o endereço, a data de consulta e o link Abrir fonte"> | <img src="../../docs/features/img/app/api-06-conversa-sem-informacao-suficiente.png" width="210" alt="Aviso de informação insuficiente para uma pergunta sem fonte publicada"> |

**Como foram feitas:** `expo export --platform web` e um servidor estático local que envia os cabeçalhos `Cross-Origin-Embedder-Policy: credentialless` e `Cross-Origin-Opener-Policy: same-origin`. No Chrome automatizado, o servidor de desenvolvimento (`npm run web`) não chegou a abrir o banco local; o build de produção abriu normalmente. Não foi investigada a causa.

## Vídeo de demonstração (até 3 minutos)

**Link: <https://youtu.be/r4uhjS2fZNY>** (2 min 46 s, 2560×1440). O vídeo foi gerado em Remotion, com a gravação real do app web em tela de celular, contra a API em C# em modo demonstração, e a narração da equipe. Não é um celular físico.

## Requisitos

- **Node.js 22.13 ou superior** (faixa completa em `engines`: `^22.13.0 || ^24.3.0 || ^26.0.0 || >=27.0.0`). Vem do `expo@58` e do `react-native@0.88`; o `node:sqlite` usado nos testes também exige 22.13 ou superior sem flag. Verificado apenas com a 22.22.0.
- npm 10 ou superior (verificado com 10.9.4).

## Comandos verificados

Dentro de `apps/mobile`:

```bash
npm install
npm run typecheck
npm run lint
npm test
npx expo-doctor
npx expo export --platform android
npx expo export --platform ios
npx expo export --platform web
```

`expo export` só confirma que o bundle compila, não que o app roda no aparelho.

## Rodar na web (desenvolvimento)

```bash
npm run web
```

O `expo run` não serve para isso. O SQLite na web usa WebAssembly e o armazenamento do navegador (OPFS); o `metro.config.js` empacota o `.wasm` e envia os cabeçalhos `Cross-Origin-Embedder-Policy: credentialless` e `Cross-Origin-Opener-Policy: same-origin` nas respostas do Metro. Se abrir o app em duas abas ao mesmo tempo, a segunda não consegue abrir o banco (acesso exclusivo ao arquivo) e mostra "Não foi possível abrir seus dados".

## Rodar a versão web exportada

Para reproduzir o caminho usado nas capturas, execute em `apps/mobile`:

```bash
npx expo export --platform web
```

Com Python 3 instalado, sirva a pasta exportada com os cabeçalhos exigidos pelo SQLite web:

```bash
python -c "import os
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
os.chdir('dist')
class Handler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cross-Origin-Embedder-Policy', 'credentialless')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        super().end_headers()
ThreadingHTTPServer(('127.0.0.1', 8081), Handler).serve_forever()
"
```

Abra `http://localhost:8081` no Chrome, mantendo apenas uma aba do app aberta. No Windows, se necessário, use `py -3` no lugar de `python`. Encerre qualquer servidor que já esteja usando a porta 8081. Alterações nas variáveis `EXPO_PUBLIC_*` exigem uma nova exportação.

## Rodar no Expo Go em um celular

1. No celular, instale o **Expo Go** pela Google Play (Android) ou pela App Store (iPhone) com uma versão compatível com o SDK 58 utilizado pelo projeto.
2. Conecte o celular e o computador **na mesma rede Wi-Fi**.
3. No computador, em `apps/mobile`:
   ```bash
   npm install
   npm start
   ```
4. No terminal aparece um QR code. No Android, abra o Expo Go e toque em **Scan QR code**. A câmera do celular sozinha pode não abrir o Expo Go. No iPhone, leia o QR code com a câmera e abra o link no Expo Go.
5. Se o celular não achar o computador, libere o Node.js no firewall do Windows para redes privadas (porta 8081) ou tente `npx expo start --tunnel` (pode pedir para instalar um pacote de túnel).
6. Para conferir o banco local: escolha um município no Início, feche o Expo Go por completo (deslizando-o para fora dos apps recentes) e reabra o app. O município escolhido deve continuar o mesmo.

`npm run android` (`expo run:android`) gera o app nativo e exige o Android Studio; não é necessário para o Expo Go.

O `app.json` está preparado para Android: `android.package` definido e `android.allowBackup: false` (ver "Dados no aparelho").

## Rodar contra a API em C#

1. Suba a API em C# (pasta `apps/api` da `main`; as instruções estão no README dela: PostgreSQL, Neo4j, `--migrate`, `--bootstrap`). Para o conhecimento sem LLM use `Llm__Mode=demo`. A integração com a LLM (Azure OpenAI) fica em outra branch e **não foi testada com o app**.
2. Se quiser dados de teste, rode `python scripts/demo_fluxo_http.py` da API. Ele publica Poá e um município fictício.
3. Configure o arquivo `.env` em `apps/mobile`:
   ```bash
   # No arquivo .env de apps/mobile (compatível com Windows e Linux):
   EXPO_PUBLIC_USAR_MOCK=false
   EXPO_PUBLIC_API_URL=http://127.0.0.1:5055/api/v1
   ```
   Depois, execute `npm run web` para desenvolvimento ou siga **Rodar a versão web exportada** para reproduzir as capturas. Se usar a API em outra porta, ajuste a URL.
4. Celular: a URL precisa usar o **IP do computador na rede Wi-Fi** (`http://<IP>:<porta>/api/v1`, veja o IP com `ipconfig`), a API precisa escutar em todas as interfaces (`--urls http://0.0.0.0:<porta>`) e o firewall do Windows precisa liberar a porta. Reinicie o Expo com `--clear` depois de mudar a variável. O CORS da API só afeta o navegador (origem padrão `http://localhost:8081`); o app nativo não usa CORS.

**Verificado (17/11/2026):** no navegador, contra a API local, o app listou municípios e serviços, enviou perguntas (a seguinte leva o histórico) e mostrou resposta com fonte e o aviso de informação insuficiente (prints acima). Essa evidência refere-se à integração web com a API em modo demonstração; não comprova resposta de LLM real nem tempo de resposta em produção.

## Dados no aparelho

O histórico e as preferências ficam em um banco SQLite local (`facil-municipio.db`), no aparelho, sem criptografia: a proteção vem do isolamento do aplicativo pelo sistema operacional. O app liga `secure_delete` para que os dados apagados sejam sobrescritos no arquivo, o que não garante que eles sejam irrecuperáveis. No Android, `allowBackup: false` impede o backup automático para o Google Drive. **Isso vale só para o Android**: o comportamento de backup no iOS não foi configurado nem verificado.

## Configuração

Copie `.env.example` para `.env` para alterar a configuração. O `.env` não é versionado.

- `EXPO_PUBLIC_USAR_MOCK`: `true` por padrão. Com `false`, o app usa `EXPO_PUBLIC_API_URL`.
- `EXPO_PUBLIC_API_URL`: URL base da API, sem barra no fim. A API em C# (`apps/api` na `main`) expõe as rotas em `/api/v1`, então o valor é `http://<host>:<porta>/api/v1`. Testado em 17/11/2026 no navegador contra a API local; ver "Rodar contra a API em C#".
- `EXPO_PUBLIC_MOCK_LATENCIA_MS`: atraso simulado do mock, 600 por padrão.

Com `EXPO_PUBLIC_USAR_MOCK=true`, o app usa dados de exemplo locais, sem links e sem rede (veja "Camada de API e dados mockados" em `mobile-app.md`).

Variáveis `EXPO_PUBLIC_*` ficam visíveis no app. Não coloque segredos nelas.

## Estrutura

`app/` rotas finas (abas em `(tabs)`, conversa em `conversa/[id]`), `screens/` composição das telas, `components/` interface reutilizável, `hooks/` casos de uso, `services/` acesso externo (`api/` com cliente real e mock, `banco/` com SQLite), `contexts/` estado compartilhado, `theme/` tokens, `types/` contrato e domínio, `config/` ambiente e limites, `utils/` funções puras.

## Entrega e acesso

O README, os prints e o vídeo estão disponíveis nesta branch. Para a avaliação, utilize o link direto de `feature/mobile/apps/mobile` informado na ficha; o conteúdo da branch pode diferir da `main`. Se o repositório estiver privado, o avaliador precisa ter permissão de leitura. O vídeo está publicado no link acima.

O [documento técnico](../../docs/features/mobile-app.md) mantém o histórico das decisões e verificações do desenvolvimento. Para o escopo e as instruções atuais da entrega Mobile, considere este README.
