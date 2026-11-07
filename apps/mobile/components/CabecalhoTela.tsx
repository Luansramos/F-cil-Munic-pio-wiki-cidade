import type { ReactNode } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import { borderWidth, colors, radii, spacing, touchTarget } from '@/theme';

type CabecalhoTelaProps = {
  titulo: string;
  subtitulo?: string;
  aoVoltar?: () => void;
  direita?: ReactNode;
};

export function CabecalhoTela({ titulo, subtitulo, aoVoltar, direita }: CabecalhoTelaProps) {
  return (
    <View style={styles.cabecalho}>
      {aoVoltar ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Voltar"
          onPress={aoVoltar}
          style={styles.voltar}
        >
          <Icon name="voltar" size="lg" />
        </Pressable>
      ) : null}
      <View style={styles.textos}>
        <AppText variant="heading" accessibilityRole="header">
          {titulo}
        </AppText>
        {subtitulo ? (
          <AppText variant="small" color="textMuted">
            {subtitulo}
          </AppText>
        ) : null}
      </View>
      {direita}
    </View>
  );
}

const styles = StyleSheet.create({
  cabecalho: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingVertical: spacing.md,
    paddingLeft: spacing.sm,
    paddingRight: spacing.lg,
    backgroundColor: colors.surface,
    borderBottomWidth: borderWidth.hairline,
    borderBottomColor: colors.border,
  },
  voltar: {
    width: Math.max(48, touchTarget ?? 48),
    height: Math.max(48, touchTarget ?? 48),
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
  },
  textos: {
    flex: 1,
  },
});
