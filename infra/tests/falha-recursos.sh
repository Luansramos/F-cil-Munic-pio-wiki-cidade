#!/usr/bin/env bash
# Teste de falha 2: estressa CPU e memoria para disparar alertas
export LOG_FILE=/var/log/nexo/testes.log
source "$(dirname "$0")/../lib/log.sh"
[ "$EUID" -eq 0 ] || { echo "rode com sudo"; exit 1; }
log WARN "Estressando CPU por 40s"
stress-ng --cpu 0 --timeout 40s
log WARN "Estressando memoria por 40s"
stress-ng --vm 1 --vm-bytes 95% --timeout 40s
sleep 12
log INFO "Ultimos alertas:"
tail -n 10 /var/log/nexo/alertas.log
