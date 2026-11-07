import { criarApiMock } from '../apiMock';
import { MARCA_EXEMPLO } from '../mock/dados';

const api = criarApiMock();

describe('apiMock', () => {
  describe('municípios e serviços', () => {
    it('lista os dois municípios do mock', async () => {
      const municipios = await api.listarMunicipios();

      expect(municipios.map((municipio) => municipio.id)).toEqual(['poa', 'exemplo']);
    });

    it('lista os três serviços do piloto em Poá', async () => {
      const servicos = await api.listarServicos('poa');

      expect(servicos).toEqual([
        { id: 'coleta-de-lixo', nome: 'Coleta de lixo' },
        { id: 'iluminacao-publica', nome: 'Iluminação pública' },
        { id: 'cata-treco', nome: 'Cata Treco' },
      ]);
    });

    it('não lista no município de exemplo um serviço que ele não oferece', async () => {
      const servicos = await api.listarServicos('exemplo');

      expect(servicos.map((servico) => servico.id)).toEqual([
        'coleta-de-lixo',
        'iluminacao-publica',
      ]);
    });

    it('rejeita município desconhecido', async () => {
      await expect(api.listarServicos('inexistente')).rejects.toMatchObject({
        tipo: 'desconhecido',
      });
      await expect(api.enviarPergunta('inexistente', { pergunta: 'lixo' })).rejects.toMatchObject({
        tipo: 'desconhecido',
      });
    });

    it('não deixa o chamador alterar o catálogo do mock', async () => {
      const primeira = await api.listarMunicipios();
      primeira[0].nome = 'Alterado';

      const segunda = await api.listarMunicipios();

      expect(segunda[0].nome).toBe('Poá');
    });
  });

  describe('resposta com informação suficiente', () => {
    it('responde sobre o Cata Treco com texto marcado como exemplo e lista numerada', async () => {
      const resposta = await api.enviarPergunta('poa', {
        pergunta: 'Como solicito a retirada de um sofá?',
      });

      expect(resposta.informacaoSuficiente).toBe(true);
      expect(resposta.servicosRelacionados).toEqual(['cata-treco']);
      expect(resposta.resposta.startsWith(MARCA_EXEMPLO)).toBe(true);
      expect(resposta.resposta).toContain('\n1. ');
      expect(resposta.resposta).toContain('\n3. ');
    });

    it('cobre os três serviços do piloto', async () => {
      const perguntas: [string, string][] = [
        ['Quando passa o caminhão de lixo?', 'coleta-de-lixo'],
        ['O poste da minha rua está sem luz', 'iluminacao-publica'],
        ['Posso descartar um colchão?', 'cata-treco'],
      ];

      for (const [pergunta, servicoId] of perguntas) {
        const resposta = await api.enviarPergunta('poa', { pergunta });
        expect(resposta.servicosRelacionados).toEqual([servicoId]);
      }
    });

    it('não inventa links: nenhuma fonte tem URL e não há canal oficial', async () => {
      const resposta = await api.enviarPergunta('poa', { pergunta: 'retirada de sofá' });

      expect(resposta.fontes.length).toBeGreaterThan(0);
      expect(resposta.fontes.every((fonte) => fonte.url === '')).toBe(true);
      expect(resposta.canalOficialUrl).toBeUndefined();
    });

    it('devolve uma fonte com data e outra sem data', async () => {
      const resposta = await api.enviarPergunta('poa', { pergunta: 'retirada de sofá' });

      expect(resposta.fontes[0].dataConsulta).toBeDefined();
      expect(resposta.fontes[1].dataConsulta).toBeUndefined();
    });
  });

  describe('informação insuficiente', () => {
    it('informa insuficiência em tema fora da cobertura, sem fontes nem serviços', async () => {
      const resposta = await api.enviarPergunta('poa', {
        pergunta: 'Posso levar o lixo eletrônico no sábado?',
      });

      expect(resposta.informacaoSuficiente).toBe(false);
      expect(resposta.fontes).toEqual([]);
      expect(resposta.servicosRelacionados).toEqual([]);
      expect(resposta.resposta).toContain('Não encontrei informação suficiente');
    });

    it('informa insuficiência quando a pergunta não casa com nenhum serviço', async () => {
      const resposta = await api.enviarPergunta('poa', { pergunta: 'Qual a capital da França?' });

      expect(resposta.informacaoSuficiente).toBe(false);
    });

    it('isola municípios: serviço de Poá não responde no município de exemplo', async () => {
      const emPoa = await api.enviarPergunta('poa', { pergunta: 'retirada de sofá' });
      const emExemplo = await api.enviarPergunta('exemplo', { pergunta: 'retirada de sofá' });

      expect(emPoa.informacaoSuficiente).toBe(true);
      expect(emExemplo.informacaoSuficiente).toBe(false);
      expect(emExemplo.fontes).toEqual([]);
    });
  });

  describe('continuidade da conversa', () => {
    const umaTroca = [
      { pergunta: 'Como solicito a retirada de um sofá?', resposta: `${MARCA_EXEMPLO} Cata Treco` },
    ];

    it('sem histórico, uma pergunta de seguimento não tem contexto', async () => {
      const resposta = await api.enviarPergunta('poa', { pergunta: 'e se for domingo?' });

      expect(resposta.informacaoSuficiente).toBe(false);
    });

    it('com histórico, interpreta a pergunta de seguimento pelo serviço anterior', async () => {
      const resposta = await api.enviarPergunta('poa', {
        pergunta: 'e se for domingo?',
        historico: umaTroca,
      });

      expect(resposta.informacaoSuficiente).toBe(true);
      expect(resposta.servicosRelacionados).toEqual(['cata-treco']);
      expect(resposta.resposta).toContain('a troca anterior desta conversa');
    });

    it('varia a resposta quando o histórico vem preenchido', async () => {
      const semContexto = await api.enviarPergunta('poa', { pergunta: 'retirada de sofá' });
      const comContexto = await api.enviarPergunta('poa', {
        pergunta: 'retirada de sofá',
        historico: umaTroca,
      });

      expect(comContexto.resposta).not.toBe(semContexto.resposta);
    });

    it('descreve a quantidade de trocas de contexto', async () => {
      const tres = [...umaTroca, ...umaTroca, ...umaTroca];

      const resposta = await api.enviarPergunta('poa', {
        pergunta: 'e se for domingo?',
        historico: tres,
      });

      expect(resposta.resposta).toContain('as 3 trocas anteriores desta conversa');
    });

    it('não usa o contexto de outro município', async () => {
      const resposta = await api.enviarPergunta('exemplo', {
        pergunta: 'e se for domingo?',
        historico: umaTroca,
      });

      expect(resposta.informacaoSuficiente).toBe(false);
    });
  });

  describe('validação', () => {
    it('rejeita pergunta vazia', async () => {
      await expect(api.enviarPergunta('poa', { pergunta: '   ' })).rejects.toMatchObject({
        tipo: 'desconhecido',
      });
    });
  });
});
