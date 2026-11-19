import { Linking, Pressable, StyleSheet } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import { spacing, touchTarget } from '@/theme';

type LinkExternoProps = {
  rotulo: string;
  url: string;
  accessibilityLabel?: string;
};

export function LinkExterno({ rotulo, url, accessibilityLabel }: LinkExternoProps) {
  function abrir() {
    Linking.openURL(url).catch(() => undefined);
  }

  return (
    <Pressable
      accessibilityRole="link"
      accessibilityLabel={accessibilityLabel ?? rotulo}
      accessibilityHint="Abre o site no navegador"
      onPress={abrir}
      style={({ pressed }) => [styles.link, pressed ? styles.pressionado : null]}
    >
      <AppText variant="label" color="primary" style={styles.texto}>
        {rotulo}
      </AppText>
      <Icon name="externo" size="sm" color="primary" />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  link: {
    minHeight: Math.max(48, touchTarget ?? 48),
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.xs,
  },
  texto: {
    textDecorationLine: 'underline',
  },
  pressionado: {
    opacity: 0.7,
  },
});
