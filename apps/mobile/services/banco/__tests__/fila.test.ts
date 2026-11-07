import { criarFilaGravacoes } from '../fila';

function esperar(milissegundos: number): Promise<void> {
  return new Promise((resolver) => setTimeout(resolver, milissegundos));
}

describe('criarFilaGravacoes', () => {
  it('executa as tarefas na ordem de envio, mesmo quando as primeiras demoram mais', async () => {
    const enfileirar = criarFilaGravacoes();
    const ordem: string[] = [];

    await Promise.all([
      enfileirar(async () => {
        await esperar(30);
        ordem.push('a');
      }),
      enfileirar(async () => {
        await esperar(5);
        ordem.push('b');
      }),
      enfileirar(async () => {
        ordem.push('c');
      }),
    ]);

    expect(ordem).toEqual(['a', 'b', 'c']);
  });

  it('não executa duas tarefas ao mesmo tempo', async () => {
    const enfileirar = criarFilaGravacoes();
    let ativas = 0;
    let maximo = 0;

    const tarefa = async () => {
      ativas += 1;
      maximo = Math.max(maximo, ativas);
      await esperar(5);
      ativas -= 1;
    };

    await Promise.all([enfileirar(tarefa), enfileirar(tarefa), enfileirar(tarefa)]);

    expect(maximo).toBe(1);
  });

  it('devolve o resultado de cada tarefa', async () => {
    const enfileirar = criarFilaGravacoes();

    const resultados = await Promise.all([enfileirar(async () => 1), enfileirar(async () => 2)]);

    expect(resultados).toEqual([1, 2]);
  });

  it('uma tarefa que falha rejeita só a si mesma e não bloqueia as seguintes', async () => {
    const enfileirar = criarFilaGravacoes();

    const falha = enfileirar(async () => {
      throw new Error('falhou');
    });
    const seguinte = enfileirar(async () => 'ok');

    await expect(falha).rejects.toThrow('falhou');
    await expect(seguinte).resolves.toBe('ok');
  });
});
