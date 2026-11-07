import type { ComponentProps } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';
import type { Tabs } from 'expo-router';

import { AppText } from '@/components/AppText';
import { borderWidth, colors, radii, spacing } from '@/theme';

type BarraAbasProps = Parameters<NonNullable<ComponentProps<typeof Tabs>['tabBar']>>[0];

export function BarraAbas({ state, descriptors, navigateToTab, insets }: BarraAbasProps) {
  return (
    <View
      accessibilityRole="tablist"
      style={[styles.barra, { paddingBottom: Math.max(insets.bottom, spacing.md + 2) }]}
    >
      {state.routes.map((rota, indice) => {
        const focada = state.index === indice;
        const opcoes = descriptors[rota.key].options;
        const rotulo = opcoes.title ?? rota.name;

        return (
          <Pressable
            key={rota.key}
            accessibilityRole="tab"
            accessibilityLabel={opcoes.tabBarAccessibilityLabel ?? rotulo}
            accessibilityState={{ selected: focada }}
            onPress={() => navigateToTab(rota.key)}
            style={[styles.aba, focada ? styles.abaAtiva : null]}
          >
            {opcoes.tabBarIcon?.({ focused: focada, color: '', size: 24 })}
            <AppText variant="caption" color={focada ? 'primary' : 'textMuted'}>
              {rotulo}
            </AppText>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  barra: {
    flexDirection: 'row',
    gap: spacing.sm,
    paddingTop: spacing.sm,
    paddingHorizontal: spacing.md,
    backgroundColor: colors.surface,
    borderTopWidth: borderWidth.hairline,
    borderTopColor: colors.border,
  },
  aba: {
    flex: 1,
    minHeight: 60,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.xs,
    borderRadius: radii.lg,
  },
  abaAtiva: {
    backgroundColor: colors.surfaceSelected,
  },
});
