import type { NomeIcone } from '@/components/Icon';
import type { Servico } from '@/types';

const ICONES_POR_SERVICO: Record<string, NomeIcone> = {
  'coleta-de-lixo': 'lixo',
  'iluminacao-publica': 'lampada',
  'cata-treco': 'sofa',
};

export function iconeDoServico(servicoId: string): NomeIcone {
  return ICONES_POR_SERVICO[servicoId] ?? 'conversa';
}

export function perguntaSugerida(servico: Servico): string {
  return `Quero saber sobre ${servico.nome.toLowerCase()}.`;
}
