import { useState } from 'react';

import { FolhaModal } from '@/components/FolhaModal';
import { ListRow } from '@/components/ListRow';
import type { Municipio } from '@/types';
import { formatarMunicipio } from '@/utils/municipio';

type SeletorMunicipioProps = {
  municipios: Municipio[];
  selecionado: Municipio | null;
  aoSelecionar: (municipioId: string) => void;
  rotulo?: string;
  destaque?: boolean;
  opcaoTodos?: {
    ativa: boolean;
    aoSelecionar: () => void;
  };
};

const ROTULO_TODOS = 'Todos os municípios';

export function SeletorMunicipio({
  municipios,
  selecionado,
  aoSelecionar,
  rotulo,
  destaque = true,
  opcaoTodos,
}: SeletorMunicipioProps) {
  const [aberto, setAberto] = useState(false);
  const todosAtivo = opcaoTodos?.ativa === true;
  const nomeAtual = todosAtivo
    ? ROTULO_TODOS
    : selecionado
      ? formatarMunicipio(selecionado)
      : 'Escolher município';

  function escolher(municipioId: string) {
    aoSelecionar(municipioId);
    setAberto(false);
  }

  function escolherTodos() {
    opcaoTodos?.aoSelecionar();
    setAberto(false);
  }

  return (
    <>
      <ListRow
        icone="local"
        sobretitulo={rotulo}
        titulo={nomeAtual}
        destaque={destaque}
        aoPressionar={() => setAberto(true)}
        accessibilityLabel={`${rotulo ? `${rotulo}: ` : ''}${nomeAtual}. Toque para alterar`}
        accessibilityHint="Abre a lista de municípios"
      />
      <FolhaModal visivel={aberto} titulo="Escolha o município" aoFechar={() => setAberto(false)}>
        {opcaoTodos ? (
          <ListRow
            icone="local"
            titulo={ROTULO_TODOS}
            tom={todosAtivo ? 'tint' : 'surface'}
            fim={todosAtivo ? 'check' : 'nenhum'}
            selecionado={todosAtivo}
            aoPressionar={escolherTodos}
          />
        ) : null}
        {municipios.map((municipio) => {
          const ativo = !todosAtivo && municipio.id === selecionado?.id;
          return (
            <ListRow
              key={municipio.id}
              icone="local"
              titulo={formatarMunicipio(municipio)}
              tom={ativo ? 'tint' : 'surface'}
              fim={ativo ? 'check' : 'nenhum'}
              selecionado={ativo}
              aoPressionar={() => escolher(municipio.id)}
            />
          );
        })}
      </FolhaModal>
    </>
  );
}
