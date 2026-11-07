import { createContext, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

import { useMunicipios } from '@/hooks/useMunicipios';
import type { EstadoCarregamento } from '@/hooks/useCarregamento';
import type { Municipio } from '@/types';

type MunicipioContextValor = {
  estado: EstadoCarregamento<Municipio[]>;
  municipioAtual: Municipio | null;
  selecionar: (municipioId: string) => void;
  recarregar: () => void;
};

const MunicipioContext = createContext<MunicipioContextValor | null>(null);

export function MunicipioProvider({ children }: { children: ReactNode }) {
  const { estado, recarregar } = useMunicipios();
  const [escolhidoId, setEscolhidoId] = useState<string | null>(null);

  const valor = useMemo<MunicipioContextValor>(() => {
    const lista = estado.status === 'pronto' ? estado.dados : [];
    const municipioAtual = lista.find((item) => item.id === escolhidoId) ?? lista[0] ?? null;
    return { estado, municipioAtual, selecionar: setEscolhidoId, recarregar };
  }, [estado, escolhidoId, recarregar]);

  return <MunicipioContext.Provider value={valor}>{children}</MunicipioContext.Provider>;
}

export function useMunicipioAtual(): MunicipioContextValor {
  const valor = useContext(MunicipioContext);
  if (valor === null) {
    throw new Error('useMunicipioAtual precisa estar dentro de MunicipioProvider.');
  }
  return valor;
}
