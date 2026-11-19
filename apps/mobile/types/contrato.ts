export type Municipio = {
  id: string;
  nome: string;
  uf: string;
};

export type Servico = {
  id: string;
  nome: string;
};

export type Fonte = {
  titulo: string;
  url: string;
  dataConsulta?: string;
};

export type RespostaConversa = {
  resposta: string;
  servicosRelacionados: string[];
  fontes: Fonte[];
  informacaoSuficiente: boolean;
  canalOficialUrl?: string;
};

export type TrocaResumida = {
  pergunta: string;
  resposta: string;
};

export type EnvioPergunta = {
  pergunta: string;
  historico?: TrocaResumida[];
};
