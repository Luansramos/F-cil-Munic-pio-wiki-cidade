#!/usr/bin/env bash
# Funcao de log padronizada (auditoria). Uso: log INFO "mensagem"
LOG_DIR=/var/log/nexo
mkdir -p "$LOG_DIR"
LOG_FILE="${LOG_FILE:-$LOG_DIR/provision.log}"
log() {
  local nivel="$1"; shift
  echo "$(date '+%F %T') [$nivel] [$(basename "$0")] $*" | tee -a "$LOG_FILE"
}

habilitar_trap_erro() {
  set -o errtrace
  trap 'rc=$?; log ERROR "falha (codigo $rc) na linha $LINENO: $BASH_COMMAND"' ERR
}
