import type { BancoSqlite } from '../tipos';

type Execucao = { changes: number | bigint; lastInsertRowid: number | bigint };

type Sentenca = {
  run(...parametros: unknown[]): Execucao;
  all(...parametros: unknown[]): unknown[];
  get(...parametros: unknown[]): unknown;
};

type BancoNode = {
  exec(sql: string): void;
  prepare(sql: string): Sentenca;
  close(): void;
};

type OpcoesBanco = {
  enableForeignKeyConstraints: boolean;
};

type ModuloSqlite = {
  DatabaseSync: new (caminho: string, opcoes: OpcoesBanco) => BancoNode;
};

function desembrulhar(valores: unknown[]): unknown[] {
  if (valores.length === 1 && Array.isArray(valores[0])) {
    return valores[0];
  }
  return valores;
}

export function criarBancoEmMemoria() {
  const { DatabaseSync } = jest.requireActual<ModuloSqlite>('node:sqlite');
  const banco = new DatabaseSync(':memory:', { enableForeignKeyConstraints: false });

  const db: BancoSqlite = {
    async execAsync(sql: string): Promise<void> {
      banco.exec(sql);
    },

    async runAsync(sql: string, ...valores: unknown[]) {
      const resultado = banco.prepare(sql).run(...desembrulhar(valores));
      return {
        lastInsertRowId: Number(resultado.lastInsertRowid),
        changes: Number(resultado.changes),
      };
    },

    async getAllAsync<T>(sql: string, ...valores: unknown[]): Promise<T[]> {
      return banco.prepare(sql).all(...desembrulhar(valores)) as T[];
    },

    async getFirstAsync<T>(sql: string, ...valores: unknown[]): Promise<T | null> {
      const linha = banco.prepare(sql).get(...desembrulhar(valores));
      return linha === undefined ? null : (linha as T);
    },

    async withTransactionAsync(tarefa: () => Promise<void>): Promise<void> {
      banco.exec('BEGIN');
      try {
        await tarefa();
        banco.exec('COMMIT');
      } catch (erro) {
        banco.exec('ROLLBACK');
        throw erro;
      }
    },
  };

  return { db, fechar: () => banco.close() };
}
