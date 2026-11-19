import { Platform } from 'react-native';

export const touchTarget = Platform.select({ ios: 44, default: 48 });

export const iconSize = {
  sm: 20,
  md: 24,
  lg: 28,
} as const;
