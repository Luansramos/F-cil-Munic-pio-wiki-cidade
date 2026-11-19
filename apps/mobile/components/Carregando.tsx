import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { colors, spacing } from '@/theme';

type CarregandoProps = {
  rotulo: string;
};

export function Carregando({ rotulo }: CarregandoProps) {
  return (
    <View
      accessible
      accessibilityRole="progressbar"
      accessibilityLabel={rotulo}
      accessibilityLiveRegion="polite"
      style={styles.linha}
    >
      <ActivityIndicator color={colors.primary} />
      <AppText variant="small" color="textMuted">
        {rotulo}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingVertical: spacing.md,
  },
});
