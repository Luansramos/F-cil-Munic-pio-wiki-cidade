const LATENCIA_MOCK_PADRAO_MS = 600;

function lerLatenciaMock(valor: string | undefined): number {
  const numero = Number(valor);
  if (valor === undefined || valor.trim() === '' || !Number.isFinite(numero) || numero < 0) {
    return LATENCIA_MOCK_PADRAO_MS;
  }
  return numero;
}

export const config = {
  apiUrl: (process.env.EXPO_PUBLIC_API_URL ?? '').replace(/\/+$/, ''),
  usarMock: process.env.EXPO_PUBLIC_USAR_MOCK !== 'false',
  mockLatenciaMs: lerLatenciaMock(process.env.EXPO_PUBLIC_MOCK_LATENCIA_MS),
} as const;
