import { tituloDaConversa } from '../titulo';

describe('tituloDaConversa', () => {
  it('mantém uma pergunta curta como está', () => {
    expect(tituloDaConversa('Como solicito a retirada de um sofá?')).toBe(
      'Como solicito a retirada de um sofá?',
    );
  });

  it('colapsa espaços e quebras de linha', () => {
    expect(tituloDaConversa('  retirada \n  de   sofá ')).toBe('retirada de sofá');
  });

  it('corta uma pergunta longa em 60 caracteres, terminando com reticências', () => {
    const titulo = tituloDaConversa('palavra '.repeat(30));

    expect(titulo.length).toBeLessThanOrEqual(60);
    expect(titulo.endsWith('…')).toBe(true);
  });

  it('não corta quando a pergunta tem exatamente 60 caracteres', () => {
    const exata = 'a'.repeat(60);

    expect(tituloDaConversa(exata)).toBe(exata);
  });
});
