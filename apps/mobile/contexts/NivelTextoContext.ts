import { createContext, useContext } from 'react';

import { NIVEL_TEXTO_PADRAO } from '@/theme';
import type { NivelTexto } from '@/theme';

export const NivelTextoContext = createContext<NivelTexto>(NIVEL_TEXTO_PADRAO);

export function useNivelTexto(): NivelTexto {
  return useContext(NivelTextoContext);
}
