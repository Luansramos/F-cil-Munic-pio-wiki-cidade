import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

import { useRepositorios } from '@/contexts/BancoContext';
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
  const { preferencias } = useRepositorios();
  const { estado, recarregar } = useMunicipios();
  const [escolhidoId, setEscolhidoId] = useState<string | null>(null);

  useEffect(() => {
    let ativo = true;
    preferencias
      .lerMunicipioId()
      .then((salvo) => {
        if (ativo && salvo !== null) {
          setEscolhidoId((atual) => atual ?? salvo);
        }
      })
      .catch(() => undefined);
    return () => {
      ativo = false;
    };
  }, [preferencias]);

  const selecionar = useCallback(
    (municipioId: string) => {
      setEscolhidoId(municipioId);
      preferencias.salvarMunicipioId(municipioId).catch(() => undefined);
    },
    [preferencias],
  );

  const valor = useMemo<MunicipioContextValor>(() => {
    const lista = estado.status === 'pronto' ? estado.dados : [];
    const municipioAtual = lista.find((item) => item.id === escolhidoId) ?? lista[0] ?? null;
    return { estado, municipioAtual, selecionar, recarregar };
  }, [estado, escolhidoId, selecionar, recarregar]);

  return <MunicipioContext.Provider value={valor}>{children}</MunicipioContext.Provider>;
}

export function useMunicipioAtual(): MunicipioContextValor {
  const valor = useContext(MunicipioContext);
  if (valor === null) {
    throw new Error('useMunicipioAtual precisa estar dentro de MunicipioProvider.');
  }
  return valor;
}
