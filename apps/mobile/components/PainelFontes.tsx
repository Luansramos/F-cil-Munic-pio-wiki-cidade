import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { CartaoFonte } from '@/components/CartaoFonte';
import { FolhaModal } from '@/components/FolhaModal';
import { Icon } from '@/components/Icon';
import { spacing } from '@/theme';
import type { Fonte } from '@/types';

type PainelFontesProps = {
  visivel: boolean;
  fontes: Fonte[];
  aoFechar: () => void;
};

export function PainelFontes({ visivel, fontes, aoFechar }: PainelFontesProps) {
  return (
    <FolhaModal visivel={visivel} titulo="Fontes desta resposta" aoFechar={aoFechar}>
      {fontes.map((fonte, indice) => (
        <CartaoFonte key={`${indice}-${fonte.titulo}`} fonte={fonte} />
      ))}
      <View style={styles.nota}>
        <Icon name="info" size="sm" color="textMuted" />
        <AppText variant="small" color="textMuted" style={styles.textoNota}>
          A data aparece só quando a fonte a informa.
        </AppText>
      </View>
    </FolhaModal>
  );
}

const styles = StyleSheet.create({
  nota: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  textoNota: {
    flex: 1,
  },
});
