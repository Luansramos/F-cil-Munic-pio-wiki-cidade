import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

import { Carregando } from '@/components/Carregando';
import { ContainerTela } from '@/components/ContainerTela';
import { NivelTextoContext } from '@/contexts/NivelTextoContext';
import { useRepositorios } from '@/contexts/BancoContext';
import { NIVEIS_TEXTO, NIVEL_TEXTO_PADRAO } from '@/theme';
import type { NivelTexto } from '@/theme';

type PreferenciasValor = {
  nivelTexto: NivelTexto;
  quantidadeDeNiveis: number;
  podeAumentar: boolean;
  podeDiminuir: boolean;
  aumentarTexto: () => void;
  diminuirTexto: () => void;
};

const PreferenciasContext = createContext<PreferenciasValor | null>(null);

const MAIOR_NIVEL = NIVEIS_TEXTO[NIVEIS_TEXTO.length - 1];
const MENOR_NIVEL = NIVEIS_TEXTO[0];

export function PreferenciasProvider({ children }: { children: ReactNode }) {
  const { preferencias } = useRepositorios();
  const [nivelTexto, setNivelTexto] = useState<NivelTexto>(NIVEL_TEXTO_PADRAO);
  const [carregado, setCarregado] = useState(false);

  useEffect(() => {
    let ativo = true;
    preferencias
      .lerNivelTexto()
      .then((salvo) => {
        if (ativo) {
          setNivelTexto(salvo);
        }
      })
      .catch(() => undefined)
      .finally(() => {
        if (ativo) {
          setCarregado(true);
        }
      });
    return () => {
      ativo = false;
    };
  }, [preferencias]);

  const definir = useCallback(
    (nivel: NivelTexto) => {
      setNivelTexto(nivel);
      preferencias.salvarNivelTexto(nivel).catch(() => undefined);
    },
    [preferencias],
  );

  const valor = useMemo<PreferenciasValor>(
    () => ({
      nivelTexto,
      quantidadeDeNiveis: NIVEIS_TEXTO.length,
      podeAumentar: nivelTexto < MAIOR_NIVEL,
      podeDiminuir: nivelTexto > MENOR_NIVEL,
      aumentarTexto: () => {
        if (nivelTexto < MAIOR_NIVEL) {
          definir((nivelTexto + 1) as NivelTexto);
        }
      },
      diminuirTexto: () => {
        if (nivelTexto > MENOR_NIVEL) {
          definir((nivelTexto - 1) as NivelTexto);
        }
      },
    }),
    [nivelTexto, definir],
  );

  if (!carregado) {
    return (
      <ContainerTela>
        <Carregando rotulo="Abrindo seus dados" />
      </ContainerTela>
    );
  }

  return (
    <PreferenciasContext.Provider value={valor}>
      <NivelTextoContext.Provider value={nivelTexto}>{children}</NivelTextoContext.Provider>
    </PreferenciasContext.Provider>
  );
}

export function usePreferencias(): PreferenciasValor {
  const valor = useContext(PreferenciasContext);
  if (valor === null) {
    throw new Error('usePreferencias precisa estar dentro de PreferenciasProvider.');
  }
  return valor;
}
