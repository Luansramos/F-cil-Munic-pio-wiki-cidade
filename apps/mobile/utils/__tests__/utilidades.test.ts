import { ErroApi } from '@/services/api/erros';

import { mensagemDeErro } from '../erros';
import { gerarId } from '../id';
import { formatarMunicipio } from '../municipio';
import { iconeDoServico, perguntaSugerida } from '../servicos';

describe('gerarId', () => {
  it('gera ids não vazios e distintos', () => {
    const ids = new Set(Array.from({ length: 1000 }, () => gerarId()));

    expect(ids.size).toBe(1000);
    expect([...ids].every((id) => id.length > 0)).toBe(true);
  });
});

describe('formatarMunicipio', () => {
  it('junta nome e UF', () => {
    expect(formatarMunicipio({ id: 'poa', nome: 'Poá', uf: 'SP' })).toBe('Poá · SP');
  });
});

describe('servicos', () => {
  it('monta a pergunta sugerida a partir do nome do serviço', () => {
    expect(perguntaSugerida({ id: 'cata-treco', nome: 'Cata Treco' })).toBe(
      'Quero saber sobre cata treco.',
    );
  });

  it('escolhe o ícone do serviço e usa um padrão para serviço desconhecido', () => {
    expect(iconeDoServico('cata-treco')).toBe('sofa');
    expect(iconeDoServico('coleta-de-lixo')).toBe('lixo');
    expect(iconeDoServico('iluminacao-publica')).toBe('lampada');
    expect(iconeDoServico('qualquer-outro')).toBe('conversa');
  });
});

describe('mensagemDeErro', () => {
  it('traduz cada tipo de erro da API em linguagem simples', () => {
    expect(mensagemDeErro(new ErroApi('sem-conexao', 'x'))).toContain('Sem conexão');
    expect(mensagemDeErro(new ErroApi('indisponivel', 'x'))).toContain('indisponível');
    expect(mensagemDeErro(new ErroApi('configuracao', 'x'))).toContain('configurado');
    expect(mensagemDeErro(new ErroApi('desconhecido', 'x'))).toContain('Algo deu errado');
  });

  it('usa a mensagem genérica para erros que não são da API', () => {
    expect(mensagemDeErro(new Error('qualquer'))).toContain('Algo deu errado');
    expect(mensagemDeErro('texto')).toContain('Algo deu errado');
  });

  it('não vaza a mensagem técnica do erro', () => {
    expect(mensagemDeErro(new ErroApi('indisponivel', 'status 500 interno'))).not.toContain('500');
  });
});
