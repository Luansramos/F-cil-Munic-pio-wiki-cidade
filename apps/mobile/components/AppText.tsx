import { Text } from 'react-native';
import type { TextProps } from 'react-native';

import { LIMITE_ESCALA_SISTEMA, colors, typography } from '@/theme';
import type { ColorToken, TypographyToken } from '@/theme';

type AppTextProps = TextProps & {
  variant?: TypographyToken;
  color?: ColorToken;
  align?: 'left' | 'center' | 'right';
};

export function AppText({
  variant = 'body',
  color = 'text',
  align,
  style,
  ...rest
}: AppTextProps) {
  return (
    <Text
      maxFontSizeMultiplier={LIMITE_ESCALA_SISTEMA}
      {...rest}
      style={[typography[variant], { color: colors[color] }, align ? { textAlign: align } : null, style]}
    />
  );
}
