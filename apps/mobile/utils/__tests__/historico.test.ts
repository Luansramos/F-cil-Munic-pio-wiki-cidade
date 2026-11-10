import { HISTORICO_CAMPO_MAX, TROCAS_NO_CONTEXTO } from '@/config/limites';
import type { Troca } from '@/types';

import { cortarTexto, montarHistorico } from '../historico';

function troca(indice: number, sobrescrever: Partial<Troca> = {}): Troca {
  return {
    id: `t${indice}`,
    pergunta: `pergunta ${indice}`,
    resposta: `resposta ${indice}`,
    fontes: [],
    servicosRelacionados: [],
    informacaoSuficiente: true,
    criadoEm: '2026-11-09T12:00:00.000Z',
    ...sobrescrever,
  };
}

describe('cortarTexto', () => {
  it('mantém o texto dentro do limite', () => {
    expect(cortarTexto('abc', 3)).toBe('abc');
  });

  it('corta com reticências sem passar do limite', () => {
    const cortado = cortarTexto('a'.repeat(50), 10);

    expect(cortado).toHaveLength(10);
    expect(cortado.endsWith('…')).toBe(true);
  });
});

describe('montarHistorico', () => {
  it('retorna lista vazia sem trocas', () => {
    expect(montarHistorico([])).toEqual([]);
  });

  it('usa só as últimas trocas, em ordem, sem ultrapassar o limite do contexto', () => {
    const trocas = [1, 2, 3, 4, 5].map((indice) => troca(indice));

    const historico = montarHistorico(trocas);

    expect(historico).toHaveLength(TROCAS_NO_CONTEXTO);
    expect(historico.map((item) => item.pergunta)).toEqual([
      'pergunta 3',
      'pergunta 4',
      'pergunta 5',
    ]);
  });

  it('envia só pergunta e resposta', () => {
    expect(montarHistorico([troca(1)])).toEqual([
      { pergunta: 'pergunta 1', resposta: 'resposta 1' },
    ]);
  });

  it('corta respostas longas no limite aceito pela API', () => {
    const historico = montarHistorico([troca(1, { resposta: 'x'.repeat(HISTORICO_CAMPO_MAX + 500) })]);

    expect(historico[0].resposta).toHaveLength(HISTORICO_CAMPO_MAX);
  });

  it('descarta trocas com campo vazio depois de aparar', () => {
    const historico = montarHistorico([troca(1, { resposta: '   ' }), troca(2)]);

    expect(historico).toEqual([{ pergunta: 'pergunta 2', resposta: 'resposta 2' }]);
  });
});
