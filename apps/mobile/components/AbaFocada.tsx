import { useIsFocused } from 'expo-router';
import type { ReactNode } from 'react';
import { Platform, StyleSheet, View } from 'react-native';
import type { ViewStyle } from 'react-native';

type AbaFocadaProps = {
  children: ReactNode;
};

const ESCONDIDA_NA_WEB = (Platform.OS === 'web' ? { visibility: 'hidden' } : {}) as ViewStyle;

export function AbaFocada({ children }: AbaFocadaProps) {
  const focada = useIsFocused();

  return <View style={[styles.raiz, focada ? null : ESCONDIDA_NA_WEB]}>{children}</View>;
}

const styles = StyleSheet.create({
  raiz: {
    flex: 1,
  },
});
