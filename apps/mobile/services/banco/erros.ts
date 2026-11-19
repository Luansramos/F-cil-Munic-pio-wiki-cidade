export type TipoErroBanco = 'abertura' | 'em-uso' | 'migracao' | 'conflito' | 'dados-invalidos' | 'operacao';

export class ErroBanco extends Error {
  readonly tipo: TipoErroBanco;

  constructor(tipo: TipoErroBanco, mensagem: string) {
    super(mensagem);
    this.name = 'ErroBanco';
    this.tipo = tipo;
  }
}

export async function protegerOperacao<T>(tarefa: () => Promise<T>): Promise<T> {
  try {
    return await tarefa();
  } catch (erro) {
    if (erro instanceof ErroBanco) {
      throw erro;
    }
    throw new ErroBanco('operacao', 'Não foi possível acessar os dados salvos neste celular.');
  }
}
