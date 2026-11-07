import { openDatabaseAsync } from 'expo-sqlite';
import type { SQLiteDatabase } from 'expo-sqlite';

import { ErroBanco } from './erros';
import { inicializarBanco } from './migracoes';

export const NOME_DO_BANCO = 'facil-municipio.db';

export async function abrirBanco(): Promise<SQLiteDatabase> {
  let db: SQLiteDatabase;
  try {
    db = await openDatabaseAsync(NOME_DO_BANCO);
  } catch {
    throw new ErroBanco('abertura', 'Não foi possível abrir os dados salvos neste celular.');
  }

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
