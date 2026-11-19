import { mensagemIndisponivel } from '../resposta';

describe('mensagemIndisponivel', () => {
  it('mostra a explicação de bloqueio de privacidade retornada pela API', () => {
    const mensagem = 'Não foi possível exibir esta resposta com segurança.';
    expect(mensagemIndisponivel(mensagem)).toBe(mensagem);
  });

  it('mostra a explicação de informação insuficiente retornada pela API', () => {
    const mensagem = 'Não encontrei informação suficiente na base publicada deste município.';
    expect(mensagemIndisponivel(mensagem)).toBe(mensagem);
  });

  it('oferece uma mensagem genérica quando a resposta vier vazia', () => {
    expect(mensagemIndisponivel('  ')).toBe('Não foi possível apresentar uma orientação. Consulte o canal oficial.');
  });
});
