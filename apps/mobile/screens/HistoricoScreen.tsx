import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { ContainerTela } from '@/components/ContainerTela';
import { Notice } from '@/components/Notice';
import { spacing } from '@/theme';

export function HistoricoScreen() {
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
        <Notice tom="tint" icone="conversa" mensagem="Você ainda não tem conversas salvas." />
      </View>
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  pagina: {
    gap: spacing.xl,
  },
  apresentacao: {
    gap: spacing.xs + 2,
  },
});
