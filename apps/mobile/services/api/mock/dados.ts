import type { Municipio, Servico } from '@/types';

export const MARCA_EXEMPLO = '(Exemplo)';

export const DATA_EXEMPLO = '2026-11-07';

export type ServicoMock = Servico & {
  descricao: string;
  passos: string[];
  palavrasChave: string[];
};

export const MUNICIPIOS_MOCK: Municipio[] = [
  { id: 'poa', nome: 'Poá', uf: 'SP' },
  { id: 'exemplo', nome: 'Município Exemplo', uf: 'XX' },
];

export const SERVICOS_MOCK: ServicoMock[] = [
  {
    id: 'coleta-de-lixo',
    nome: 'Coleta de lixo',
    descricao: 'o serviço de coleta de lixo',
    passos: [
      'Confira o que é coletado.',
      'Consulte as orientações oficiais.',
      'Acesse o canal de atendimento.',
    ],
    palavrasChave: ['lixo', 'coleta', 'residuo', 'reciclavel', 'caminhao'],
  },
  {
    id: 'iluminacao-publica',
    nome: 'Iluminação pública',
    descricao: 'o serviço de iluminação pública',
    passos: [
      'Identifique o local do problema.',
      'Consulte as orientações oficiais.',
      'Acesse o canal de atendimento.',
    ],
    palavrasChave: ['iluminacao', 'luz', 'poste', 'lampada', 'escuro'],
  },
  {
    id: 'cata-treco',
    nome: 'Cata Treco',
    descricao: 'o serviço de retirada de grandes objetos',
    passos: [
      'Confira os itens aceitos.',
      'Consulte as orientações oficiais.',
      'Acesse o canal de atendimento.',
    ],
    palavrasChave: [
      'cata treco',
      'catatreco',
      'sofa',
      'movel',
      'moveis',
      'colchao',
      'geladeira',
      'eletrodomestico',
      'objeto grande',
      'objetos grandes',
    ],
  },
];

const SERVICOS_POR_MUNICIPIO: Record<string, string[]> = {
  poa: ['coleta-de-lixo', 'iluminacao-publica', 'cata-treco'],
  exemplo: ['coleta-de-lixo', 'iluminacao-publica'],
};

export const TERMOS_FORA_DE_COBERTURA = ['eletronico', 'pilha', 'bateria', 'oleo'];

export function buscarMunicipio(municipioId: string): Municipio | undefined {
  return MUNICIPIOS_MOCK.find((municipio) => municipio.id === municipioId);
}

export function servicosDoMunicipio(municipioId: string): ServicoMock[] {
  const ids = SERVICOS_POR_MUNICIPIO[municipioId] ?? [];
  return SERVICOS_MOCK.filter((servico) => ids.includes(servico.id));
}
