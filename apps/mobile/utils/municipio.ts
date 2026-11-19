import type { Municipio } from '@/types';

export function formatarMunicipio(municipio: Municipio): string {
  return `${municipio.nome} · ${municipio.uf}`;
}
