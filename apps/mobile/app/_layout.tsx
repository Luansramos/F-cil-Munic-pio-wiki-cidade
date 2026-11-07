import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

import { BancoProvider } from '@/contexts/BancoContext';
import { MunicipioProvider } from '@/contexts/MunicipioContext';
import { colors } from '@/theme';

export default function RootLayout() {
  return (
    <BancoProvider>
      <MunicipioProvider>
        <StatusBar style="dark" />
        <Stack
          screenOptions={{
            headerShown: false,
            contentStyle: { backgroundColor: colors.background },
          }}
        />
      </MunicipioProvider>
    </BancoProvider>
  );
}
