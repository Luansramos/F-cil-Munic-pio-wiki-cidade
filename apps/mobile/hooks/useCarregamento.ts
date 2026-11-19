import { useCallback, useEffect, useState } from 'react';

export type EstadoCarregamento<T> =
  | { status: 'carregando' }
  | { status: 'erro'; erro: unknown }
  | { status: 'pronto'; dados: T };

export function useCarregamento<T>(carregar: () => Promise<T>) {
  const [estado, setEstado] = useState<EstadoCarregamento<T>>({ status: 'carregando' });
  const [tentativa, setTentativa] = useState(0);

  useEffect(() => {
    let ativo = true;
    setEstado({ status: 'carregando' });

    carregar()
      .then((dados) => {
        if (ativo) {
          setEstado({ status: 'pronto', dados });
        }
      })
      .catch((erro: unknown) => {
        if (ativo) {
          setEstado({ status: 'erro', erro });
        }
      });

    return () => {
      ativo = false;
    };
  }, [carregar, tentativa]);

  const recarregar = useCallback(() => setTentativa((valor) => valor + 1), []);

  return { estado, recarregar };
}
