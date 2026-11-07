import type { EnvioPergunta, Municipio, RespostaConversa, Servico } from '@/types';

import type { ApiClient } from './ApiClient';
import { ErroApi } from './erros';
import { MUNICIPIOS_MOCK, buscarMunicipio, servicosDoMunicipio } from './mock/dados';
import { responderMock } from './mock/responder';

type OpcoesApiMock = {
  latenciaMs?: number;
};

function esperar(milissegundos: number): Promise<void> {
  if (milissegundos <= 0) {
    return Promise.resolve();
  }
  return new Promise((resolver) => setTimeout(resolver, milissegundos));
}

function exigirMunicipio(municipioId: string): Municipio {
  const municipio = buscarMunicipio(municipioId);
  if (!municipio) {
    throw new ErroApi('desconhecido', 'Município não encontrado.');
  }
  return municipio;
}

export function criarApiMock(opcoes: OpcoesApiMock = {}): ApiClient {
  const latenciaMs = opcoes.latenciaMs ?? 0;

  return {
    async listarMunicipios(): Promise<Municipio[]> {
      await esperar(latenciaMs);
      return MUNICIPIOS_MOCK.map((municipio) => ({ ...municipio }));
    },

    async listarServicos(municipioId: string): Promise<Servico[]> {
      await esperar(latenciaMs);
      exigirMunicipio(municipioId);
      return servicosDoMunicipio(municipioId).map(({ id, nome }) => ({ id, nome }));
    },

    async enviarPergunta(municipioId: string, envio: EnvioPergunta): Promise<RespostaConversa> {
      await esperar(latenciaMs);
      const municipio = exigirMunicipio(municipioId);
      if (envio.pergunta.trim() === '') {
        throw new ErroApi('desconhecido', 'A pergunta está vazia.');
      }
      return responderMock(municipio, envio);
    },
  };
}
