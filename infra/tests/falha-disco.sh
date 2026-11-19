#!/usr/bin/env bash
# Teste de falha 3: enche o disco acima do limite e depois libera
export LOG_FILE=/var/log/nexo/testes.log
source "$(dirname "$0")/../lib/log.sh"
[ "$EUID" -eq 0 ] || { echo "rode com sudo"; exit 1; }
LIM=$(grep -E '^DISK_MAX=' /etc/nexo/nexo.env | cut -d= -f2)
read -r used avail <<<"$(df -B1 / | awk 'NR==2{print $3, $4}')"
alvo=$(( (used+avail)*(LIM+3)/100 - used ))
if [ "$alvo" -le 0 ]; then log INFO "Disco ja acima do limite"; exit 0; fi
log WARN "Criando arquivo de $((alvo/1024/1024)) MB para passar de ${LIM}%"
fallocate -l "$alvo" /var/tmp/enche.img
sleep 25
tail -n 5 /var/log/nexo/alertas.log
rm -f /var/tmp/enche.img
log INFO "Arquivo removido, disco liberado"
