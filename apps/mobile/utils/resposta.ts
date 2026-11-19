/** Preserva a explicação segura da API para insuficiência ou bloqueio de privacidade. */
export function mensagemIndisponivel(resposta: string): string {
  return resposta.trim() || 'Não foi possível apresentar uma orientação. Consulte o canal oficial.';
}
