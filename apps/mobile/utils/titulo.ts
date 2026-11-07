import { TITULO_CONVERSA_MAX } from '@/config/limites';

export function tituloDaConversa(pergunta: string): string {
  const limpa = pergunta.replace(/\s+/g, ' ').trim();
  if (limpa.length <= TITULO_CONVERSA_MAX) {
    return limpa;
  }
  return `${limpa.slice(0, TITULO_CONVERSA_MAX - 1).trimEnd()}…`;
}
