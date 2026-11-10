import { HISTORICO_CAMPO_MAX, TROCAS_NO_CONTEXTO } from '@/config/limites';
import type { Troca, TrocaResumida } from '@/types';

export function cortarTexto(texto: string, maximo: number): string {
  if (texto.length <= maximo) {
    return texto;
  }
  return `${texto.slice(0, maximo - 1).trimEnd()}…`;
}

export function montarHistorico(trocas: Troca[]): TrocaResumida[] {
  return trocas
    .slice(-TROCAS_NO_CONTEXTO)
    .map((troca) => ({
      pergunta: cortarTexto(troca.pergunta.trim(), HISTORICO_CAMPO_MAX),
      resposta: cortarTexto(troca.resposta.trim(), HISTORICO_CAMPO_MAX),
    }))
    .filter((troca) => troca.pergunta !== '' && troca.resposta !== '');
}
