import type { SQLiteDatabase } from 'expo-sqlite';

export type BancoSqlite = Pick<
  SQLiteDatabase,
  'execAsync' | 'runAsync' | 'getAllAsync' | 'getFirstAsync' | 'withTransactionAsync'
>;
