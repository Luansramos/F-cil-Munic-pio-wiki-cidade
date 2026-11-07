# App mobile do cidadão

App Expo (SDK 58) do Fácil Município. Protótipo acadêmico em desenvolvimento. O escopo, a arquitetura, as decisões e as verificações estão em [docs/features/mobile-app.md](../../docs/features/mobile-app.md).

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

## Rodar no Expo Go em um celular Android (NÃO VERIFICADO)

Este passo a passo foi escrito a partir da documentação da Expo e **não foi executado**: não há Android Studio nem aparelho Android nesta máquina. Se algo divergir, corrija aqui.

1. No celular, instale o **Expo Go** pela Google Play e atualize para a versão mais recente. O SDK 58 exige um Expo Go compatível (a API de versões da Expo lista a 58.0.5 para Android).
2. Conecte o celular e o computador **na mesma rede Wi-Fi**.
3. No computador, em `apps/mobile`:
   ```bash
   npm install
   npm start
   ```
4. No terminal aparece um QR code. No Android, abra o Expo Go e toque em **Scan QR code**. A câmera do celular sozinha pode não abrir o Expo Go.
5. Se o celular não achar o computador, libere o Node.js no firewall do Windows para redes privadas (porta 8081) ou tente `npx expo start --tunnel` (pode pedir para instalar um pacote de túnel).
6. Para conferir o banco local: escolha "Município Exemplo" no Início, feche o Expo Go por completo (deslizando-o para fora dos apps recentes) e reabra o app. O município escolhido deve continuar o mesmo.

`npm run android` (`expo run:android`) gera o app nativo e exige o Android Studio; não é necessário para o Expo Go.

O `app.json` está preparado para Android: `android.package` definido e `android.allowBackup: false` (ver "Dados no aparelho"). Nada disso foi testado em Android.

## Dados no aparelho

O histórico e as preferências ficam em um banco SQLite local (`facil-municipio.db`), no aparelho, sem criptografia: a proteção vem do isolamento do aplicativo pelo sistema operacional. O app liga `secure_delete` para que os dados apagados sejam sobrescritos no arquivo, o que não garante que eles sejam irrecuperáveis. No Android, `allowBackup: false` impede o backup automático para o Google Drive. **Isso vale só para o Android**: o comportamento de backup no iOS não foi configurado nem verificado.

## Configuração

Copie `.env.example` para `.env` para alterar a configuração. O `.env` não é versionado.

- `EXPO_PUBLIC_USAR_MOCK`: `true` por padrão. A API real ainda não existe.
- `EXPO_PUBLIC_API_URL`: URL da API. Vazia por enquanto.
- `EXPO_PUBLIC_MOCK_LATENCIA_MS`: atraso simulado do mock, 600 por padrão.

Com `EXPO_PUBLIC_USAR_MOCK=true`, o app usa dados de exemplo locais, sem links e sem rede (veja "Camada de API e dados mockados" em `mobile-app.md`).

Variáveis `EXPO_PUBLIC_*` ficam visíveis no app. Não coloque segredos nelas.

## Estrutura

`app/` rotas finas (abas em `(tabs)`, conversa em `conversa/[id]`), `screens/` composição das telas, `components/` interface reutilizável, `hooks/` casos de uso, `services/` acesso externo (`api/` com cliente real e mock, `banco/` com SQLite), `contexts/` estado compartilhado, `theme/` tokens, `types/` contrato e domínio, `config/` ambiente e limites, `utils/` funções puras.
