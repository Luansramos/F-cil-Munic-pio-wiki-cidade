import { useState } from 'react';
import { Pressable, StyleSheet, TextInput, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Icon } from '@/components/Icon';
import { PERGUNTA_MAX } from '@/config/limites';
import { useNivelTexto } from '@/contexts/NivelTextoContext';
import {
  borderWidth,
  colors,
  escalarEstilo,
  radii,
  spacing,
  touchTarget,
  typography,
} from '@/theme';

type CampoPerguntaProps = {
  valor: string;
  aoMudar: (texto: string) => void;
  aoEnviar: () => void;
  desativado?: boolean;
};

export function CampoPergunta({
  valor,
  aoMudar,
  aoEnviar,
  desativado = false,
}: CampoPerguntaProps) {
  const [focado, setFocado] = useState(false);
  const nivel = useNivelTexto();
  const textoDoCampo = escalarEstilo(typography.body, nivel);
  const podeEnviar = !desativado && valor.trim() !== '';

  return (
    <View style={styles.raiz}>
      <View style={styles.linha}>
        <TextInput
          accessibilityLabel="Escreva sua dúvida"
          placeholder="Escreva sua dúvida..."
          placeholderTextColor={colors.textMuted}
          value={valor}
          onChangeText={aoMudar}
          onFocus={() => setFocado(true)}
          onBlur={() => setFocado(false)}
          editable={!desativado}
          multiline
          maxLength={PERGUNTA_MAX}
          maxFontSizeMultiplier={2}
          style={[
            styles.campo,
            { fontSize: textoDoCampo.fontSize, lineHeight: textoDoCampo.lineHeight },
            focado ? styles.campoFocado : null,
          ]}
        />
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Enviar pergunta"
          accessibilityState={{ disabled: !podeEnviar }}
          disabled={!podeEnviar}
          onPress={aoEnviar}
          style={({ pressed }) => [
            styles.enviar,
            !podeEnviar ? styles.enviarDesativado : null,
            pressed ? styles.pressionado : null,
          ]}
        >
          <Icon name="enviar" color="onPrimary" />
        </Pressable>
      </View>
      <AppText variant="small" color="textMuted">
        Não digite CPF, senhas ou dados de saúde.
      </AppText>
    </View>
  );
}

const TAMANHO_BOTAO = Math.max(52, touchTarget ?? 48);

const styles = StyleSheet.create({
  raiz: {
    gap: spacing.sm,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    backgroundColor: colors.surface,
    borderTopWidth: borderWidth.hairline,
    borderTopColor: colors.border,
  },
  linha: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: spacing.md,
  },
  campo: {
    flex: 1,
    minHeight: TAMANHO_BOTAO,
    maxHeight: 140,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    color: colors.text,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.borderStrong,
    borderRadius: radii.sheet,
  },
  campoFocado: {
    borderWidth: borderWidth.emphasis,
    borderColor: colors.primary,
  },
  enviar: {
    width: TAMANHO_BOTAO,
    height: TAMANHO_BOTAO,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.primary,
    borderRadius: radii.pill,
  },
  enviarDesativado: {
    opacity: 0.5,
  },
  pressionado: {
    opacity: 0.85,
  },
});
