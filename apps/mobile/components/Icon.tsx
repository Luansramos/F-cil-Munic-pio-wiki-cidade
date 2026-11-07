import { MaterialCommunityIcons } from '@expo/vector-icons';
import type { ComponentProps } from 'react';

import { colors, iconSize } from '@/theme';
import type { ColorToken } from '@/theme';

type NomeGlifo = ComponentProps<typeof MaterialCommunityIcons>['name'];

const GLIFOS = {
  predio: 'office-building-outline',
  local: 'map-marker-outline',
  avancar: 'chevron-right',
  expandir: 'chevron-down',
  voltar: 'arrow-left',
  conversa: 'chat-outline',
  apagar: 'trash-can-outline',
  lixo: 'trash-can-outline',
  lampada: 'lightbulb-outline',
  sofa: 'sofa-outline',
  info: 'information-outline',
  inicio: 'home-outline',
  preferencias: 'cog-outline',
  privacidade: 'shield-check-outline',
  alerta: 'alert-outline',
  semInternet: 'wifi-off',
  enviar: 'send-outline',
  documento: 'file-document-outline',
  fechar: 'close',
  check: 'check',
} as const satisfies Record<string, NomeGlifo>;

export type NomeIcone = keyof typeof GLIFOS;

type IconProps = {
  name: NomeIcone;
  size?: keyof typeof iconSize | number;
  color?: ColorToken;
};

export function Icon({ name, size = 'md', color = 'text' }: IconProps) {
  const tamanho = typeof size === 'number' ? size : iconSize[size];
  return (
    <MaterialCommunityIcons
      name={GLIFOS[name]}
      size={tamanho}
      color={colors[color]}
      aria-hidden
    />
  );
}
