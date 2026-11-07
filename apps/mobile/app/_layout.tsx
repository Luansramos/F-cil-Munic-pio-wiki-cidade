import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

import { MunicipioProvider } from '@/contexts/MunicipioContext';
import { colors } from '@/theme';

export default function RootLayout() {
  return (
    <MunicipioProvider>
      <StatusBar style="dark" />
      <Stack
        screenOptions={{
          headerShown: false,
          contentStyle: { backgroundColor: colors.background },
        }}
      />
    </MunicipioProvider>
  );
}
