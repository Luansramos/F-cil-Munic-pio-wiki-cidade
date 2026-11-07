import { api } from '@/services/api';

import { useCarregamento } from './useCarregamento';

const carregarMunicipios = () => api.listarMunicipios();

export function useMunicipios() {
  return useCarregamento(carregarMunicipios);
}
