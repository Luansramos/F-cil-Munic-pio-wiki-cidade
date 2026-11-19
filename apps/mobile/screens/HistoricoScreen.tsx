import { useRouter } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Carregando } from '@/components/Carregando';
import { ConfirmarApagar } from '@/components/ConfirmarApagar';
import { ContainerTela } from '@/components/ContainerTela';
import { ItemConversa } from '@/components/ItemConversa';
import { Notice } from '@/components/Notice';
import { SeletorMunicipio } from '@/components/SeletorMunicipio';
import { useMunicipioAtual } from '@/contexts/MunicipioContext';
import { useConexao } from '@/hooks/useConexao';
import { useHistorico } from '@/hooks/useHistorico';
import { spacing } from '@/theme';
import type { ResumoConversa } from '@/types';
import { formatarDataHistorico } from '@/utils/datas';
import { mensagemDeErro } from '@/utils/erros';
import { formatarMunicipio } from '@/utils/municipio';

export function HistoricoScreen() {
  const router = useRouter();
  const { estado: estadoMunicipios, municipioAtual } = useMunicipioAtual();
  const conectado = useConexao();

  const [mostrarTodos, setMostrarTodos] = useState(false);
  const [filtroEscolhido, setFiltroEscolhido] = useState<string | null>(null);
  const [aApagar, setAApagar] = useState<ResumoConversa | null>(null);
  const [apagando, setApagando] = useState(false);
  const [erroAoApagar, setErroAoApagar] = useState<unknown>(null);

  const municipios = estadoMunicipios.status === 'pronto' ? estadoMunicipios.dados : [];
  const filtroId = mostrarTodos ? null : (filtroEscolhido ?? municipioAtual?.id ?? null);
  const municipioDoFiltro = municipios.find((item) => item.id === filtroId) ?? municipioAtual;

  const historico = useHistorico(filtroId);

  function nomeDoMunicipio(municipioId: string): string {
    const encontrado = municipios.find((item) => item.id === municipioId);
    return encontrado ? formatarMunicipio(encontrado) : municipioId;
  }

  function abrir(conversa: ResumoConversa) {
    router.push({ pathname: '/conversa/[id]', params: { id: conversa.id } });
  }

  async function confirmarApagar() {
    if (aApagar === null) {
      return;
    }
    setApagando(true);
    setErroAoApagar(null);
    try {
      await historico.apagar(aApagar.id);
      setAApagar(null);
    } catch (erro) {
      setErroAoApagar(erro);
      setAApagar(null);
    } finally {
      setApagando(false);
    }
  }

  const lista = historico.estado.status === 'pronto' ? historico.estado.dados : [];

  return (
    <ContainerTela rolavel>
      <View style={styles.pagina}>
        <View style={styles.apresentacao}>
          <AppText variant="title" accessibilityRole="header">
            Histórico
          </AppText>
          <AppText variant="body" color="textMuted">
            Suas conversas salvas neste celular.
          </AppText>
        </View>

        {municipios.length > 0 ? (
          <SeletorMunicipio
            rotulo="Município"
            destaque
            municipios={municipios}
            selecionado={municipioDoFiltro}
            aoSelecionar={(municipioId) => {
              setMostrarTodos(false);
              setFiltroEscolhido(municipioId);
            }}
            opcaoTodos={{
              ativa: mostrarTodos,
              aoSelecionar: () => setMostrarTodos(true),
            }}
          />
        ) : null}

        {historico.estado.status === 'carregando' ? <Carregando rotulo="Carregando conversas" /> : null}

        {historico.estado.status === 'erro' ? (
          <Notice
            tom="warning"
            titulo="Não foi possível carregar o histórico"
            mensagem={mensagemDeErro(historico.estado.erro)}
            acao={{ rotulo: 'Tentar novamente', aoPressionar: historico.recarregar }}
          />
        ) : null}

        {erroAoApagar !== null ? (
          <Notice
            tom="warning"
            titulo="Não foi possível apagar a conversa"
            mensagem={mensagemDeErro(erroAoApagar)}
          />
        ) : null}

        {historico.estado.status === 'pronto' && lista.length === 0 ? (
          <Notice
            tom="tint"
            icone="conversa"
            mensagem={
              filtroId === null
                ? 'Você ainda não tem conversas salvas.'
                : 'Você ainda não tem conversas salvas neste município. Escolha "Todos os municípios" para ver as outras.'
            }
          />
        ) : null}

        {lista.map((conversa) => (
          <ItemConversa
            key={conversa.id}
            titulo={conversa.titulo}
            data={formatarDataHistorico(conversa.atualizadoEm)}
            municipio={filtroId === null ? nomeDoMunicipio(conversa.municipioId) : undefined}
            aoAbrir={() => abrir(conversa)}
            aoApagar={() => {
              setErroAoApagar(null);
              setAApagar(conversa);
            }}
          />
        ))}

        {conectado === false ? (
          <Notice
            tom="tint"
            icone="semInternet"
            titulo="Disponível sem internet"
            mensagem="Consulte as conversas já salvas."
          />
        ) : null}
      </View>

      <ConfirmarApagar
        visivel={aApagar !== null}
        titulo="Apagar conversa?"
        mensagem={`A conversa "${aApagar?.titulo ?? ''}" será apagada deste celular. Isso não pode ser desfeito.`}
        rotuloConfirmar="Apagar conversa"
        ocupado={apagando}
        aoConfirmar={confirmarApagar}
        aoCancelar={() => setAApagar(null)}
      />
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  pagina: {
    gap: spacing.lg,
  },
  apresentacao: {
    gap: spacing.xs + 2,
  },
});
