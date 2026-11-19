import { ErroBanco } from '../erros';
import { VERSAO_ATUAL_DO_ESQUEMA, inicializarBanco, migrar } from '../migracoes';
import { criarBancoEmMemoria } from '../testes/bancoEmMemoria';

let banco: ReturnType<typeof criarBancoEmMemoria>;

beforeEach(() => {
  banco = criarBancoEmMemoria();
});

afterEach(() => {
  banco.fechar();
});

async function nomesDasTabelas(): Promise<string[]> {
  const linhas = await banco.db.getAllAsync<{ name: string }>(
    "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name",
  );
  return linhas.map((linha) => linha.name);
}

describe('inicializarBanco', () => {
  it('em banco novo cria as três tabelas e grava a versão do esquema', async () => {
    await inicializarBanco(banco.db);

    expect(await nomesDasTabelas()).toEqual(['conversas', 'preferencias', 'trocas']);
    const versao = await banco.db.getFirstAsync<{ user_version: number }>('PRAGMA user_version');
    expect(versao?.user_version).toBe(VERSAO_ATUAL_DO_ESQUEMA);
  });

  it('liga as chaves estrangeiras e o secure_delete', async () => {
    await inicializarBanco(banco.db);

    const chaves = await banco.db.getFirstAsync<{ foreign_keys: number }>('PRAGMA foreign_keys');
    const seguro = await banco.db.getFirstAsync<{ secure_delete: number }>('PRAGMA secure_delete');
    expect(chaves?.foreign_keys).toBe(1);
    expect(seguro?.secure_delete).toBe(1);
  });

  it('é idempotente: iniciar de novo mantém os dados já gravados', async () => {
    await inicializarBanco(banco.db);
    await banco.db.runAsync('INSERT INTO preferencias (chave, valor) VALUES (?, ?)', [
      'municipio_id',
      'sao-paulo',
    ]);

    await inicializarBanco(banco.db);

    const linha = await banco.db.getFirstAsync<{ valor: string }>(
      'SELECT valor FROM preferencias WHERE chave = ?',
      ['municipio_id'],
    );
    expect(linha?.valor).toBe('sao-paulo');
  });

  it('recusa um banco de versão mais nova e não altera nada', async () => {
    await banco.db.execAsync('PRAGMA user_version = 99');

    await expect(inicializarBanco(banco.db)).rejects.toMatchObject({
      name: 'ErroBanco',
      tipo: 'migracao',
    });
    expect(await nomesDasTabelas()).toEqual([]);
    const versao = await banco.db.getFirstAsync<{ user_version: number }>('PRAGMA user_version');
    expect(versao?.user_version).toBe(99);
  });

  it('desfaz a migração inteira quando ela falha no meio', async () => {
    await banco.db.execAsync('CREATE TABLE trocas (x TEXT)');

    await expect(migrar(banco.db)).rejects.toThrow();

    expect(await nomesDasTabelas()).toEqual(['trocas']);
    const versao = await banco.db.getFirstAsync<{ user_version: number }>('PRAGMA user_version');
    expect(versao?.user_version).toBe(0);
  });
});

describe('chaves estrangeiras', () => {
  const insercaoOrfa = [
    'INSERT INTO trocas (id, conversa_id, ordem, pergunta, resposta, fontes_json,',
    'servicos_relacionados_json, informacao_suficiente, criado_em)',
    "VALUES ('t1', 'inexistente', 1, 'p', 'r', '[]', '[]', 1, '2026-11-07T10:00:00.000Z')",
  ].join(' ');

  it('rejeitam uma troca de conversa que não existe depois de inicializarBanco', async () => {
    await inicializarBanco(banco.db);

    await expect(banco.db.execAsync(insercaoOrfa)).rejects.toThrow();
  });

  it('sem o PRAGMA a restrição não é aplicada (controle do teste)', async () => {
    await migrar(banco.db);

    await expect(banco.db.execAsync(insercaoOrfa)).resolves.toBeUndefined();
  });

  it('rejeitam ordem repetida na mesma conversa', async () => {
    await inicializarBanco(banco.db);
    await banco.db.runAsync(
      'INSERT INTO conversas (id, municipio_id, titulo, criado_em, atualizado_em) VALUES (?, ?, ?, ?, ?)',
      ['c1', 'm', 't', 'x', 'x'],
    );
    const inserir = (id: string) =>
      banco.db.runAsync(
        `INSERT INTO trocas (id, conversa_id, ordem, pergunta, resposta, fontes_json,
          servicos_relacionados_json, informacao_suficiente, criado_em)
         VALUES (?, 'c1', 1, 'p', 'r', '[]', '[]', 1, 'x')`,
        [id],
      );

    await inserir('t1');

    await expect(inserir('t2')).rejects.toThrow();
  });
});

describe('ErroBanco', () => {
  it('carrega o tipo e o nome', () => {
    const erro = new ErroBanco('conflito', 'x');

    expect(erro).toBeInstanceOf(Error);
    expect(erro.tipo).toBe('conflito');
    expect(erro.name).toBe('ErroBanco');
  });
});
