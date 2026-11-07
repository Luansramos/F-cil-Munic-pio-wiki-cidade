# App mobile do cidadão

App Expo (SDK 58) do Fácil Município. Protótipo acadêmico em desenvolvimento. O escopo, a arquitetura, as decisões e as verificações estão em [docs/features/mobile-app.md](../../docs/features/mobile-app.md).

## Comandos verificados

Dentro de `apps/mobile`:

```bash
npm install
npm run typecheck
npm run lint
npm test
npx expo-doctor
npx expo export --platform android
npx expo export --platform web
```

Para abrir na web durante o desenvolvimento: `npm run web` (`expo start --web`). O `npx expo run` não serve para isso. A abertura no navegador, `npx expo start` e o Expo Go ainda não foram verificados.

## Configuração

Copie `.env.example` para `.env` para alterar a configuração. O `.env` não é versionado.

- `EXPO_PUBLIC_USAR_MOCK`: `true` por padrão. A API real ainda não existe.
- `EXPO_PUBLIC_API_URL`: URL da API. Vazia por enquanto.
- `EXPO_PUBLIC_MOCK_LATENCIA_MS`: atraso simulado do mock, 600 por padrão.

Com `EXPO_PUBLIC_USAR_MOCK=true`, o app usa dados de exemplo locais, sem links e sem rede (veja "Camada de API e dados mockados" em `mobile-app.md`).

Variáveis `EXPO_PUBLIC_*` ficam visíveis no app. Não coloque segredos nelas.

## Estrutura

`app/` rotas finas (abas em `(tabs)`, conversa em `conversa/[id]`), `screens/` composição das telas, `components/` interface reutilizável, `hooks/` casos de uso, `services/` acesso externo (API real e mock), `contexts/` estado compartilhado, `theme/` tokens, `types/` contrato e domínio, `config/` ambiente e limites, `utils/` funções puras. O SQLite ainda não existe.
