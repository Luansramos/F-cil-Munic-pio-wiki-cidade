#!/usr/bin/env bash
# Provisionamento de infraestrutura: hostname, IP fixo, DNS e firewall
set -euo pipefail
source "$(dirname "$0")/../lib/log.sh"
habilitar_trap_erro
[ "$EUID" -eq 0 ] || { echo "rode com sudo"; exit 1; }

HOSTNAME_NEW="${HOSTNAME_NEW:-nexo-srv}"
IFACE="${IFACE:-enp0s8}"
IP_CIDR="${IP_CIDR:-192.168.56.10/24}"
DNS_SERVERS="${DNS_SERVERS:-8.8.8.8 1.1.1.1}"

log INFO "Definindo hostname: $HOSTNAME_NEW"
hostnamectl set-hostname "$HOSTNAME_NEW"
grep -q "$HOSTNAME_NEW" /etc/hosts || echo "127.0.1.1 $HOSTNAME_NEW" >> /etc/hosts

log INFO "Configurando IP fixo $IP_CIDR e DNS ($DNS_SERVERS) em $IFACE"
nmcli con delete nexo-lan 2>/dev/null || true
nmcli con add type ethernet ifname "$IFACE" con-name nexo-lan \
  ipv4.method manual ipv4.addresses "$IP_CIDR" ipv4.dns "$DNS_SERVERS"
nmcli con up nexo-lan

log INFO "Configurando firewall (ufw)"
apt-get install -y ufw >/dev/null
ufw allow 22/tcp
ufw allow 5000/tcp
ufw --force enable
log INFO "Infra concluida: $(hostname) | $(ip -br a show "$IFACE")"
