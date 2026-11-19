import { Tabs } from 'expo-router';

import { BarraAbas } from '@/components/BarraAbas';
import { Icon } from '@/components/Icon';

export default function TabsLayout() {
  return (
    <Tabs screenOptions={{ headerShown: false }} tabBar={(props) => <BarraAbas {...props} />}>
      <Tabs.Screen
        name="index"
        options={{
          title: 'Início',
          tabBarIcon: ({ focused }) => (
            <Icon name="inicio" color={focused ? 'primary' : 'textMuted'} />
          ),
        }}
      />
      <Tabs.Screen
        name="historico"
        options={{
          title: 'Histórico',
          tabBarIcon: ({ focused }) => (
            <Icon name="conversa" color={focused ? 'primary' : 'textMuted'} />
          ),
        }}
      />
      <Tabs.Screen
        name="preferencias"
        options={{
          title: 'Preferências',
          tabBarIcon: ({ focused }) => (
            <Icon name="preferencias" color={focused ? 'primary' : 'textMuted'} />
          ),
        }}
      />
    </Tabs>
  );
}
