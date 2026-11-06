# Feature — Infraestrutura e IoT

- Status: **em andamento** — infraestrutura Linux (G1–G4) implementada com aplicação placeholder; IoT (G5–G7) planejado, integração Wokwi a comprovar
- Responsável e colaboradores: Matheus (Operating Systems); apoio P1; Physical Computing a confirmar com o grupo
- Itens do backlog / issue: G1–G7
- Componentes e disciplinas afetados: `infra/`, Operating Systems, Physical Computing
- Última atualização: 06/11/2026

## Problema e objetivo

Provisionar uma aplicação em Linux, monitorar recursos e falhas e demonstrar um totem com ESP32 no Wokwi integrado à proposta.

## Escopo e limites

- **Operating Systems (feito):** scripts Bash de provisionamento (rede, DNS, IP, aplicação), monitor de CPU/memória/disco com alertas, serviços systemd com `Restart=always`, logs de auditoria, testes de falha. Documentação em [infra/README.md](../../infra/README.md).
- **Limite atual:** a aplicação hospedada é um placeholder; a API C# e os bancos ainda não foram integrados.
- **Physical Computing:** não iniciado nesta branch.

## Fluxo e contratos de integração

Contrato esperado do responsável pela API: endpoint `/health`, lista de variáveis de ambiente, comando de build (`dotnet publish`), porta e definição dos bancos (Docker Compose). Nenhuma rota ou contrato existente foi alterado.

## Dependências e decisões

- Distribuição: Debian 13; VirtualBox 7.2.6; NetworkManager (`nmcli`) para IP fixo.
- Bancos na VM por Docker Compose: proposta, a alinhar com o responsável pela API.
- Nenhuma decisão arquitetural do projeto foi alterada.

## Critérios de aceite

- Scripts automatizam infraestrutura e execução; monitoramento produz logs e alertas e roda como serviço tolerante a falhas.
- Vídeo mostra provisionamento e falha/recuperação em até 7 minutos.
- Circuito funciona, sua função é explicada e integração é comprovada ou a limitação registrada e a proposta ajustada.
- Link Wokwi, vídeo de até 4 minutos e arquivo de integrantes estão preparados.

## Verificações e evidências

Executado em 06/11/2026 numa VM Debian 13 (VirtualBox 7.2.6), com a aplicação placeholder:

- `01-infra.sh` e `02-app.sh`: concluídos sem erro; app respondeu em `:5000`; `02-app.sh` reexecutado com sucesso.
- `falha-app.sh`: serviço recuperado pelo systemd após `kill -9` (PID 2256 → 2401).
- Alertas observados em `alertas.log`: `CPU alta: 100%`, `Memoria alta: 87%/88%`, `Disco cheio: 84%`.
- Reinício da VM: serviços `active`, IP fixo e hostname preservados.

**Não verificado:**
- Provisionamento a partir de snapshot limpo, de ponta a ponta (a repetir antes do vídeo).
- Aplicação real (API C#) e bancos na VM.
- Circuito ESP32 e comunicação Wokwi ↔ API.
- Vídeo de demonstração.

## Riscos, pendências e próximos passos

- Integrar a API real quando disponível (2 pts de "funcionamento completo").
- Reensaiar o provisionamento do zero e gravar o vídeo (até 7 min).
- Confirmar cedo a comunicação entre simulador e API; não representar simulação desconectada como integração concluída.
- Adicionar prints em `infra/evidencias/`.
