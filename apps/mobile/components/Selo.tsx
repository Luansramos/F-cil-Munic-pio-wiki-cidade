import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { colors, radii, spacing } from '@/theme';

type SeloProps = {
  texto: string;
};

export function Selo({ texto }: SeloProps) {
  return (
    <View style={styles.selo}>
      <AppText variant="caption" color="primary">
        {texto}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  selo: {
    paddingVertical: spacing.xs + 2,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.surfaceSelected,
    borderRadius: radii.pill,
  },
});
