#!/usr/bin/env bash
# Monitor de CPU, memoria e disco. Gera log e alertas.
export LOG_FILE=/var/log/nexo/monitor.log
source /opt/nexo/lib/log.sh
CPU_MAX="${CPU_MAX:-80}"; MEM_MAX="${MEM_MAX:-80}"
DISK_MAX="${DISK_MAX:-80}"; INTERVAL="${INTERVAL:-10}"

alerta() { log ALERT "$1"; echo "$(date '+%F %T') $1" >> /var/log/nexo/alertas.log; }

log INFO "Monitor iniciado (CPU>${CPU_MAX}% MEM>${MEM_MAX}% DISCO>${DISK_MAX}%)"
while true; do
  idle=$(LC_ALL=C top -bn2 -d1 | grep "Cpu(s)" | tail -1 | sed 's/.*[, ]\([0-9.]*\) id.*/\1/')
  cpu=$(awk -v i="${idle:-0}" 'BEGIN{printf "%.0f", 100-i}')
  mem=$(LC_ALL=C free | awk '/Mem:/ {printf "%.0f", $3/$2*100}')
  disco=$(df / | awk 'NR==2 {gsub("%","",$5); print $5}')
  log INFO "cpu=${cpu}% mem=${mem}% disco=${disco}%"
  [ "$cpu" -gt "$CPU_MAX" ]    && alerta "CPU alta: ${cpu}%"
  [ "$mem" -gt "$MEM_MAX" ]    && alerta "Memoria alta: ${mem}%"
  [ "$disco" -gt "$DISK_MAX" ] && alerta "Disco cheio: ${disco}%"
  systemctl is-active --quiet nexo-app || alerta "Servico nexo-app FORA do ar"
  sleep "$INTERVAL"
done
