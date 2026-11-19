export const colors = {
  background: '#F7FBFA',
  surface: '#FFFFFF',
  surfaceTint: '#E3F3EF',
  surfaceSelected: '#D5EEE8',
  border: '#D6E6E1',
  borderTint: '#C5E3DC',
  borderStrong: '#5F8D82',
  text: '#12201D',
  textMuted: '#3D4F4A',
  primary: '#0B6B5E',
  onPrimary: '#FFFFFF',
  danger: '#B42318',
  warningSurface: '#FFF4E0',
  warningBorder: '#E3B866',
  warningIcon: '#6B4300',
  warningTitle: '#4A2E00',
  warningText: '#3A2500',
  overlay: 'rgba(18, 32, 29, 0.55)',
  handle: '#B7CCC6',
} as const;

export type ColorToken = keyof typeof colors;
