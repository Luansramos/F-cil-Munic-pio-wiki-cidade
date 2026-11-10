export type TipoErroApi =
  | 'sem-conexao'
  | 'indisponivel'
  | 'limite-de-uso'
  | 'pergunta-invalida'
  | 'configuracao'
  | 'desconhecido';

export class ErroApi extends Error {
  readonly tipo: TipoErroApi;

  constructor(tipo: TipoErroApi, mensagem: string) {
    super(mensagem);
    this.name = 'ErroApi';
    this.tipo = tipo;
  }
}
