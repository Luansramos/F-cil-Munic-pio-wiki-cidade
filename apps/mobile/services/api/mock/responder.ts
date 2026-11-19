import type { EnvioPergunta, Municipio, RespostaConversa, TrocaResumida } from '@/types';
import { normalizarTexto } from '@/utils/texto';

import {
  DATA_EXEMPLO,
  MARCA_EXEMPLO,
  SERVICOS_MOCK,
  TERMOS_FORA_DE_COBERTURA,
  servicosDoMunicipio,
} from './dados';
import type { ServicoMock } from './dados';

function encontrarServico(texto: string): ServicoMock | undefined {
  const normalizado = normalizarTexto(texto);
  return SERVICOS_MOCK.find((servico) =>
    servico.palavrasChave.some((palavra) => normalizado.includes(palavra)),
  );
}

function encontrarServicoNoHistorico(historico: TrocaResumida[]): ServicoMock | undefined {
  for (let indice = historico.length - 1; indice >= 0; indice -= 1) {
    const troca = historico[indice];
    const servico = encontrarServico(`${troca.pergunta} ${troca.resposta}`);
    if (servico) {
      return servico;
    }
  }
  return undefined;
}

function descreverContexto(quantidade: number): string {
  return quantidade === 1
    ? 'a troca anterior desta conversa'
    : `as ${quantidade} trocas anteriores desta conversa`;
}

function respostaInsuficiente(): RespostaConversa {
  return {
    resposta: `${MARCA_EXEMPLO} Não encontrei informação suficiente para responder. Para não passar uma orientação errada, consulte o canal oficial.`,
    servicosRelacionados: [],
    fontes: [],
    informacaoSuficiente: false,
  };
}

function respostaDoServico(
  municipio: Municipio,
  servico: ServicoMock,
  quantidadeContexto: number,
): RespostaConversa {
  const abertura =
    quantidadeContexto > 0
      ? `Considerando ${descreverContexto(quantidadeContexto)}, continuo ajudando com ${servico.descricao} em ${municipio.nome}.`
      : `Posso ajudar a encontrar ${servico.descricao} em ${municipio.nome}.`;
  const passos = servico.passos.map((passo, indice) => `${indice + 1}. ${passo}`);

  return {
    resposta: [`${MARCA_EXEMPLO} ${abertura}`, ...passos].join('\n'),
    servicosRelacionados: [servico.id],
    fontes: [
      {
        titulo: `Fonte de exemplo: ${servico.nome} (${municipio.nome})`,
        url: '',
        dataConsulta: DATA_EXEMPLO,
      },
      {
        titulo: `Outra fonte de exemplo: ${servico.nome}`,
        url: '',
      },
    ],
    informacaoSuficiente: true,
  };
}

export function responderMock(municipio: Municipio, envio: EnvioPergunta): RespostaConversa {
  const historico = envio.historico ?? [];
  const normalizada = normalizarTexto(envio.pergunta);

  if (TERMOS_FORA_DE_COBERTURA.some((termo) => normalizada.includes(termo))) {
    return respostaInsuficiente();
  }

  const servico = encontrarServico(envio.pergunta) ?? encontrarServicoNoHistorico(historico);
  if (!servico) {
    return respostaInsuficiente();
  }

  const oferecido = servicosDoMunicipio(municipio.id).some((item) => item.id === servico.id);
  if (!oferecido) {
    return respostaInsuficiente();
  }

  return respostaDoServico(municipio, servico, historico.length);
}
