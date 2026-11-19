import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

import { BancoProvider } from '@/contexts/BancoContext';
import { MunicipioProvider } from '@/contexts/MunicipioContext';
import { PreferenciasProvider } from '@/contexts/PreferenciasContext';
import { colors } from '@/theme';

export default function RootLayout() {
  return (
    <BancoProvider>
      <PreferenciasProvider>
        <MunicipioProvider>
          <StatusBar style="dark" />
          <Stack
            screenOptions={{
              headerShown: false,
              contentStyle: { backgroundColor: colors.background },
            }}
          />
        </MunicipioProvider>
      </PreferenciasProvider>
    </BancoProvider>
  );
}
