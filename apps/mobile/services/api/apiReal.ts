import type { EnvioPergunta, Municipio, RespostaConversa, Servico } from '@/types';

import type { ApiClient } from './ApiClient';
import { ErroApi } from './erros';

const TEMPO_LIMITE_MS = 15000;

function ehObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null;
}

function ehListaDeStrings(valor: unknown): valor is string[] {
  return Array.isArray(valor) && valor.every((item) => typeof item === 'string');
}

function ehMunicipio(valor: unknown): valor is Municipio {
  return (
    ehObjeto(valor) &&
    typeof valor.id === 'string' &&
    typeof valor.nome === 'string' &&
    typeof valor.uf === 'string'
  );
}

function ehServico(valor: unknown): valor is Servico {
  return ehObjeto(valor) && typeof valor.id === 'string' && typeof valor.nome === 'string';
}

function ehFonte(valor: unknown): boolean {
  return (
    ehObjeto(valor) &&
    typeof valor.titulo === 'string' &&
    typeof valor.url === 'string' &&
    (valor.dataConsulta === undefined || typeof valor.dataConsulta === 'string')
  );
}

function ehRespostaConversa(valor: unknown): valor is RespostaConversa {
  return (
    ehObjeto(valor) &&
    typeof valor.resposta === 'string' &&
    ehListaDeStrings(valor.servicosRelacionados) &&
    Array.isArray(valor.fontes) &&
    valor.fontes.every(ehFonte) &&
    typeof valor.informacaoSuficiente === 'boolean' &&
    (valor.canalOficialUrl === undefined || typeof valor.canalOficialUrl === 'string')
  );
}

function formatoInesperado(): ErroApi {
  return new ErroApi('indisponivel', 'A API respondeu em um formato inesperado.');
}

export function criarApiReal(apiUrl: string): ApiClient {
  const base = apiUrl.replace(/\/+$/, '');

  async function requisitar(caminho: string, init?: RequestInit): Promise<unknown> {
    if (base === '') {
      throw new ErroApi('configuracao', 'A URL da API não foi configurada.');
    }

    const controlador = new AbortController();
    const temporizador = setTimeout(() => controlador.abort(), TEMPO_LIMITE_MS);

    let resposta: Response;
    try {
      resposta = await fetch(`${base}${caminho}`, {
        ...init,
        headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
        signal: controlador.signal,
      });
    } catch {
      throw new ErroApi('sem-conexao', 'Não foi possível falar com a API.');
    } finally {
      clearTimeout(temporizador);
    }

    if (!resposta.ok) {
      throw new ErroApi('indisponivel', `A API respondeu com o status ${resposta.status}.`);
    }

    try {
      return await resposta.json();
    } catch {
      throw formatoInesperado();
    }
  }

  return {
    async listarMunicipios(): Promise<Municipio[]> {
      const dados = await requisitar('/municipios');
      if (!Array.isArray(dados) || !dados.every(ehMunicipio)) {
        throw formatoInesperado();
      }
      return dados;
    },

    async listarServicos(municipioId: string): Promise<Servico[]> {
      const dados = await requisitar(`/municipios/${encodeURIComponent(municipioId)}/servicos`);
      if (!Array.isArray(dados) || !dados.every(ehServico)) {
        throw formatoInesperado();
      }
      return dados;
    },

    async enviarPergunta(municipioId: string, envio: EnvioPergunta): Promise<RespostaConversa> {
      const dados = await requisitar(`/municipios/${encodeURIComponent(municipioId)}/conversas`, {
        method: 'POST',
        body: JSON.stringify(envio),
      });
      if (!ehRespostaConversa(dados)) {
        throw formatoInesperado();
      }
      return dados;
    },
  };
}
