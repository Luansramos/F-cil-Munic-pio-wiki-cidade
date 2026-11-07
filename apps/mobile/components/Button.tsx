import { Pressable, StyleSheet } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import type { NomeIcone } from '@/components/Icon';
import { borderWidth, colors, radii, spacing, touchTarget } from '@/theme';
import type { ColorToken } from '@/theme';

type Variante = 'primary' | 'secondary' | 'danger';

type ButtonProps = {
  rotulo: string;
  aoPressionar: () => void;
  variante?: Variante;
  icone?: NomeIcone;
  desativado?: boolean;
  larguraTotal?: boolean;
  accessibilityHint?: string;
};

const ESTILOS: Record<
  Variante,
  { fundo: ColorToken; borda: ColorToken; texto: ColorToken; larguraBorda: number }
> = {
  primary: {
    fundo: 'primary',
    borda: 'primary',
    texto: 'onPrimary',
    larguraBorda: 0,
  },
  secondary: {
    fundo: 'surface',
    borda: 'borderStrong',
    texto: 'text',
    larguraBorda: borderWidth.strong,
  },
  danger: {
    fundo: 'surface',
    borda: 'danger',
    texto: 'danger',
    larguraBorda: borderWidth.strong,
  },
};

export function Button({
  rotulo,
  aoPressionar,
  variante = 'primary',
  icone,
  desativado = false,
  larguraTotal = true,
  accessibilityHint,
}: ButtonProps) {
  const estilo = ESTILOS[variante];

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={rotulo}
      accessibilityHint={accessibilityHint}
      accessibilityState={{ disabled: desativado }}
      disabled={desativado}
      onPress={aoPressionar}
      style={({ pressed }) => [
        styles.base,
        {
          backgroundColor: colors[estilo.fundo],
          borderColor: colors[estilo.borda],
          borderWidth: estilo.larguraBorda,
        },
        larguraTotal ? styles.larguraTotal : styles.larguraAjustada,
        pressed ? styles.pressionado : null,
        desativado ? styles.desativado : null,
      ]}
    >
      {icone ? <Icon name={icone} color={estilo.texto} /> : null}
      <AppText variant="button" color={estilo.texto}>
        {rotulo}
      </AppText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  base: {
    minHeight: Math.max(56, touchTarget ?? 48),
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.sm,
    paddingHorizontal: spacing.xl,
    borderRadius: radii.lg,
  },
  larguraTotal: {
    alignSelf: 'stretch',
  },
  larguraAjustada: {
    alignSelf: 'flex-start',
  },
  pressionado: {
    opacity: 0.85,
  },
  desativado: {
    opacity: 0.5,
  },
});
