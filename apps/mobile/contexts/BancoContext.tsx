import { createContext, useContext } from 'react';
import type { ReactNode } from 'react';

import { Carregando } from '@/components/Carregando';
import { ContainerTela } from '@/components/ContainerTela';
import { Notice } from '@/components/Notice';
import { useCarregamento } from '@/hooks/useCarregamento';
import { abrirRepositorios } from '@/services/banco';
import type { Repositorios } from '@/services/banco';
import { mensagemDeErro } from '@/utils/erros';

const BancoContext = createContext<Repositorios | null>(null);

export function BancoProvider({ children }: { children: ReactNode }) {
  const { estado, recarregar } = useCarregamento(abrirRepositorios);

  if (estado.status === 'carregando') {
    return (
      <ContainerTela>
        <Carregando rotulo="Abrindo seus dados" />
      </ContainerTela>
    );
  }

  if (estado.status === 'erro') {
    return (
      <ContainerTela>
        <Notice
          tom="warning"
          titulo="Não foi possível abrir seus dados"
          mensagem={mensagemDeErro(estado.erro)}
          acao={{ rotulo: 'Tentar novamente', aoPressionar: recarregar }}
        />
      </ContainerTela>
    );
  }

  return <BancoContext.Provider value={estado.dados}>{children}</BancoContext.Provider>;
}

export function useRepositorios(): Repositorios {
  const repositorios = useContext(BancoContext);
  if (repositorios === null) {
    throw new Error('useRepositorios precisa estar dentro de BancoProvider.');
  }
  return repositorios;
}
