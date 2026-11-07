import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Button } from '@/components/Button';
import { Icon } from '@/components/Icon';
import type { NomeIcone } from '@/components/Icon';
import { borderWidth, colors, radii, spacing } from '@/theme';
import type { ColorToken } from '@/theme';

type Tom = 'info' | 'tint' | 'warning';

type NoticeProps = {
  mensagem: string;
  titulo?: string;
  tom?: Tom;
  icone?: NomeIcone;
  acao?: { rotulo: string; aoPressionar: () => void };
};

const TONS: Record<
  Tom,
  {
    fundo: string;
    borda: string;
    icone: ColorToken;
    titulo: ColorToken;
    texto: ColorToken;
    iconePadrao: NomeIcone;
  }
> = {
  info: {
    fundo: colors.surface,
    borda: colors.border,
    icone: 'textMuted',
    titulo: 'text',
    texto: 'textMuted',
    iconePadrao: 'info',
  },
  tint: {
    fundo: colors.surfaceTint,
    borda: colors.borderTint,
    icone: 'primary',
    titulo: 'text',
    texto: 'textMuted',
    iconePadrao: 'info',
  },
  warning: {
    fundo: colors.warningSurface,
    borda: colors.warningBorder,
    icone: 'warningIcon',
    titulo: 'warningTitle',
    texto: 'warningText',
    iconePadrao: 'alerta',
  },
};

export function Notice({ mensagem, titulo, tom = 'info', icone, acao }: NoticeProps) {
  const estilo = TONS[tom];
  const variante = tom === 'warning' ? 'body' : 'small';

  return (
    <View
      accessibilityRole={tom === 'warning' ? 'alert' : undefined}
      style={[styles.caixa, { backgroundColor: estilo.fundo, borderColor: estilo.borda }]}
    >
      <View style={styles.linha}>
        <Icon name={icone ?? estilo.iconePadrao} color={estilo.icone} />
        <View style={styles.textos}>
          {titulo ? (
            <AppText variant="label" color={estilo.titulo}>
              {titulo}
            </AppText>
          ) : null}
          <AppText variant={variante} color={estilo.texto}>
            {mensagem}
          </AppText>
        </View>
      </View>
      {acao ? (
        <Button rotulo={acao.rotulo} aoPressionar={acao.aoPressionar} larguraTotal={false} />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  caixa: {
    gap: spacing.md,
    padding: spacing.md + 2,
    borderWidth: borderWidth.hairline,
    borderRadius: radii.md,
  },
  linha: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.md,
  },
  textos: {
    flex: 1,
    gap: spacing.xs,
  },
});
