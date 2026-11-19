import type { ReactNode } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import type { Edge } from 'react-native-safe-area-context';

import { colors, spacing } from '@/theme';

type ContainerTelaProps = {
  children: ReactNode;
  rolavel?: boolean;
  semPadding?: boolean;
  bordas?: Edge[];
};

export function ContainerTela({
  children,
  rolavel = false,
  semPadding = false,
  bordas = ['top'],
}: ContainerTelaProps) {
  return (
    <SafeAreaView edges={bordas} style={styles.raiz}>
      {rolavel ? (
        <ScrollView
          keyboardShouldPersistTaps="handled"
          contentContainerStyle={[styles.conteudo, semPadding ? null : styles.padding]}
        >
          {children}
        </ScrollView>
      ) : (
        <View style={[styles.corpo, semPadding ? null : styles.padding]}>{children}</View>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  raiz: {
    flex: 1,
    backgroundColor: colors.background,
  },
  corpo: {
    flex: 1,
  },
  conteudo: {
    flexGrow: 1,
  },
  padding: {
    paddingHorizontal: spacing.xl,
    paddingTop: spacing.xxl,
    paddingBottom: spacing.xl,
  },
});
