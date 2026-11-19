import { useCallback } from 'react';

import { api } from '@/services/api';
import type { Servico } from '@/types';

import { useCarregamento } from './useCarregamento';

export function useServicos(municipioId: string | null) {
  const carregar = useCallback(
    (): Promise<Servico[]> =>
      municipioId === null ? Promise.resolve([]) : api.listarServicos(municipioId),
    [municipioId],
  );

  return useCarregamento(carregar);
}
