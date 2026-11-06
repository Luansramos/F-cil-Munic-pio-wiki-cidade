#!/usr/bin/env bash
# Teste de falha 1: mata a aplicacao e verifica a recuperacao pelo systemd
export LOG_FILE=/var/log/nexo/testes.log
source "$(dirname "$0")/../lib/log.sh"
[ "$EUID" -eq 0 ] || { echo "rode com sudo"; exit 1; }
pid=$(systemctl show -p MainPID --value nexo-app)
log WARN "Matando nexo-app (PID $pid) com kill -9"
kill -9 "$pid"
sleep 6
novo=$(systemctl show -p MainPID --value nexo-app)
if systemctl is-active --quiet nexo-app; then
  log INFO "RECUPERADO: systemd reiniciou o servico (PID antigo $pid, novo $novo)"
  curl -s http://localhost:5000/; echo
else
  log ERROR "Servico NAO recuperou"
fi
