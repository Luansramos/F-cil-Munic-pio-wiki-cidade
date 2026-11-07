export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
  xxxl: 32,
} as const;

export const radii = {
  xs: 4,
  sm: 10,
  md: 12,
  lg: 14,
  xl: 18,
  sheet: 24,
  pill: 999,
} as const;

export const borderWidth = {
  hairline: 1,
  strong: 1.5,
  emphasis: 2,
} as const;

export type SpacingToken = keyof typeof spacing;
export type RadiusToken = keyof typeof radii;
