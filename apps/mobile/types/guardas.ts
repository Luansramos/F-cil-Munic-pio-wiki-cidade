import type { Fonte, Municipio, RespostaConversa, Servico } from './contrato';

export function ehObjeto(valor: unknown): valor is Record<string, unknown> {
  return typeof valor === 'object' && valor !== null;
}

export function ehListaDeStrings(valor: unknown): valor is string[] {
  return Array.isArray(valor) && valor.every((item) => typeof item === 'string');
}

export function ehFonte(valor: unknown): valor is Fonte {
  return (
    ehObjeto(valor) &&
    typeof valor.titulo === 'string' &&
    typeof valor.url === 'string' &&
    (valor.dataConsulta === undefined || typeof valor.dataConsulta === 'string')
  );
}

export function ehListaDeFontes(valor: unknown): valor is Fonte[] {
  return Array.isArray(valor) && valor.every(ehFonte);
}

export function ehMunicipio(valor: unknown): valor is Municipio {
  return (
    ehObjeto(valor) &&
    typeof valor.id === 'string' &&
    typeof valor.nome === 'string' &&
    typeof valor.uf === 'string'
  );
}

export function ehServico(valor: unknown): valor is Servico {
  return ehObjeto(valor) && typeof valor.id === 'string' && typeof valor.nome === 'string';
}

export function ehRespostaConversa(valor: unknown): valor is RespostaConversa {
  return (
    ehObjeto(valor) &&
    typeof valor.resposta === 'string' &&
    ehListaDeStrings(valor.servicosRelacionados) &&
    ehListaDeFontes(valor.fontes) &&
    typeof valor.informacaoSuficiente === 'boolean' &&
    (valor.canalOficialUrl === undefined || typeof valor.canalOficialUrl === 'string')
  );
}
