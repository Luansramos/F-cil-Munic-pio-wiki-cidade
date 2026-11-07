import { useLocalSearchParams, useRouter } from 'expo-router';
import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { CabecalhoTela } from '@/components/CabecalhoTela';
import { ContainerTela } from '@/components/ContainerTela';
import { Notice } from '@/components/Notice';
import { Selo } from '@/components/Selo';
import { useMunicipioAtual } from '@/contexts/MunicipioContext';
import { spacing } from '@/theme';
import { formatarMunicipio } from '@/utils/municipio';

type ParametrosConversa = {
  id: string;
  municipioId?: string;
  perguntaSugerida?: string;
};

export function ConversaScreen() {
  const router = useRouter();
  const { municipioId, perguntaSugerida } = useLocalSearchParams<ParametrosConversa>();
  const { estado } = useMunicipioAtual();

  const municipio =
    estado.status === 'pronto' ? estado.dados.find((item) => item.id === municipioId) : undefined;

  function voltar() {
    if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/');
    }
  }

  return (
    <ContainerTela bordas={['top', 'bottom']} semPadding>
      <CabecalhoTela
        titulo="Conversa"
        subtitulo={municipio ? formatarMunicipio(municipio) : undefined}
        aoVoltar={voltar}
        direita={<Selo texto="Demonstração" />}
      />
      <View style={styles.corpo}>
        {perguntaSugerida ? (
          <View style={styles.sugestao}>
            <AppText variant="small" color="textMuted">
              Pergunta sugerida
            </AppText>
            <AppText variant="body">{perguntaSugerida}</AppText>
          </View>
        ) : null}
        <Notice
          tom="tint"
          icone="conversa"
          mensagem="A conversa será concluída na próxima etapa. Esta tela já recebe o município e a pergunta sugerida."
        />
      </View>
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  corpo: {
    padding: spacing.lg,
    gap: spacing.lg,
  },
  sugestao: {
    gap: spacing.xs,
  },
});
