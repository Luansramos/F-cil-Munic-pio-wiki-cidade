const mockAbrirBanco = jest.fn();

jest.mock('../abrirBanco', () => ({
  abrirBanco: () => mockAbrirBanco(),
}));

import { criarBancoEmMemoria } from '../testes/bancoEmMemoria';

type ModuloIndice = typeof import('../index');

function carregarIndice(): ModuloIndice {
  let modulo: ModuloIndice | undefined;
  jest.isolateModules(() => {
    modulo = jest.requireActual<ModuloIndice>('../index');
  });
  if (modulo === undefined) {
    throw new Error('Não foi possível carregar o módulo.');
  }
  return modulo;
}

beforeEach(() => {
  mockAbrirBanco.mockReset();
});

describe('abrirRepositorios', () => {
  it('abre o banco uma única vez, mesmo com chamadas repetidas', async () => {
    const memoria = criarBancoEmMemoria();
    mockAbrirBanco.mockResolvedValue(memoria.db);
    const { abrirRepositorios } = carregarIndice();

    const [primeiro, segundo] = await Promise.all([abrirRepositorios(), abrirRepositorios()]);

    expect(primeiro).toBe(segundo);
    expect(mockAbrirBanco).toHaveBeenCalledTimes(1);
    memoria.fechar();
  });

  it('depois de uma falha, a chamada seguinte tenta abrir de novo', async () => {
    const memoria = criarBancoEmMemoria();
    mockAbrirBanco.mockRejectedValueOnce(new Error('falhou')).mockResolvedValue(memoria.db);
    const { abrirRepositorios } = carregarIndice();

    await expect(abrirRepositorios()).rejects.toThrow('falhou');
    const repositorios = await abrirRepositorios();

    expect(mockAbrirBanco).toHaveBeenCalledTimes(2);
    expect(typeof repositorios.conversas.salvarTroca).toBe('function');
    memoria.fechar();
  });
});
