import { addNetworkStateListener, getNetworkStateAsync } from 'expo-network';
import { useEffect, useState } from 'react';

export function useConexao(): boolean | null {
  const [conectado, setConectado] = useState<boolean | null>(null);

  useEffect(() => {
    let ativo = true;

    getNetworkStateAsync()
      .then((estado) => {
        if (ativo) {
          setConectado(estado.isConnected ?? null);
        }
      })
      .catch(() => undefined);

    let assinatura: { remove: () => void } | null = null;
    try {
      assinatura = addNetworkStateListener((estado) => {
        if (ativo) {
          setConectado(estado.isConnected ?? null);
        }
      });
    } catch {
      assinatura = null;
    }

    return () => {
      ativo = false;
      assinatura?.remove();
    };
  }, []);

  return conectado;
}
