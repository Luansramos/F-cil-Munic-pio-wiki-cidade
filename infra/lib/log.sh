#!/usr/bin/env bash
# Funcao de log padronizada (auditoria). Uso: log INFO "mensagem"
LOG_DIR=/var/log/nexo
mkdir -p "$LOG_DIR"
LOG_FILE="${LOG_FILE:-$LOG_DIR/provision.log}"
log() {
  local nivel="$1"; shift
  echo "$(date '+%F %T') [$nivel] [$(basename "$0")] $*" | tee -a "$LOG_FILE"
}
