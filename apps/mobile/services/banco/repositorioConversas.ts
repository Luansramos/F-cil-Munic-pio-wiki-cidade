import type { Conversa, ResumoConversa, Troca } from '@/types';
import { ehListaDeFontes, ehListaDeStrings } from '@/types/guardas';
import { tituloDaConversa } from '@/utils/titulo';

import { ErroBanco, protegerOperacao } from './erros';
import { criarFilaGravacoes } from './fila';
import type { FilaGravacoes } from './fila';
import type { BancoSqlite } from './tipos';

type LinhaResumo = {
  id: string;
  municipio_id: string;
  titulo: string;
  atualizado_em: string;
};

type LinhaTroca = {
  id: string;
  pergunta: string;
  resposta: string;
  fontes_json: string;
  servicos_relacionados_json: string;
  informacao_suficiente: number;
  canal_oficial_url: string | null;
  criado_em: string;
};

function lerJson(texto: string): unknown {
  try {
    return JSON.parse(texto);
  } catch {
    throw new ErroBanco('dados-invalidos', 'Os dados salvos desta conversa estão inválidos.');
  }
}

function paraTroca(linha: LinhaTroca): Troca {
  const fontes = lerJson(linha.fontes_json);
  const servicos = lerJson(linha.servicos_relacionados_json);
  if (!ehListaDeFontes(fontes) || !ehListaDeStrings(servicos)) {
    throw new ErroBanco('dados-invalidos', 'Os dados salvos desta conversa estão inválidos.');
  }

  return {
    id: linha.id,
    pergunta: linha.pergunta,
    resposta: linha.resposta,
    fontes,
    servicosRelacionados: servicos,
    informacaoSuficiente: linha.informacao_suficiente === 1,
    ...(linha.canal_oficial_url === null ? {} : { canalOficialUrl: linha.canal_oficial_url }),
    criadoEm: linha.criado_em,
  };
}

function paraResumo(linha: LinhaResumo): ResumoConversa {
  return {
    id: linha.id,
    municipioId: linha.municipio_id,
    titulo: linha.titulo,
    atualizadoEm: linha.atualizado_em,
  };
}

export function criarRepositorioConversas(
  db: BancoSqlite,
  enfileirar: FilaGravacoes = criarFilaGravacoes(),
) {
  return {
    salvarTroca(conversaId: string, municipioId: string, troca: Troca): Promise<void> {
      return enfileirar(() =>
        protegerOperacao(() =>
          db.withTransactionAsync(async () => {
            const existente = await db.getFirstAsync<{ municipio_id: string }>(
              'SELECT municipio_id FROM conversas WHERE id = ?',
              [conversaId],
            );

            if (existente === null) {
              await db.runAsync(
                'INSERT INTO conversas (id, municipio_id, titulo, criado_em, atualizado_em) VALUES (?, ?, ?, ?, ?)',
                [conversaId, municipioId, tituloDaConversa(troca.pergunta), troca.criadoEm, troca.criadoEm],
              );
            } else if (existente.municipio_id !== municipioId) {
              throw new ErroBanco('conflito', 'Esta conversa pertence a outro município.');
            }

            const proxima = await db.getFirstAsync<{ proxima: number }>(
              'SELECT COALESCE(MAX(ordem), 0) + 1 AS proxima FROM trocas WHERE conversa_id = ?',
              [conversaId],
            );

            await db.runAsync(
              `INSERT INTO trocas (
                id, conversa_id, ordem, pergunta, resposta, fontes_json,
                servicos_relacionados_json, informacao_suficiente, canal_oficial_url, criado_em
              ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
              [
                troca.id,
                conversaId,
                proxima?.proxima ?? 1,
                troca.pergunta,
                troca.resposta,
                JSON.stringify(troca.fontes),
                JSON.stringify(troca.servicosRelacionados),
                troca.informacaoSuficiente ? 1 : 0,
                troca.canalOficialUrl ?? null,
                troca.criadoEm,
              ],
            );

            await db.runAsync('UPDATE conversas SET atualizado_em = ? WHERE id = ?', [
              troca.criadoEm,
              conversaId,
            ]);
          }),
        ),
      );
    },

    listar(municipioId?: string): Promise<ResumoConversa[]> {
      return protegerOperacao(async () => {
        const base = 'SELECT id, municipio_id, titulo, atualizado_em FROM conversas';
        const ordem = 'ORDER BY atualizado_em DESC, id ASC';
        const linhas =
          municipioId === undefined
            ? await db.getAllAsync<LinhaResumo>(`${base} ${ordem}`)
            : await db.getAllAsync<LinhaResumo>(`${base} WHERE municipio_id = ? ${ordem}`, [
                municipioId,
              ]);
        return linhas.map(paraResumo);
      });
    },

    buscar(conversaId: string): Promise<Conversa | null> {
      return protegerOperacao(async () => {
        const linha = await db.getFirstAsync<LinhaResumo>(
          'SELECT id, municipio_id, titulo, atualizado_em FROM conversas WHERE id = ?',
          [conversaId],
        );
        if (linha === null) {
          return null;
        }

        const trocas = await db.getAllAsync<LinhaTroca>(
          `SELECT id, pergunta, resposta, fontes_json, servicos_relacionados_json,
                  informacao_suficiente, canal_oficial_url, criado_em
             FROM trocas WHERE conversa_id = ? ORDER BY ordem ASC`,
          [conversaId],
        );

        return { ...paraResumo(linha), trocas: trocas.map(paraTroca) };
      });
    },

    apagar(conversaId: string): Promise<void> {
      return enfileirar(() =>
        protegerOperacao(async () => {
          await db.runAsync('DELETE FROM conversas WHERE id = ?', [conversaId]);
        }),
      );
    },

    apagarTudo(): Promise<void> {
      return enfileirar(() =>
        protegerOperacao(async () => {
          await db.runAsync('DELETE FROM conversas');
        }),
      );
    },
  };
}

export type RepositorioConversas = ReturnType<typeof criarRepositorioConversas>;
