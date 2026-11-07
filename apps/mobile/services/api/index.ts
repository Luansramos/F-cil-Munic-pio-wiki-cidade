import { config } from '@/config';

import type { ApiClient } from './ApiClient';
import { criarApiMock } from './apiMock';
import { criarApiReal } from './apiReal';

export const api: ApiClient = config.usarMock
  ? criarApiMock({ latenciaMs: config.mockLatenciaMs })
  : criarApiReal(config.apiUrl);

export type { ApiClient } from './ApiClient';
export { ErroApi } from './erros';
export type { TipoErroApi } from './erros';
