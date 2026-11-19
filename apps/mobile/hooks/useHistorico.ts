import { useFocusEffect } from 'expo-router';
import { useCallback, useRef, useState } from 'react';

import { useRepositorios } from '@/contexts/BancoContext';
import type { ResumoConversa } from '@/types';

import type { EstadoCarregamento } from './useCarregamento';

export function useHistorico(municipioId: string | null) {
  const { conversas } = useRepositorios();
  const [estado, setEstado] = useState<EstadoCarregamento<ResumoConversa[]>>({
    status: 'carregando',
  });
  const ultimaBusca = useRef(0);

  const recarregar = useCallback(async () => {
    ultimaBusca.current += 1;
    const busca = ultimaBusca.current;
    try {
      const lista = await conversas.listar(municipioId ?? undefined);
      if (busca === ultimaBusca.current) {
        setEstado({ status: 'pronto', dados: lista });
      }
    } catch (erro) {
      if (busca === ultimaBusca.current) {
        setEstado({ status: 'erro', erro });
      }
    }
  }, [conversas, municipioId]);

  useFocusEffect(
    useCallback(() => {
      recarregar();
    }, [recarregar]),
  );

  const apagar = useCallback(
    async (conversaId: string) => {
      await conversas.apagar(conversaId);
      await recarregar();
    },
    [conversas, recarregar],
  );

  return { estado, recarregar, apagar };
}
