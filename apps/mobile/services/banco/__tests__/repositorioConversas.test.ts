import type { Troca } from '@/types';

import { criarFilaGravacoes } from '../fila';
import { inicializarBanco } from '../migracoes';
import { criarRepositorioConversas } from '../repositorioConversas';
import { criarBancoEmMemoria } from '../testes/bancoEmMemoria';

let banco: ReturnType<typeof criarBancoEmMemoria>;
let repositorio: ReturnType<typeof criarRepositorioConversas>;

beforeEach(async () => {
  banco = criarBancoEmMemoria();
  await inicializarBanco(banco.db);
  repositorio = criarRepositorioConversas(banco.db, criarFilaGravacoes());
});

afterEach(() => {
  banco.fechar();
});

function criarTroca(sobrescrever: Partial<Troca> = {}): Troca {
  return {
    id: 't1',
    pergunta: 'Como solicito a retirada de um sofá?',
    resposta: '(Exemplo) resposta',
    servicosRelacionados: ['cata-treco'],
    fontes: [
      { titulo: 'Fonte A', url: '', dataConsulta: '2026-11-07' },
      { titulo: 'Fonte B', url: 'https://exemplo.invalid/b' },
    ],
    informacaoSuficiente: true,
    criadoEm: '2026-11-07T10:00:00.000Z',
    ...sobrescrever,
  };
}

async function contar(tabela: 'conversas' | 'trocas'): Promise<number> {
  const linha = await banco.db.getFirstAsync<{ total: number }>(
    `SELECT COUNT(*) AS total FROM ${tabela}`,
  );
  return linha?.total ?? -1;
}

describe('conversa criada só com a primeira troca', () => {
  it('não existe conversa antes da primeira troca', async () => {
    expect(await repositorio.listar()).toEqual([]);
    expect(await contar('conversas')).toBe(0);
  });

  it('a primeira troca cria a conversa com título cortado e ordem 1', async () => {
    const pergunta = 'a'.repeat(100);

    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ pergunta }));

    const [resumo] = await repositorio.listar();
    expect(resumo.id).toBe('c1');
    expect(resumo.municipioId).toBe('sao-paulo');
    expect(resumo.titulo).toHaveLength(60);
    expect(resumo.titulo.endsWith('…')).toBe(true);
    const ordem = await banco.db.getFirstAsync<{ ordem: number }>(
      'SELECT ordem FROM trocas WHERE id = ?',
      ['t1'],
    );
    expect(ordem?.ordem).toBe(1);
  });

  it('a segunda troca recebe ordem 2 e atualiza a data sem mudar o título', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca());
    await repositorio.salvarTroca(
      'c1',
      'sao-paulo',
      criarTroca({ id: 't2', pergunta: 'e se for domingo?', criadoEm: '2026-11-07T11:00:00.000Z' }),
    );

    const conversa = await repositorio.buscar('c1');

    expect(conversa?.trocas.map((troca) => troca.id)).toEqual(['t1', 't2']);
    expect(conversa?.titulo).toBe('Como solicito a retirada de um sofá?');
    expect(conversa?.atualizadoEm).toBe('2026-11-07T11:00:00.000Z');
    expect(await contar('conversas')).toBe(1);
  });

  it('uma falha ao gravar a troca desfaz também a criação da conversa', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' }));

    await expect(
      repositorio.salvarTroca('c2', 'sao-paulo', criarTroca({ id: 't1' })),
    ).rejects.toMatchObject({ name: 'ErroBanco', tipo: 'operacao' });

    expect(await repositorio.buscar('c2')).toBeNull();
    expect(await contar('conversas')).toBe(1);
    expect(await contar('trocas')).toBe(1);
  });

  it('rejeita gravar em uma conversa de outro município e não altera nada', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca());

    await expect(
      repositorio.salvarTroca('c1', 'exemplo', criarTroca({ id: 't2' })),
    ).rejects.toMatchObject({ tipo: 'conflito' });

    expect(await contar('trocas')).toBe(1);
  });

  it('gravações simultâneas ficam em ordem e não colidem, graças à fila', async () => {
    await Promise.all([
      repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' })),
      repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't2' })),
      repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't3' })),
    ]);

    const conversa = await repositorio.buscar('c1');
    expect(conversa?.trocas.map((troca) => troca.id)).toEqual(['t1', 't2', 't3']);
  });

  it('sem a fila, gravações simultâneas colidem (controle do teste)', async () => {
    const semFila = criarRepositorioConversas(banco.db, (tarefa) => tarefa());

    const resultados = await Promise.allSettled([
      semFila.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' })),
      semFila.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't2' })),
    ]);

    expect(resultados.some((resultado) => resultado.status === 'rejected')).toBe(true);
  });
});

