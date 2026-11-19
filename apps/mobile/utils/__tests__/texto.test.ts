import { normalizarTexto } from '../texto';

describe('normalizarTexto', () => {
  it('remove acentos e coloca em minúsculas', () => {
    expect(normalizarTexto('Iluminação Pública')).toBe('iluminacao publica');
    expect(normalizarTexto('Lixo ELETRÔNICO')).toBe('lixo eletronico');
  });

  it('colapsa espaços e remove as pontas', () => {
    expect(normalizarTexto('  retirada   de  sofá ')).toBe('retirada de sofa');
  });

  it('mantém texto vazio vazio', () => {
    expect(normalizarTexto('   ')).toBe('');
  });
});
