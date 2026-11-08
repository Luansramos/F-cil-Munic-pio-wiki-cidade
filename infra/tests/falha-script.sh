#!/usr/bin/env bash
# Falha controlada: um comando falha e o erro deve ficar registrado no log
set -euo pipefail
source "$(dirname "$0")/../lib/log.sh"
habilitar_trap_erro
log INFO "Teste de falha controlada: copiando arquivo inexistente"
cp /arquivo/que/nao/existe /tmp/destino
log INFO "esta linha nao deve aparecer"
