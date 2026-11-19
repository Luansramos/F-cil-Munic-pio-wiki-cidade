import type { EnvioPergunta, Municipio, RespostaConversa, Servico } from '@/types';

export interface ApiClient {
  listarMunicipios(): Promise<Municipio[]>;
  listarServicos(municipioId: string): Promise<Servico[]>;
  enviarPergunta(municipioId: string, envio: EnvioPergunta): Promise<RespostaConversa>;
}
