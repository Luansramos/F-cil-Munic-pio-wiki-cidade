import { abrirBanco } from './abrirBanco';
import { criarFilaGravacoes } from './fila';
import { criarRepositorioConversas } from './repositorioConversas';
import type { RepositorioConversas } from './repositorioConversas';
import { criarRepositorioPreferencias } from './repositorioPreferencias';
import type { RepositorioPreferencias } from './repositorioPreferencias';

export type Repositorios = {
  conversas: RepositorioConversas;
  preferencias: RepositorioPreferencias;
};

let abertura: Promise<Repositorios> | null = null;

async function criarRepositorios(): Promise<Repositorios> {
  const db = await abrirBanco();
  const enfileirar = criarFilaGravacoes();
  return {
    conversas: criarRepositorioConversas(db, enfileirar),
    preferencias: criarRepositorioPreferencias(db, enfileirar),
  };
}

export function abrirRepositorios(): Promise<Repositorios> {
  if (abertura === null) {
    abertura = criarRepositorios().catch((erro: unknown) => {
      abertura = null;
      throw erro;
    });
  }
  return abertura;
}

export { ErroBanco } from './erros';
export type { TipoErroBanco } from './erros';
export type { RepositorioConversas } from './repositorioConversas';
export type { RepositorioPreferencias } from './repositorioPreferencias';
