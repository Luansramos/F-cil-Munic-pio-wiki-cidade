const MESES = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

function doisDigitos(valor: number): string {
  return String(valor).padStart(2, '0');
}

function mesmoDia(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()
  );
}

export function formatarDataHistorico(iso: string, agora: Date = new Date()): string {
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) {
    return '';
  }

  const hora = `${doisDigitos(data.getHours())}:${doisDigitos(data.getMinutes())}`;
  const ontem = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate() - 1);

  if (mesmoDia(data, agora)) {
    return `Hoje, ${hora}`;
  }
  if (mesmoDia(data, ontem)) {
    return `Ontem, ${hora}`;
  }

  const diaEMes = `${doisDigitos(data.getDate())} ${MESES[data.getMonth()]}`;
  return data.getFullYear() === agora.getFullYear()
    ? `${diaEMes}, ${hora}`
    : `${diaEMes} ${data.getFullYear()}, ${hora}`;
}
