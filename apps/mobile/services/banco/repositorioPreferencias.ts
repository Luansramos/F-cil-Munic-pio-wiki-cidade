import { NIVEIS_TEXTO, NIVEL_TEXTO_PADRAO } from '@/theme';
import type { NivelTexto } from '@/theme';

import { protegerOperacao } from './erros';
import { criarFilaGravacoes } from './fila';
import type { FilaGravacoes } from './fila';
import type { BancoSqlite } from './tipos';

const CHAVE_MUNICIPIO = 'municipio_id';
const CHAVE_NIVEL_TEXTO = 'nivel_texto';

function ehNivelTexto(valor: number): valor is NivelTexto {
  return (NIVEIS_TEXTO as readonly number[]).includes(valor);
}

export function criarRepositorioPreferencias(
  db: BancoSqlite,
  enfileirar: FilaGravacoes = criarFilaGravacoes(),
) {
  async function ler(chave: string): Promise<string | null> {
    const linha = await db.getFirstAsync<{ valor: string }>(
      'SELECT valor FROM preferencias WHERE chave = ?',
      [chave],
    );
    return linha === null ? null : linha.valor;
  }

  function gravar(chave: string, valor: string): Promise<void> {
    return enfileirar(() =>
      protegerOperacao(async () => {
        await db.runAsync(
          'INSERT INTO preferencias (chave, valor) VALUES (?, ?) ON CONFLICT (chave) DO UPDATE SET valor = excluded.valor',
          [chave, valor],
        );
      }),
    );
  }

  return {
    lerMunicipioId(): Promise<string | null> {
      return protegerOperacao(() => ler(CHAVE_MUNICIPIO));
    },

    salvarMunicipioId(municipioId: string): Promise<void> {
      return gravar(CHAVE_MUNICIPIO, municipioId);
    },

    lerNivelTexto(): Promise<NivelTexto> {
      return protegerOperacao(async () => {
        const valor = await ler(CHAVE_NIVEL_TEXTO);
        const numero = valor === null ? Number.NaN : Number(valor);
        return ehNivelTexto(numero) ? numero : NIVEL_TEXTO_PADRAO;
      });
    },

    salvarNivelTexto(nivel: NivelTexto): Promise<void> {
      return gravar(CHAVE_NIVEL_TEXTO, String(nivel));
    },
  };
}

export type RepositorioPreferencias = ReturnType<typeof criarRepositorioPreferencias>;
