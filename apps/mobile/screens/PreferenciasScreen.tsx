import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { ContainerTela } from '@/components/ContainerTela';
import { Notice } from '@/components/Notice';
import { spacing } from '@/theme';

export function PreferenciasScreen() {
  return (
    <ContainerTela rolavel>
      <View style={styles.pagina}>
        <AppText variant="title" accessibilityRole="header">
          Preferências
        </AppText>
        <Notice
          tom="tint"
          icone="preferencias"
          mensagem="As preferências de texto e privacidade chegam na próxima etapa."
        />
        <Notice mensagem="Este aplicativo orienta. A solicitação é feita no canal oficial." />
      </View>
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  pagina: {
    gap: spacing.xl,
  },
});
