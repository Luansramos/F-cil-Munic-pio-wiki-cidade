import { ErroBanco } from './erros';
import type { BancoSqlite } from './tipos';

type Migracao = {
  versao: number;
  sql: string;
};

const MIGRACOES: Migracao[] = [
  {
    versao: 1,
    sql: `
      CREATE TABLE conversas (
        id TEXT PRIMARY KEY NOT NULL,
        municipio_id TEXT NOT NULL,
        titulo TEXT NOT NULL,
        criado_em TEXT NOT NULL,
        atualizado_em TEXT NOT NULL
      );
      CREATE INDEX idx_conversas_municipio_atualizado
        ON conversas (municipio_id, atualizado_em DESC);
      CREATE TABLE trocas (
        id TEXT PRIMARY KEY NOT NULL,
        conversa_id TEXT NOT NULL REFERENCES conversas (id) ON DELETE CASCADE,
        ordem INTEGER NOT NULL,
        pergunta TEXT NOT NULL,
        resposta TEXT NOT NULL,
        fontes_json TEXT NOT NULL,
        servicos_relacionados_json TEXT NOT NULL,
        informacao_suficiente INTEGER NOT NULL CHECK (informacao_suficiente IN (0, 1)),
        canal_oficial_url TEXT,
        criado_em TEXT NOT NULL,
        UNIQUE (conversa_id, ordem)
      );
      CREATE TABLE preferencias (
        chave TEXT PRIMARY KEY NOT NULL,
        valor TEXT NOT NULL
      );
    `,
  },
];

export const VERSAO_ATUAL_DO_ESQUEMA = MIGRACOES[MIGRACOES.length - 1].versao;

export async function migrar(db: BancoSqlite): Promise<void> {
  const linha = await db.getFirstAsync<{ user_version: number }>('PRAGMA user_version');
  if (linha === null) {
    throw new ErroBanco('migracao', 'Não foi possível ler a versão dos dados salvos.');
  }
  if (linha.user_version > VERSAO_ATUAL_DO_ESQUEMA) {
    throw new ErroBanco('migracao', 'Os dados salvos são de uma versão mais nova do aplicativo.');
  }

  const pendentes = MIGRACOES.filter((migracao) => migracao.versao > linha.user_version);
  if (pendentes.length === 0) {
    return;
  }

  await db.withTransactionAsync(async () => {
    for (const migracao of pendentes) {
      await db.execAsync(migracao.sql);
    }
    await db.execAsync(`PRAGMA user_version = ${VERSAO_ATUAL_DO_ESQUEMA}`);
  });
}

export async function inicializarBanco(db: BancoSqlite): Promise<void> {
  await db.execAsync('PRAGMA foreign_keys = ON; PRAGMA secure_delete = ON;');
  await migrar(db);
}
