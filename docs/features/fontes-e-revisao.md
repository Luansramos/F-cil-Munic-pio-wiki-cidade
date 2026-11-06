# Feature — Fontes e revisão da wiki

- Status: planejada
- Responsáveis sugeridos: P5 e P1; extração P3
- Backlog: B1–B8

## Objetivo e escopo

Permitir que administrador autorizado informe URL, texto e data de uma fonte, receba proposta estruturada e revise antes de publicar. Extração automática de PDF e URL é opcional.

## Integração e critérios de aceite

- Painel → API → PostgreSQL e LLM, conforme [contratos](../contratos-de-integracao.md).
- Administrador gerencia apenas seu município.
- Proposta é editável e continua rascunho até aprovação.
- Campos sem evidência não viram fatos; fonte e revisão são registradas.

## Verificações e pendências

Não implementado nem verificado. Registrar cenários de extração inválida, revisão e autorização quando executados.