describe('leitura', () => {
  it('devolve a conversa completa e os dados voltam iguais aos gravados', async () => {
    const comCanal = criarTroca({
      id: 't2',
      informacaoSuficiente: false,
      servicosRelacionados: [],
      fontes: [],
      canalOficialUrl: 'https://exemplo.invalid/canal',
      criadoEm: '2026-11-07T10:05:00.000Z',
    });
    const primeira = criarTroca();
    await repositorio.salvarTroca('c1', 'sao-paulo', primeira);
    await repositorio.salvarTroca('c1', 'sao-paulo', comCanal);

    const conversa = await repositorio.buscar('c1');

    expect(conversa?.trocas).toEqual([primeira, comCanal]);
    expect(conversa?.trocas[0]).not.toHaveProperty('canalOficialUrl');
    expect(conversa?.trocas[1].informacaoSuficiente).toBe(false);
  });

  it('devolve null para uma conversa que não existe', async () => {
    expect(await repositorio.buscar('nao-existe')).toBeNull();
  });

  it('lista da mais recente para a mais antiga e filtra por município', async () => {
    await repositorio.salvarTroca('antiga', 'sao-paulo', criarTroca({ id: 'a' }));
    await repositorio.salvarTroca(
      'outra',
      'exemplo',
      criarTroca({ id: 'b', criadoEm: '2026-11-07T12:00:00.000Z' }),
    );
    await repositorio.salvarTroca(
      'recente',
      'sao-paulo',
      criarTroca({ id: 'c', criadoEm: '2026-11-07T13:00:00.000Z' }),
    );

    const todas = await repositorio.listar();
    const deSaoPaulo = await repositorio.listar('sao-paulo');

    expect(todas.map((resumo) => resumo.id)).toEqual(['recente', 'outra', 'antiga']);
    expect(deSaoPaulo.map((resumo) => resumo.id)).toEqual(['recente', 'antiga']);
  });

  it('falha com dados inválidos quando o JSON gravado está corrompido', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca());
    await banco.db.runAsync("UPDATE trocas SET fontes_json = 'isso não é json'");

    await expect(repositorio.buscar('c1')).rejects.toMatchObject({ tipo: 'dados-invalidos' });
  });

  it('falha com dados inválidos quando o JSON tem o formato errado', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca());
    await banco.db.runAsync("UPDATE trocas SET fontes_json = '[{\"titulo\": 1}]'");

    await expect(repositorio.buscar('c1')).rejects.toMatchObject({ tipo: 'dados-invalidos' });
  });
});

describe('apagar', () => {
  it('apagar uma conversa remove as trocas dela e preserva as das outras', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' }));
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't2' }));
    await repositorio.salvarTroca('c2', 'sao-paulo', criarTroca({ id: 't3' }));

    await repositorio.apagar('c1');

    expect(await repositorio.buscar('c1')).toBeNull();
    expect(await contar('trocas')).toBe(1);
    expect((await repositorio.listar()).map((resumo) => resumo.id)).toEqual(['c2']);
  });

  it('apagar tudo remove conversas e trocas e preserva as preferências', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' }));
    await repositorio.salvarTroca('c2', 'exemplo', criarTroca({ id: 't2' }));
    await banco.db.runAsync('INSERT INTO preferencias (chave, valor) VALUES (?, ?)', [
      'nivel_texto',
      '2',
    ]);

    await repositorio.apagarTudo();

    expect(await contar('conversas')).toBe(0);
    expect(await contar('trocas')).toBe(0);
    const preferencia = await banco.db.getFirstAsync<{ valor: string }>(
      'SELECT valor FROM preferencias WHERE chave = ?',
      ['nivel_texto'],
    );
    expect(preferencia?.valor).toBe('2');
  });

  it('apagar uma conversa que não existe não falha', async () => {
    await expect(repositorio.apagar('nao-existe')).resolves.toBeUndefined();
  });
});

describe('privacidade dos erros', () => {
  it('a mensagem do erro não contém o texto da pergunta', async () => {
    await repositorio.salvarTroca('c1', 'sao-paulo', criarTroca({ id: 't1' }));

    const erro = await repositorio
      .salvarTroca('c2', 'sao-paulo', criarTroca({ id: 't1', pergunta: 'dado muito sensível' }))
      .catch((falha: unknown) => falha);

    expect(erro).toBeInstanceOf(Error);
    expect((erro as Error).message).not.toContain('sensível');
  });
});
