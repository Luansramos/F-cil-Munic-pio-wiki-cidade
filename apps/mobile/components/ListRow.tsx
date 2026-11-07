import { Pressable, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import type { NomeIcone } from '@/components/Icon';
import { borderWidth, colors, radii, spacing } from '@/theme';

type Tom = 'tint' | 'surface';
type Fim = 'seta' | 'check' | 'nenhum';

type ListRowProps = {
  titulo: string;
  aoPressionar: () => void;
  sobretitulo?: string;
  subtitulo?: string;
  icone?: NomeIcone;
  tom?: Tom;
  fim?: Fim;
  destaque?: boolean;
  accessibilityLabel?: string;
  accessibilityHint?: string;
  selecionado?: boolean;
};

const FUNDOS = {
  tint: { fundo: colors.surfaceTint, borda: colors.borderTint },
  surface: { fundo: colors.surface, borda: colors.border },
} as const;

export function ListRow({
  titulo,
  aoPressionar,
  sobretitulo,
  subtitulo,
  icone,
  tom = 'tint',
  fim = 'seta',
  destaque = false,
  accessibilityLabel,
  accessibilityHint,
  selecionado,
}: ListRowProps) {
  const cores = FUNDOS[tom];

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={accessibilityLabel ?? titulo}
      accessibilityHint={accessibilityHint}
      accessibilityState={selecionado === undefined ? undefined : { selected: selecionado }}
      onPress={aoPressionar}
      style={({ pressed }) => [
        styles.linha,
        { backgroundColor: cores.fundo, borderColor: cores.borda },
        pressed ? styles.pressionada : null,
      ]}
    >
      {icone ? <Icon name={icone} color="primary" /> : null}
      <View style={styles.textos}>
        {sobretitulo ? (
          <AppText variant="small" color="textMuted">
            {sobretitulo}
          </AppText>
        ) : null}
        <AppText variant={destaque ? 'heading' : 'label'}>{titulo}</AppText>
        {subtitulo ? (
          <AppText variant="small" color="textMuted">
            {subtitulo}
          </AppText>
        ) : null}
      </View>
      {fim === 'seta' ? <Icon name="avancar" /> : null}
      {fim === 'check' ? <Icon name="check" color="primary" /> : null}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  linha: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderWidth: borderWidth.hairline,
    borderRadius: radii.lg,
  },
  textos: {
    flex: 1,
  },
  pressionada: {
    opacity: 0.85,
  },
});
