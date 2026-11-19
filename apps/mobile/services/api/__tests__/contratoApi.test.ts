import { criarApiReal } from '../apiReal';

const fetchMock = jest.fn();

function resposta(corpo: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => corpo,
  } as Response;
}

beforeEach(() => {
  fetchMock.mockReset();
  globalThis.fetch = fetchMock as unknown as typeof fetch;
});

describe('exemplos do contrato da API (docs/contratos-de-integracao.md, branch feat/soa-csharp-api)', () => {
  const api = criarApiReal('http://localhost:5055/api/v1');

  it('aceita a lista de municípios no formato documentado', async () => {
    fetchMock.mockResolvedValue(resposta([{ id: 'poa', nome: 'Poá', uf: 'SP' }]));

    await expect(api.listarMunicipios()).resolves.toEqual([
      { id: 'poa', nome: 'Poá', uf: 'SP' },
    ]);
  });

  it('aceita a lista de serviços no formato documentado e a lista vazia de município sem publicação', async () => {
    fetchMock.mockResolvedValueOnce(resposta([{ id: 'cata-treco', nome: 'Cata Treco' }]));
    fetchMock.mockResolvedValueOnce(resposta([]));

    await expect(api.listarServicos('poa')).resolves.toEqual([
      { id: 'cata-treco', nome: 'Cata Treco' },
    ]);
    await expect(api.listarServicos('sem-publicacao')).resolves.toEqual([]);
  });

  it('aceita a resposta suficiente, com fonte https e dataConsulta, sem canalOficialUrl', async () => {
    const corpo = {
      resposta:
        'Cata Treco: orientações para retirada de móveis.\n\nEtapas:\n1. Consulte o canal oficial.',
      servicosRelacionados: ['cata-treco'],
      fontes: [
        {
          titulo: 'Fonte de exemplo',
          url: 'https://example.org/servico',
          dataConsulta: '2026-11-10',
        },
      ],
      informacaoSuficiente: true,
    };
    fetchMock.mockResolvedValue(resposta(corpo));

    await expect(
      api.enviarPergunta('poa', { pergunta: 'Como solicito a retirada de um sofá?' }),
    ).resolves.toEqual(corpo);
  });

  it('aceita a resposta de informação insuficiente com listas vazias e sem campos opcionais', async () => {
    const corpo = {
      resposta:
        'Não encontrei informação suficiente na base publicada deste município. Consulte o canal oficial para confirmar a orientação.',
      servicosRelacionados: [],
      fontes: [],
      informacaoSuficiente: false,
    };
    fetchMock.mockResolvedValue(resposta(corpo));

    await expect(api.enviarPergunta('poa', { pergunta: 'E sobre a CNH?' })).resolves.toEqual(
      corpo,
    );
  });

  it('aceita canalOficialUrl quando a API o envia como texto', async () => {
    const corpo = {
      resposta: 'Sem informação.',
      servicosRelacionados: [],
      fontes: [],
      informacaoSuficiente: false,
      canalOficialUrl: 'https://example.org/atendimento',
    };
    fetchMock.mockResolvedValue(resposta(corpo));

    const recebida = await api.enviarPergunta('poa', { pergunta: 'x' });

    expect(recebida.canalOficialUrl).toBe('https://example.org/atendimento');
  });

  it('rejeita null em campos opcionais, que o contrato diz que a API nunca envia', async () => {
    fetchMock.mockResolvedValue(
      resposta({
        resposta: 'x',
        servicosRelacionados: [],
        fontes: [{ titulo: 'F', url: 'https://example.org', dataConsulta: null }],
        informacaoSuficiente: true,
      }),
    );

    await expect(api.enviarPergunta('poa', { pergunta: 'x' })).rejects.toMatchObject({
      tipo: 'indisponivel',
    });
  });

  it('envia o corpo documentado, com histórico, sem identificador de usuário ou aparelho', async () => {
    fetchMock.mockResolvedValue(
      resposta({
        resposta: 'x',
        servicosRelacionados: [],
        fontes: [],
        informacaoSuficiente: false,
      }),
    );
    const envio = {
      pergunta: 'E no domingo?',
      historico: [
        { pergunta: 'Como solicito a retirada de um sofá?', resposta: 'Orientação anterior.' },
      ],
    };

    await api.enviarPergunta('poa', envio);

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('http://localhost:5055/api/v1/municipios/poa/conversas');
    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toEqual(envio);
    expect(Object.keys(init.headers as Record<string, string>).sort()).toEqual([
      'Accept',
      'Content-Type',
    ]);
  });

  it.each([
    [404, 'indisponivel'],
    [422, 'pergunta-invalida'],
    [429, 'limite-de-uso'],
    [503, 'indisponivel'],
  ])('traduz o status HTTP %i da tabela de erros do contrato em %s', async (status, tipo) => {
    fetchMock.mockResolvedValue(resposta({}, status));

    await expect(api.enviarPergunta('poa', { pergunta: 'x' })).rejects.toMatchObject({ tipo });
  });
});
