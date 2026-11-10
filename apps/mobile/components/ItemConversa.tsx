import { Pressable, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import { borderWidth, colors, radii, spacing, touchTarget } from '@/theme';

type ItemConversaProps = {
  titulo: string;
  data: string;
  municipio?: string;
  aoAbrir: () => void;
  aoApagar: () => void;
};

export function ItemConversa({ titulo, data, municipio, aoAbrir, aoApagar }: ItemConversaProps) {
  const detalhes = [municipio, data].filter((parte) => parte !== undefined && parte !== '');

  return (
    <View style={styles.linha}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`${titulo}. ${detalhes.join(', ')}`}
        accessibilityHint="Abre a conversa"
        onPress={aoAbrir}
        style={({ pressed }) => [styles.abrir, pressed ? styles.pressionado : null]}
      >
        <Icon name="conversa" color="primary" />
        <View style={styles.textos}>
          <AppText variant="label" numberOfLines={2}>
            {titulo}
          </AppText>
          {detalhes.length > 0 ? (
            <AppText variant="small" color="textMuted">
              {detalhes.join(' · ')}
            </AppText>
          ) : null}
        </View>
        <Icon name="avancar" />
      </Pressable>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel={`Apagar conversa ${titulo}`}
        onPress={aoApagar}
        style={({ pressed }) => [styles.apagar, pressed ? styles.pressionado : null]}
      >
        <Icon name="apagar" />
      </Pressable>
    </View>
  );
}

const TAMANHO_APAGAR = Math.max(48, touchTarget ?? 48);

const styles = StyleSheet.create({
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    paddingLeft: spacing.lg,
    paddingRight: spacing.sm,
    paddingVertical: spacing.sm,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.border,
    borderRadius: radii.lg,
  },
  abrir: {
    flex: 1,
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  textos: {
    flex: 1,
  },
  apagar: {
    width: TAMANHO_APAGAR,
    height: TAMANHO_APAGAR,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surfaceTint,
    borderRadius: radii.md,
  },
  pressionado: {
    opacity: 0.7,
  },
});
