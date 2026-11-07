import type { TextStyle } from 'react-native';

export type EstiloTexto = {
  fontSize: number;
  lineHeight: number;
  fontWeight: NonNullable<TextStyle['fontWeight']>;
};

export const typography = {
  title: { fontSize: 32, lineHeight: 38, fontWeight: '700' },
  heading: { fontSize: 20, lineHeight: 26, fontWeight: '700' },
  button: { fontSize: 18, lineHeight: 24, fontWeight: '700' },
  label: { fontSize: 17, lineHeight: 24, fontWeight: '600' },
  body: { fontSize: 17, lineHeight: 24, fontWeight: '400' },
  small: { fontSize: 14, lineHeight: 20, fontWeight: '400' },
  caption: { fontSize: 13, lineHeight: 18, fontWeight: '600' },
} as const satisfies Record<string, EstiloTexto>;

export type TypographyToken = keyof typeof typography;

export const NIVEIS_TEXTO = [0, 1, 2, 3] as const;
export type NivelTexto = (typeof NIVEIS_TEXTO)[number];

export const NIVEL_TEXTO_PADRAO: NivelTexto = 0;

const MULTIPLICADOR_POR_NIVEL: Record<NivelTexto, number> = {
  0: 1,
  1: 1.15,
  2: 1.3,
  3: 1.5,
};

export const LIMITE_ESCALA_SISTEMA = 2;

export function multiplicadorDoNivel(nivel: NivelTexto): number {
  return MULTIPLICADOR_POR_NIVEL[nivel];
}

export function escalarEstilo(estilo: EstiloTexto, nivel: NivelTexto): EstiloTexto {
  const multiplicador = multiplicadorDoNivel(nivel);
  return {
    fontSize: Math.round(estilo.fontSize * multiplicador),
    lineHeight: Math.round(estilo.lineHeight * multiplicador),
    fontWeight: estilo.fontWeight,
  };
}
