import { ehUrlSegura, enderecoExibido, formatarDataConsulta } from '../links';

describe('ehUrlSegura', () => {
  it.each([
    ['https://exemplo.gov.br', true],
    ['https://exemplo.gov.br/servico?ref=1#topo', true],
    ['HTTPS://exemplo.gov.br/a', true],
    ['http://exemplo.gov.br', false],
    ['javascript:alert(1)', false],
    ['https://usuario:senha@exemplo.gov.br', false],
    ['https://usuario@exemplo.gov.br', false],
    ['https://', false],
    ['/relativo', false],
    ['', false],
    ['https://exemplo.gov.br/com espaço', false],
  ])('%s -> %s', (url, esperado) => {
    expect(ehUrlSegura(url)).toBe(esperado);
  });

  it('rejeita ausência de URL', () => {
    expect(ehUrlSegura(undefined)).toBe(false);
  });
});

describe('enderecoExibido', () => {
  it('devolve só o domínio', () => {
    expect(enderecoExibido('https://exemplo.gov.br/a/b?x=1')).toBe('exemplo.gov.br');
  });

  it('devolve undefined para URL inválida', () => {
    expect(enderecoExibido('exemplo.gov.br')).toBeUndefined();
  });
});

describe('formatarDataConsulta', () => {
  it('formata yyyy-MM-dd em dd/MM/yyyy', () => {
    expect(formatarDataConsulta('2025-07-24')).toBe('24/07/2025');
  });

  it.each([undefined, '', '24/07/2025', '2025-13-01', '2025-00-10', '2025-07-32', '2025-7-4'])(
    'devolve undefined para %s',
    (data) => {
      expect(formatarDataConsulta(data)).toBeUndefined();
    },
  );
});
