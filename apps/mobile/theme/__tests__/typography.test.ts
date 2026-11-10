import {
  NIVEIS_TEXTO,
  NIVEL_TEXTO_PADRAO,
  escalarEstilo,
  multiplicadorDoNivel,
  typography,
} from '../typography';

describe('escalarEstilo', () => {
  it('no nível padrão mantém os tamanhos do mockup', () => {
    expect(NIVEL_TEXTO_PADRAO).toBe(0);
    expect(escalarEstilo(typography.body, NIVEL_TEXTO_PADRAO)).toEqual(typography.body);
  });

  it('aumenta fonte e altura da linha a cada nível, sem mudar o peso', () => {
    for (const token of Object.keys(typography) as (keyof typeof typography)[]) {
      const tamanhos = NIVEIS_TEXTO.map((nivel) => escalarEstilo(typography[token], nivel));

      for (let indice = 1; indice < tamanhos.length; indice += 1) {
        expect(tamanhos[indice].fontSize).toBeGreaterThan(tamanhos[indice - 1].fontSize);
        expect(tamanhos[indice].lineHeight).toBeGreaterThan(tamanhos[indice - 1].lineHeight);
        expect(tamanhos[indice].fontWeight).toBe(typography[token].fontWeight);
      }
    }
  });

  it('usa os multiplicadores provisórios e arredonda', () => {
    expect(multiplicadorDoNivel(3)).toBe(1.5);
    expect(escalarEstilo(typography.body, 3)).toEqual({
      fontSize: 26,
      lineHeight: 36,
      fontWeight: '400',
    });
  });
});
