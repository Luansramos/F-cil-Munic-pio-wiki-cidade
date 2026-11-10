const mockAbrir = jest.fn();

jest.mock('expo-sqlite', () => ({
  openDatabaseAsync: (...argumentos: unknown[]) => mockAbrir(...argumentos),
}));

import { NOME_DO_BANCO, abrirBanco } from '../abrirBanco';

function criarBancoFalso(versao = 1) {
  return {
    execAsync: jest.fn(async () => undefined),
    getFirstAsync: jest.fn(async () => ({ user_version: versao })),
    getAllAsync: jest.fn(async () => []),
    runAsync: jest.fn(async () => ({ lastInsertRowId: 0, changes: 0 })),
    withTransactionAsync: jest.fn(async () => undefined),
    closeAsync: jest.fn(async () => undefined),
  };
}

beforeEach(() => {
  mockAbrir.mockReset();
});

describe('abrirBanco', () => {
  it('abre o banco pelo nome fixo e liga os PRAGMAs antes de ler a versão do esquema', async () => {
    const falso = criarBancoFalso();
    mockAbrir.mockResolvedValue(falso);

    const resultado = await abrirBanco();

    expect(resultado).toBe(falso);
    expect(mockAbrir).toHaveBeenCalledWith(NOME_DO_BANCO);
    const [sqlDosPragmas] = falso.execAsync.mock.calls[0] as unknown as [string];
    expect(sqlDosPragmas).toContain('PRAGMA foreign_keys = ON');
    expect(sqlDosPragmas).toContain('PRAGMA secure_delete = ON');
    expect(falso.execAsync.mock.invocationCallOrder[0]).toBeLessThan(
      falso.getFirstAsync.mock.invocationCallOrder[0],
    );
  });

  it('falha com ErroBanco de abertura quando o expo-sqlite não abre', async () => {
    mockAbrir.mockRejectedValue(new Error('detalhe interno'));

    await expect(abrirBanco()).rejects.toMatchObject({ name: 'ErroBanco', tipo: 'abertura' });
  });

  it('falha com o tipo em-uso, sem tentar de novo, quando outro processo segura o arquivo', async () => {
    mockAbrir.mockRejectedValue(
      new Error('Access Handles cannot be created if there is another open Access Handle'),
    );

    await expect(abrirBanco()).rejects.toMatchObject({ name: 'ErroBanco', tipo: 'em-uso' });
    expect(mockAbrir).toHaveBeenCalledTimes(1);
  });

  it('fecha o banco e falha quando a inicialização dá erro', async () => {
    const falso = criarBancoFalso();
    falso.execAsync.mockRejectedValue(new Error('disco cheio'));
    mockAbrir.mockResolvedValue(falso);

    await expect(abrirBanco()).rejects.toMatchObject({ name: 'ErroBanco', tipo: 'abertura' });
    expect(falso.closeAsync).toHaveBeenCalledTimes(1);
  });

  it('fecha o banco e preserva o tipo do erro quando o esquema é de versão mais nova', async () => {
    const falso = criarBancoFalso(99);
    mockAbrir.mockResolvedValue(falso);

    await expect(abrirBanco()).rejects.toMatchObject({ tipo: 'migracao' });
    expect(falso.closeAsync).toHaveBeenCalledTimes(1);
  });

  it('não repassa a mensagem técnica do erro original', async () => {
    mockAbrir.mockRejectedValue(new Error('caminho /dados/secreto'));

    const erro = await abrirBanco().catch((falha: unknown) => falha);

    expect((erro as Error).message).not.toContain('secreto');
  });
});
