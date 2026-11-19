# Vídeo de Cybersecurity (Remotion)

Vídeo de 3 min 35 s (1920×1080, 30 fps) da entrega de Cybersecurity, feito com [Remotion](https://www.remotion.dev/) (React). Usa **capturas reais** do painel `/auditoria/` e a narração `Facil_Municipio_Narracao_PTBR.mp3` já gravada pela equipe.

> Licença do Remotion: uso gratuito para indivíduos e equipes pequenas (consulte a licença atual antes de usar comercialmente).

## Como foi montado
- **Áudio:** `public/narracao.mp3` (214,6 s, não versionado). Os cortes das 12 cenas seguem as **11 pausas longas** (~1,3 s) dessa faixa (`BOUNDS_S` em `src/theme.tsx`). Não foi possível transcrever o áudio; a correspondência entre cada trecho falado e cada cena assume a mesma ordem do vídeo anterior (abertura, contexto, parsing, taint, bloqueio, auditoria, IA, dashboard, explicação, acesso, testes, limites). **Ouça uma vez conferindo.**
- **Imagens:** `public/evidencias/` (capturas de `docs/evidencias-cybersecurity/`) e `public/capturas/` (recortes reais da sequência ao vivo: evento recém-criado com mensagem padrão → explicação do Gemini ≈ 10 s depois).
- **Números** mostrados (12 eventos, 10 sanitizações, 2 bloqueios, 99 testes, 401/200/403) vêm das evidências do repositório.
- **Fora do vídeo:** o app mobile (não faz parte dos requisitos de Cybersecurity); aparece só como limite.

## Comandos
```bash
cd video
npm install
npm run studio     # pré-visualização
npm run render     # gera out/Facil_Municipio_Cybersecurity_Remotion_1080p.mp4
```
Se o Chrome do Remotion não for baixado, use o instalado: `--browser-executable="C:/Program Files/Google/Chrome/Application/chrome.exe" --chrome-mode=chrome-for-testing`.

## Trocar a narração (ex.: ElevenLabs)
Substitua `public/narracao.mp3` e ajuste `BOUNDS_S` (início/fim de cada cena em segundos) em `src/theme.tsx`. Chaves de API de provedores de voz **não** devem ser colocadas no repositório; use variáveis de ambiente no seu terminal.
