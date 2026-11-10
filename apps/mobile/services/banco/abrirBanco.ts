import { openDatabaseAsync } from 'expo-sqlite';
import type { SQLiteDatabase } from 'expo-sqlite';

import { ErroBanco } from './erros';
import { inicializarBanco } from './migracoes';

export const NOME_DO_BANCO = 'facil-municipio.db';

function arquivoEmUso(erro: unknown): boolean {
  return /access handle/i.test(String(erro));
}

async function abrirNoExpoSqlite(): Promise<SQLiteDatabase> {
  try {
    return await openDatabaseAsync(NOME_DO_BANCO);
  } catch (erro) {
    if (arquivoEmUso(erro)) {
      throw new ErroBanco('em-uso', 'Os dados salvos estão em uso por outra aba.');
    }
    throw new ErroBanco('abertura', 'Não foi possível abrir os dados salvos neste celular.');
  }
}

export async function abrirBanco(): Promise<SQLiteDatabase> {
  const db = await abrirNoExpoSqlite();

  try {
    await inicializarBanco(db);
    return db;
  } catch (erro) {
    await db.closeAsync().catch(() => undefined);
    if (erro instanceof ErroBanco) {
      throw erro;
    }
    throw new ErroBanco('abertura', 'Não foi possível abrir os dados salvos neste celular.');
  }
}
