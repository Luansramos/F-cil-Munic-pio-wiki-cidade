import type { RespostaConversa } from './contrato';

export type Troca = RespostaConversa & {
  id: string;
  pergunta: string;
  criadoEm: string;
};

export type ResumoConversa = {
  id: string;
  municipioId: string;
  titulo: string;
  atualizadoEm: string;
};

export type Conversa = {
  id: string;
  municipioId: string;
  titulo: string;
  trocas: Troca[];
  atualizadoEm: string;
};
