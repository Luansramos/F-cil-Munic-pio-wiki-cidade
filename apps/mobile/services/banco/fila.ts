export type FilaGravacoes = <T>(tarefa: () => Promise<T>) => Promise<T>;

export function criarFilaGravacoes(): FilaGravacoes {
  let ultima: Promise<unknown> = Promise.resolve();

  return <T>(tarefa: () => Promise<T>): Promise<T> => {
    const execucao = ultima.then(() => tarefa());
    ultima = execucao.catch(() => undefined);
    return execucao;
  };
}
