# Infraestrutura Linux — Nexo Cidadão

Entrega de **Operating Systems** (GS CitizenTech 2026.2). Provisiona, de forma automática, uma VM Linux que hospeda uma aplicação web, monitora CPU/memória/disco, gera logs de auditoria e se recupera de falhas.

> **Estado:** a infraestrutura está implementada e verificada com uma **aplicação web própria da camada de infraestrutura** (`app/`): uma página de status do Nexo Cidadão com CPU, memória, disco, serviços e alertas em tempo real, mais os endpoints `/health` e `/api/status`. A API real do Nexo (C#/ASP.NET Core) ainda **não** foi integrada; a troca é descrita em [Integração com a API real](#integração-com-a-api-real).

## Integrantes

- Fernanda Rocha Menon — RM 554673
- Luiza Macena Dantas — RM 556237
- Luan Ramos Garcia de Souza — RM 558537
- Matheus Ricciotti — RM 556930
- Matheus Bortolotto — RM 555189

Vídeo de demonstração (3 min 47 s): **https://youtu.be/xE7B7_obxFU**

## Ambiente testado

| Item | Valor |
|---|---|
| Hipervisor | VirtualBox 7.2.6 (Windows) |
| SO convidado | Debian GNU/Linux 13 (trixie) amd64 |
| Recursos da VM | 4 GB RAM, 2 vCPUs, 30 GB de disco |
| Rede | Adaptador 1 NAT (port forward) + Adaptador 2 Host-only |
| Gerenciador de rede | NetworkManager (`nmcli`) |
| Boot | `multi-user.target` (sem interface gráfica) |

Os scripts usam `apt`, `nmcli`, `ufw` e `systemd`. Em outras distribuições é preciso adaptar.

## Estrutura

```
infra/
├── lib/log.sh                    # função de log padronizada (auditoria)
├── provision/
│   ├── 01-infra.sh               # hostname, IP fixo, DNS, firewall
│   └── 02-app.sh                 # dependências, usuário, variáveis, serviços
├── app/
│   ├── app.py                    # servidor web (stdlib) com /health e /api/status
│   └── static/index.html         # página de status
├── monitor/monitor.sh            # monitor de CPU, memória e disco
├── systemd/
│   ├── nexo-app.service          # aplicação (Restart=always)
│   └── nexo-monitor.service      # monitor (Restart=always)
├── logrotate/nexo                # rotação dos logs
└── tests/
    ├── falha-app.sh              # kill -9 na aplicação
    ├── falha-recursos.sh         # estresse de CPU e memória
    ├── falha-disco.sh            # enche o disco acima do limite
    └── falha-script.sh           # falha controlada: o erro deve ir para o log
```

## Preparar a VM

1. Instalar o Debian 13 (netinst) no VirtualBox com 4 GB de RAM, 2 vCPUs e 30 GB.
2. Com a VM desligada, configurar a rede (PowerShell no host; substitua o nome da VM):
   ```powershell
   cd "C:\Program Files\Oracle\VirtualBox"
   .\VBoxManage.exe modifyvm "NOME-DA-VM" --natpf1 "ssh,tcp,,2222,,22"
   .\VBoxManage.exe modifyvm "NOME-DA-VM" --natpf1 "app,tcp,,8080,,5000"
   .\VBoxManage.exe modifyvm "NOME-DA-VM" --nic2 hostonly --hostonlyadapter2 "VirtualBox Host-Only Ethernet Adapter"
   ```
3. Na VM, como root: instalar o básico e desativar a interface gráfica (opcional).
   ```bash
   apt update && apt install -y sudo openssh-server curl
   usermod -aG sudo <usuario>
   systemctl set-default multi-user.target
   ip -br a        # confirmar o nome da interface host-only (aqui: enp0s8)
   ```
4. Tirar um snapshot da VM limpa (`ponto-zero`).

## Executar o provisionamento

Do host, copie a pasta e conecte por SSH:
```powershell
scp -P 2222 -r infra <usuario>@127.0.0.1:~
ssh -p 2222 <usuario>@127.0.0.1
```
Na VM:
```bash
sudo bash infra/provision/01-infra.sh   # rede, DNS, IP fixo, firewall
sudo bash infra/provision/02-app.sh     # dependências, usuário, serviços
```
Verificar:
```bash
systemctl status nexo-app nexo-monitor --no-pager
curl localhost:5000/health               # no host, abrir http://localhost:8080
tail -n 5 /var/log/nexo/monitor.log
```
Os scripts podem ser reexecutados: o `02-app.sh` não sobrescreve `/etc/nexo/nexo.env` e reinicia os serviços.

## Variáveis

`01-infra.sh` (ambiente do shell, com padrões):

| Variável | Padrão | Função |
|---|---|---|
| `HOSTNAME_NEW` | `nexo-srv` | hostname da VM |
| `IFACE` | `enp0s8` | interface da rede host-only |
| `IP_CIDR` | `192.168.56.10/24` | IP fixo |
| `DNS_SERVERS` | `8.8.8.8 1.1.1.1` | servidores DNS |

`/etc/nexo/nexo.env` (criado pelo `02-app.sh`, lido pelos serviços):

| Variável | Padrão | Função |
|---|---|---|
| `APP_PORT` | `5000` | porta da aplicação |
| `CPU_MAX` / `MEM_MAX` / `DISK_MAX` | `80` | limite (%) para alerta |
| `INTERVAL` | `10` | intervalo do monitor, em segundos |

Não há segredos neste repositório. Valores sensíveis da API real devem ficar em `/etc/nexo/nexo.env`, fora do Git.

## O que cada parte faz

| Critério | Implementação |
|---|---|
| Provisionamento de infra (rede, DNS, IP) | `01-infra.sh`: hostname, `/etc/hosts`, IP fixo e DNS via `nmcli`, firewall `ufw` (portas 22 e 5000) |
| Provisionamento da aplicação | `02-app.sh`: dependências, usuário de serviço `nexo`, diretórios, `nexo.env`, units systemd, logrotate |
| Monitoramento com alertas | `monitor.sh`: lê CPU, memória e disco a cada `INTERVAL` s; acima do limite registra `ALERT` e grava em `alertas.log`; também alerta se `nexo-app` estiver fora do ar |
| Monitor como serviço tolerante a falhas | `nexo-monitor.service` com `Restart=always` e sem limite de reinícios |
| Logs de auditoria e execução | `lib/log.sh`: timestamp, nível e script de origem, em `/var/log/nexo/`; `habilitar_trap_erro` registra como `ERROR` qualquer comando que falhe (código, linha e comando) |
| Funcionamento completo | aplicação web servida como serviço systemd (`:5000`, no host `:8080`), lendo o estado real da VM; troca pela API real descrita abaixo |

## Logs

| Arquivo (`/var/log/nexo/`) | Conteúdo |
|---|---|
| `provision.log` | execução dos scripts de provisionamento e erros de scripts (`ERROR`) |
| `monitor.log` | leituras periódicas de CPU, memória e disco |
| `alertas.log` | alertas de limite e de serviço fora do ar |
| `testes.log` | execução dos testes de falha |

## Testes de falha

Rodar como `sudo`, na VM provisionada.

| Teste | Comando | Resultado observado (06/11/2026) |
|---|---|---|
| Aplicação morta | `bash infra/tests/falha-app.sh` | `kill -9` no PID 3255; o systemd reiniciou o serviço (novo PID 3603) em 6 s; `/health` voltou a responder; alerta `Servico nexo-app FORA do ar` |
| Página de status | abrir `http://localhost:8080` | serviços `active`; durante o estresse, as barras passam de 80% e os alertas aparecem na página |
| CPU | `bash infra/tests/falha-recursos.sh` | alertas `CPU alta: 100%` |
| Memória | `stress-ng --vm 1 --vm-bytes 95% --timeout 40s` | alertas `Memoria alta: 87%` e `88%` |
| Disco | `bash infra/tests/falha-disco.sh` | alerta `Disco cheio: 84%`; arquivo removido ao final |
| Provisionamento do zero | restaurar o snapshot limpo e rodar `01-infra.sh` e `02-app.sh` | concluído sem erro em 24 s; serviços `active` e app respondendo em `:5000` |
| Monitor morto | `sudo kill -9 $(systemctl show nexo-monitor -p MainPID --value)` | PID 1368 morto; o systemd recriou o processo (PID 1532) em poucos segundos e o serviço voltou `active` |
| Falha controlada em script | `sudo bash infra/tests/falha-script.sh` | `cp` de arquivo inexistente; o script sai com código 1 e o `provision.log` registra `[ERROR] [falha-script.sh] falha (codigo 1) na linha 7: cp ...` |
| Reinício da VM | `sudo reboot` | os dois serviços sobem `active`, IP `192.168.56.10/24` e hostname mantidos, monitor volta a logar |

## Integração com a API real

A API do projeto está em `apps/api/` (ASP.NET Core 10, PostgreSQL e Neo4j, ver `apps/api/README.md`). Ela ainda **não** foi integrada a esta infraestrutura. Os passos previstos são:

1. Instalar o .NET 10 (SDK para publicar, runtime para executar) e o Docker Compose na VM, adicionando-os ao `apt-get` do `02-app.sh`.
2. Subir PostgreSQL e Neo4j com `docker compose up -d` a partir de `compose.yaml`, usando um `.env` com senhas locais (fora do Git).
3. Publicar a API: `dotnet publish apps/api -c Release -o /opt/nexo/app`, e aplicar `--migrate` e `--bootstrap`.
4. Colocar `ConnectionStrings__Postgres`, `Neo4j__*`, `Jwt__Key` e `Bootstrap__*` em `/etc/nexo/nexo.env`.
5. Em `systemd/nexo-app.service`, trocar o `ExecStart` por `/usr/bin/dotnet /opt/nexo/app/FacilMunicipio.Api.dll`. A verificação final do `02-app.sh` hoje consulta `/`; com a API real, trocar para `/health`, que ela já expõe.

## Limitações e pendências

- A aplicação hospedada é uma página de status da infraestrutura, **não** a plataforma Nexo; a API real e seus bancos ainda não foram integrados.
- Testado apenas em Debian 13 com VirtualBox 7.2.6; não foi validado em outras distribuições.
- O monitor depende do formato de saída de `top` e `free` com `LC_ALL=C`.

## Evidências

Capturas de 06/11/2026, feitas numa VM restaurada do snapshot limpo, em [`evidencias/`](evidencias/):

| Arquivo | Mostra |
|---|---|
| `01-repositorio-github.png` | pasta `infra/` no repositório |
| `02-vm-limpa.png` | VM antes do provisionamento (`nexo-app` inexistente) |
| `03-estrutura-infra.png` | estrutura de `infra/` copiada para a VM |
| `06-servicos-e-monitor.png` | `nexo-app` e `nexo-monitor` ativos e leituras do monitor |
| `07-pagina-status.png` | página de status no navegador |
| `09-alertas-log.png` | alertas de CPU e memória registrados |
| `10-falha-app-recuperacao.png` | `kill -9` e recuperação pelo systemd |
| `11-logs-auditoria.png` | arquivos de log e execução dos scripts |
| `12-falha-script-erro-log.png` | falha controlada em script registrada como `ERROR` no log |
| `13-kill-monitor-recuperacao.png` | `kill -9` no `nexo-monitor` e recriação do processo (PID 1368 → 1532) |
| `14-reboot-persistencia.png` | após `reboot`: serviços `active` e `/health` ok |

Vídeo de demonstração (3 min 47 s): **https://youtu.be/xE7B7_obxFU**.
