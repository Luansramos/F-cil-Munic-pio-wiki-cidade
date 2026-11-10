import { useCallback, useState } from 'react';

import { useRepositorios } from '@/contexts/BancoContext';

export function useApagarHistorico() {
  const { conversas } = useRepositorios();
  const [apagando, setApagando] = useState(false);
  const [concluido, setConcluido] = useState(false);
  const [erro, setErro] = useState<unknown>(null);

  const apagar = useCallback(async (): Promise<boolean> => {
    setApagando(true);
    setErro(null);
    setConcluido(false);
    try {
      await conversas.apagarTudo();
      setConcluido(true);
      return true;
    } catch (falha) {
      setErro(falha);
      return false;
    } finally {
      setApagando(false);
    }
  }, [conversas]);

  const limpar = useCallback(() => {
    setErro(null);
    setConcluido(false);
  }, []);

  return { apagando, concluido, erro, apagar, limpar };
}
