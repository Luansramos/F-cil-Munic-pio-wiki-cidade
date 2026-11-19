import { criarApiReal } from '../apiReal';

const fetchMock = jest.fn();

function resposta(corpo: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => corpo,
  } as Response;
}

const respostaValida = {
  resposta: 'texto',
  servicosRelacionados: ['cata-treco'],
  fontes: [{ titulo: 'Fonte', url: 'https://exemplo.invalid/a' }],
  informacaoSuficiente: true,
};

beforeEach(() => {
  fetchMock.mockReset();
  globalThis.fetch = fetchMock as unknown as typeof fetch;
});

describe('apiReal', () => {
  it('sem URL configurada, falha com erro de configuração e não chama a rede', async () => {
    const api = criarApiReal('');

    await expect(api.listarMunicipios()).rejects.toMatchObject({ tipo: 'configuracao' });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('lista municípios em GET /municipios, ignorando barra final da URL', async () => {
    fetchMock.mockResolvedValue(resposta([{ id: 'a', nome: 'A', uf: 'SP' }]));
    const api = criarApiReal('https://api.invalid/');

    const municipios = await api.listarMunicipios();

    expect(municipios).toEqual([{ id: 'a', nome: 'A', uf: 'SP' }]);
    expect(fetchMock.mock.calls[0][0]).toBe('https://api.invalid/municipios');
  });

  it('lista serviços em GET /municipios/{id}/servicos com o id codificado', async () => {
    fetchMock.mockResolvedValue(resposta([{ id: 's', nome: 'S' }]));
    const api = criarApiReal('https://api.invalid');

    await api.listarServicos('a b');

    expect(fetchMock.mock.calls[0][0]).toBe('https://api.invalid/municipios/a%20b/servicos');
  });

  it('envia a pergunta e o histórico em POST /municipios/{id}/conversas', async () => {
    fetchMock.mockResolvedValue(resposta(respostaValida));
    const api = criarApiReal('https://api.invalid');
    const envio = {
      pergunta: 'e se for domingo?',
      historico: [{ pergunta: 'antes', resposta: 'depois' }],
    };

    const resultado = await api.enviarPergunta('poa', envio);

    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe('https://api.invalid/municipios/poa/conversas');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body)).toEqual(envio);
    expect(resultado).toEqual(respostaValida);
  });

  it('classifica falha de rede como sem conexão', async () => {
    fetchMock.mockRejectedValue(new TypeError('Network request failed'));
    const api = criarApiReal('https://api.invalid');

    await expect(api.listarMunicipios()).rejects.toMatchObject({ tipo: 'sem-conexao' });
  });

  it('classifica status de erro como indisponível', async () => {
    fetchMock.mockResolvedValue(resposta({}, 500));
    const api = criarApiReal('https://api.invalid');

    await expect(api.listarMunicipios()).rejects.toMatchObject({ tipo: 'indisponivel' });
  });

  it('rejeita resposta de conversa em formato inesperado', async () => {
    fetchMock.mockResolvedValue(resposta({ resposta: 'sem os outros campos' }));
    const api = criarApiReal('https://api.invalid');

    await expect(api.enviarPergunta('poa', { pergunta: 'x' })).rejects.toMatchObject({
      tipo: 'indisponivel',
    });
  });

  it('rejeita lista de municípios em formato inesperado', async () => {
    fetchMock.mockResolvedValue(resposta([{ id: 1 }]));
    const api = criarApiReal('https://api.invalid');

    await expect(api.listarMunicipios()).rejects.toMatchObject({ tipo: 'indisponivel' });
  });

  it('rejeita corpo que não é JSON', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => {
        throw new SyntaxError('Unexpected token');
      },
    } as unknown as Response);
    const api = criarApiReal('https://api.invalid');

    await expect(api.listarMunicipios()).rejects.toMatchObject({ tipo: 'indisponivel' });
  });
});

describe('apiReal: erros por status', () => {
  it.each([
    [429, 'limite-de-uso'],
    [422, 'pergunta-invalida'],
    [503, 'indisponivel'],
    [404, 'indisponivel'],
  ])('status %s vira o tipo %s', async (status, tipo) => {
    fetchMock.mockResolvedValue(resposta({}, status));
    const api = criarApiReal('https://api.invalid');

    await expect(api.enviarPergunta('poa', { pergunta: 'x' })).rejects.toMatchObject({ tipo });
  });
});
