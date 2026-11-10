import { Text } from 'react-native';
import type { TextProps } from 'react-native';

import { useNivelTexto } from '@/contexts/NivelTextoContext';
import {
  LIMITE_ESCALA_SISTEMA,
  colors,
  escalarEstilo,
  nivelParaVariante,
  typography,
} from '@/theme';
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
  const nivel = useNivelTexto();

  return (
    <Text
      maxFontSizeMultiplier={LIMITE_ESCALA_SISTEMA}
      {...rest}
      style={[
        escalarEstilo(typography[variant], nivelParaVariante(variant, nivel)),
        { color: colors[color] },
        align ? { textAlign: align } : null,
        style,
      ]}
    />
  );
}
