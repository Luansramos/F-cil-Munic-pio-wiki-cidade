import type { ReactNode } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import { colors, radii, spacing, touchTarget } from '@/theme';

type FolhaModalProps = {
  visivel: boolean;
  titulo: string;
  aoFechar: () => void;
  children: ReactNode;
};

export function FolhaModal({ visivel, titulo, aoFechar, children }: FolhaModalProps) {
  return (
    <Modal
      visible={visivel}
      transparent
      animationType="slide"
      onRequestClose={aoFechar}
      accessibilityViewIsModal
    >
      <View style={styles.raiz}>
        <Pressable
          accessible={false}
          tabIndex={-1}
          importantForAccessibility="no"
          accessibilityElementsHidden
          onPress={aoFechar}
          style={styles.fundo}
        />
        <View accessibilityViewIsModal style={styles.folha}>
          <View style={styles.alca} />
          <View style={styles.cabecalho}>
            <AppText variant="heading" accessibilityRole="header" style={styles.titulo}>
              {titulo}
            </AppText>
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={`Fechar ${titulo}`}
              onPress={aoFechar}
              style={styles.fechar}
            >
              <Icon name="fechar" />
            </Pressable>
          </View>
          <ScrollView contentContainerStyle={styles.conteudo}>{children}</ScrollView>
        </View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  raiz: {
    flex: 1,
    justifyContent: 'flex-end',
  },
  fundo: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    backgroundColor: colors.overlay,
  },
  folha: {
    maxHeight: '80%',
    gap: spacing.md + 2,
    paddingTop: spacing.md,
    paddingHorizontal: spacing.xl,
    paddingBottom: spacing.xxxl - 4,
    backgroundColor: colors.surface,
    borderTopLeftRadius: radii.sheet,
    borderTopRightRadius: radii.sheet,
  },
  alca: {
    alignSelf: 'center',
    width: 44,
    height: 5,
    borderRadius: 3,
    backgroundColor: colors.handle,
  },
  cabecalho: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
  },
  titulo: {
    flex: 1,
  },
  fechar: {
    width: Math.max(48, touchTarget ?? 48),
    height: Math.max(48, touchTarget ?? 48),
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceTint,
  },
  conteudo: {
    gap: spacing.md,
  },
});
