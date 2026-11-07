import { useRouter } from 'expo-router';
import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Button } from '@/components/Button';
import { Carregando } from '@/components/Carregando';
import { ContainerTela } from '@/components/ContainerTela';
import { Icon } from '@/components/Icon';
import { ListRow } from '@/components/ListRow';
import { Notice } from '@/components/Notice';
import { SectionTitle } from '@/components/SectionTitle';
import { SeletorMunicipio } from '@/components/SeletorMunicipio';
import { useMunicipioAtual } from '@/contexts/MunicipioContext';
import { useServicos } from '@/hooks/useServicos';
import { spacing } from '@/theme';
import { mensagemDeErro } from '@/utils/erros';
import { gerarId } from '@/utils/id';
import { iconeDoServico, perguntaSugerida } from '@/utils/servicos';

export function InicioScreen() {
  const router = useRouter();
  const { estado, municipioAtual, selecionar, recarregar } = useMunicipioAtual();
  const servicos = useServicos(municipioAtual?.id ?? null);

  function abrirConversa(pergunta?: string) {
    if (!municipioAtual) {
      return;
    }
    router.push({
      pathname: '/conversa/[id]',
      params: {
        id: gerarId(),
        municipioId: municipioAtual.id,
        ...(pergunta ? { perguntaSugerida: pergunta } : {}),
      },
    });
  }

  return (
    <ContainerTela rolavel>
      <View style={styles.pagina}>
        <View style={styles.marca}>
          <Icon name="predio" size="lg" color="primary" />
          <AppText variant="heading">Fácil Município</AppText>
        </View>

        <View style={styles.apresentacao}>
          <AppText variant="title" accessibilityRole="header">
            Como podemos ajudar?
          </AppText>
          <AppText variant="body" color="textMuted">
            Encontre orientações sobre os serviços da sua cidade.
          </AppText>
        </View>

        <View style={styles.bloco}>
          {estado.status === 'carregando' ? <Carregando rotulo="Carregando municípios" /> : null}

          {estado.status === 'erro' ? (
            <Notice
              tom="warning"
              titulo="Não foi possível carregar os municípios"
              mensagem={mensagemDeErro(estado.erro)}
              acao={{ rotulo: 'Tentar novamente', aoPressionar: recarregar }}
            />
          ) : null}

          {estado.status === 'pronto' && estado.dados.length === 0 ? (
            <Notice tom="tint" mensagem="Nenhum município disponível por enquanto." />
          ) : null}

          {estado.status === 'pronto' && estado.dados.length > 0 ? (
            <>
              <SeletorMunicipio
                rotulo="Seu município"
                municipios={estado.dados}
                selecionado={municipioAtual}
                aoSelecionar={selecionar}
              />
              <Button
                rotulo="Iniciar conversa"
                icone="conversa"
                aoPressionar={() => abrirConversa()}
                accessibilityHint="Abre uma conversa nova neste município"
              />
            </>
          ) : null}
        </View>

        {municipioAtual ? (
          <View style={styles.bloco}>
            <SectionTitle titulo="Serviços disponíveis" />

            {servicos.estado.status === 'carregando' ? (
              <Carregando rotulo="Carregando serviços" />
            ) : null}

            {servicos.estado.status === 'erro' ? (
              <Notice
                tom="warning"
                titulo="Não foi possível carregar os serviços"
                mensagem={mensagemDeErro(servicos.estado.erro)}
                acao={{ rotulo: 'Tentar novamente', aoPressionar: servicos.recarregar }}
              />
            ) : null}

            {servicos.estado.status === 'pronto' && servicos.estado.dados.length === 0 ? (
              <Notice tom="tint" mensagem="Este município ainda não tem serviços disponíveis." />
            ) : null}

            {servicos.estado.status === 'pronto'
              ? servicos.estado.dados.map((servico) => (
                  <ListRow
                    key={servico.id}
                    icone={iconeDoServico(servico.id)}
                    titulo={servico.nome}
                    accessibilityHint="Abre uma conversa com uma pergunta sugerida que você pode editar"
                    aoPressionar={() => abrirConversa(perguntaSugerida(servico))}
                  />
                ))
              : null}
          </View>
        ) : null}

        <Notice mensagem="Você será orientado ao canal oficial." />
      </View>
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  pagina: {
    gap: spacing.xl,
  },
  marca: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  apresentacao: {
    gap: spacing.sm,
  },
  bloco: {
    gap: spacing.md + 2,
  },
});
