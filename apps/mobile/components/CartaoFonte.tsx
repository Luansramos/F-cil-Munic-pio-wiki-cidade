import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { LinkExterno } from '@/components/LinkExterno';
import { borderWidth, colors, radii, spacing } from '@/theme';
import type { Fonte } from '@/types';
import { ehUrlSegura, enderecoExibido, formatarDataConsulta } from '@/utils/links';

type CartaoFonteProps = {
  fonte: Fonte;
};

export function CartaoFonte({ fonte }: CartaoFonteProps) {
  const temLink = ehUrlSegura(fonte.url);
  const endereco = temLink ? enderecoExibido(fonte.url) : undefined;
  const data = formatarDataConsulta(fonte.dataConsulta);

  return (
    <View style={styles.cartao}>
      <AppText variant="label">{fonte.titulo}</AppText>
      {endereco ? (
        <AppText variant="small" color="textMuted" selectable>
          {endereco}
        </AppText>
      ) : null}
      {data ? (
        <AppText variant="small" color="textMuted">
          Consultada em {data}
        </AppText>
      ) : null}
      {temLink ? (
        <LinkExterno
          rotulo="Abrir fonte"
          url={fonte.url}
          accessibilityLabel={`Abrir fonte ${fonte.titulo}`}
        />
      ) : (
        <AppText variant="small" color="textMuted">
          Esta fonte de exemplo não tem link.
        </AppText>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  cartao: {
    gap: spacing.xs,
    padding: spacing.lg,
    backgroundColor: colors.background,
    borderWidth: borderWidth.hairline,
    borderColor: colors.borderTint,
    borderRadius: radii.xl,
  },
});
