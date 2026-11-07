import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { colors, radii, spacing } from '@/theme';

type SectionTitleProps = {
  titulo: string;
  faixa?: boolean;
};

export function SectionTitle({ titulo, faixa = false }: SectionTitleProps) {
  if (faixa) {
    return (
      <View style={styles.faixa}>
        <AppText variant="label" accessibilityRole="header">
          {titulo}
        </AppText>
      </View>
    );
  }

  return (
    <AppText variant="heading" accessibilityRole="header">
      {titulo}
    </AppText>
  );
}

const styles = StyleSheet.create({
  faixa: {
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md + 2,
    backgroundColor: colors.surfaceTint,
    borderRadius: radii.sm,
  },
});
