import { criarFilaGravacoes } from '../fila';
import { inicializarBanco } from '../migracoes';
import { criarRepositorioPreferencias } from '../repositorioPreferencias';
import { criarBancoEmMemoria } from '../testes/bancoEmMemoria';

let banco: ReturnType<typeof criarBancoEmMemoria>;
let repositorio: ReturnType<typeof criarRepositorioPreferencias>;

beforeEach(async () => {
  banco = criarBancoEmMemoria();
  await inicializarBanco(banco.db);
  repositorio = criarRepositorioPreferencias(banco.db, criarFilaGravacoes());
});

afterEach(() => {
  banco.fechar();
});

describe('município escolhido', () => {
  it('é nulo enquanto nada foi salvo', async () => {
    expect(await repositorio.lerMunicipioId()).toBeNull();
  });

  it('guarda e devolve o município', async () => {
    await repositorio.salvarMunicipioId('exemplo');

    expect(await repositorio.lerMunicipioId()).toBe('exemplo');
  });

  it('salvar de novo sobrescreve sem duplicar a chave', async () => {
    await repositorio.salvarMunicipioId('exemplo');
    await repositorio.salvarMunicipioId('sao-paulo');

    expect(await repositorio.lerMunicipioId()).toBe('sao-paulo');
    const total = await banco.db.getFirstAsync<{ total: number }>(
      'SELECT COUNT(*) AS total FROM preferencias',
    );
    expect(total?.total).toBe(1);
  });
});

describe('nível do texto', () => {
  it('usa o padrão 0 enquanto nada foi salvo', async () => {
    expect(await repositorio.lerNivelTexto()).toBe(0);
  });

  it('guarda e devolve cada nível válido', async () => {
    for (const nivel of [0, 1, 2, 3] as const) {
      await repositorio.salvarNivelTexto(nivel);
      expect(await repositorio.lerNivelTexto()).toBe(nivel);
    }
  });

  it('volta ao padrão quando o valor no disco é inválido', async () => {
    for (const invalido of ['9', '-1', '1.5', 'grande', '']) {
      await banco.db.runAsync(
        'INSERT INTO preferencias (chave, valor) VALUES (?, ?) ON CONFLICT (chave) DO UPDATE SET valor = excluded.valor',
        ['nivel_texto', invalido],
      );
      expect(await repositorio.lerNivelTexto()).toBe(0);
    }
  });

  it('município e nível de texto convivem sem se afetar', async () => {
    await repositorio.salvarMunicipioId('exemplo');
    await repositorio.salvarNivelTexto(2);

    expect(await repositorio.lerMunicipioId()).toBe('exemplo');
    expect(await repositorio.lerNivelTexto()).toBe(2);
  });
});
