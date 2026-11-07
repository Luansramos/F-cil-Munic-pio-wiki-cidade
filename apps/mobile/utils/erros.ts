import { ErroApi } from '@/services/api/erros';

export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof ErroApi) {
    switch (erro.tipo) {
      case 'sem-conexao':
        return 'Sem conexão com a internet. Verifique sua conexão e tente de novo.';
      case 'indisponivel':
        return 'O serviço está indisponível agora. Tente de novo em instantes.';
      case 'configuracao':
        return 'O aplicativo não está configurado para acessar o serviço.';
      default:
        return 'Algo deu errado. Tente de novo.';
    }
  }
  return 'Algo deu errado. Tente de novo.';
}
