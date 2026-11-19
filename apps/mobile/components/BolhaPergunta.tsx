import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { colors, radii, spacing } from '@/theme';

type BolhaPerguntaProps = {
  texto: string;
  enviando?: boolean;
};

export function BolhaPergunta({ texto, enviando = false }: BolhaPerguntaProps) {
  return (
    <View
      accessible
      accessibilityLabel={`Você perguntou: ${texto}`}
      style={[styles.bolha, enviando ? styles.enviando : null]}
    >
      <AppText variant="body" selectable>
        {texto}
      </AppText>
    </View>
  );
}

const styles = StyleSheet.create({
  bolha: {
    alignSelf: 'flex-end',
    maxWidth: '85%',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.lg,
    backgroundColor: colors.surfaceSelected,
    borderRadius: radii.xl,
    borderBottomRightRadius: radii.sm,
  },
  enviando: {
    opacity: 0.7,
  },
});
