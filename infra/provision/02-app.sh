#!/usr/bin/env bash
# Provisionamento da aplicacao: dependencias, usuario, variaveis, servicos
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
source "$ROOT/lib/log.sh"
[ "$EUID" -eq 0 ] || { echo "rode com sudo"; exit 1; }

log INFO "Instalando dependencias"
apt-get update -y
apt-get install -y python3 curl stress-ng

log INFO "Criando usuario e diretorios"
id nexo &>/dev/null || useradd --system --home /opt/nexo --shell /usr/sbin/nologin nexo
mkdir -p /opt/nexo/app /opt/nexo/lib /opt/nexo/monitor /etc/nexo
cp -r "$ROOT/app/." /opt/nexo/app/
cp "$ROOT/lib/log.sh" /opt/nexo/lib/
cp "$ROOT/monitor/monitor.sh" /opt/nexo/monitor/
chmod +x /opt/nexo/monitor/monitor.sh
chown -R nexo:nexo /opt/nexo /var/log/nexo

log INFO "Gerando variaveis de ambiente"
if [ ! -f /etc/nexo/nexo.env ]; then
cat > /etc/nexo/nexo.env <<EOT
APP_PORT=5000
CPU_MAX=80
MEM_MAX=80
DISK_MAX=80
INTERVAL=10
EOT
fi

log INFO "Instalando servicos systemd e logrotate"
cp "$ROOT"/systemd/*.service /etc/systemd/system/
cp "$ROOT/logrotate/nexo" /etc/logrotate.d/nexo
systemctl daemon-reload
systemctl enable nexo-app nexo-monitor
systemctl restart nexo-app nexo-monitor

sleep 2
if curl -fs http://localhost:5000/ >/dev/null; then
  log INFO "Aplicacao respondendo em :5000"
else
  log ERROR "Aplicacao NAO respondeu"; exit 1
fi
