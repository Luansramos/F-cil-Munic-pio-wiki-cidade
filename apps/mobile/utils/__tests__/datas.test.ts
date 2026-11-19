import { formatarDataHistorico } from '../datas';

const AGORA = new Date(2026, 10, 9, 15, 0);

function iso(ano: number, mes: number, dia: number, hora: number, minuto: number): string {
  return new Date(ano, mes - 1, dia, hora, minuto).toISOString();
}

describe('formatarDataHistorico', () => {
  it('mostra "Hoje" com hora e minuto de dois dígitos', () => {
    expect(formatarDataHistorico(iso(2026, 11, 9, 9, 5), AGORA)).toBe('Hoje, 09:05');
  });

  it('mostra "Ontem"', () => {
    expect(formatarDataHistorico(iso(2026, 11, 8, 16, 20), AGORA)).toBe('Ontem, 16:20');
  });

  it('mostra dia e mês abreviado no mesmo ano', () => {
    expect(formatarDataHistorico(iso(2026, 11, 3, 9, 15), AGORA)).toBe('03 nov, 09:15');
  });

  it('inclui o ano quando é de outro ano', () => {
    expect(formatarDataHistorico(iso(2025, 12, 31, 23, 59), AGORA)).toBe('31 dez 2025, 23:59');
  });

  it('trata o dia anterior na virada de mês', () => {
    const primeiroDoMes = new Date(2026, 11, 1, 10, 0);

    expect(formatarDataHistorico(iso(2026, 11, 30, 8, 0), primeiroDoMes)).toBe('Ontem, 08:00');
  });

  it('devolve texto vazio para data inválida', () => {
    expect(formatarDataHistorico('não é data', AGORA)).toBe('');
  });
});
