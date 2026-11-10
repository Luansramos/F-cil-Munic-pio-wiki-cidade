import { ErroApi } from '@/services/api/erros';
import { ErroBanco } from '@/services/banco/erros';

const MENSAGEM_PADRAO = 'Algo deu errado. Tente de novo.';

export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof ErroApi) {
    switch (erro.tipo) {
      case 'sem-conexao':
        return 'Sem conexão com a internet. Verifique sua conexão e tente de novo.';
      case 'indisponivel':
        return 'O serviço está indisponível agora. Tente de novo em instantes.';
      case 'limite-de-uso':
        return 'Você fez muitas perguntas em pouco tempo. Aguarde um minuto e tente de novo.';
      case 'pergunta-invalida':
        return 'Não foi possível enviar esta pergunta. Revise o texto e tente de novo.';
      case 'configuracao':
        return 'O aplicativo não está configurado para acessar o serviço.';
      default:
        return MENSAGEM_PADRAO;
    }
  }

  if (erro instanceof ErroBanco) {
    switch (erro.tipo) {
      case 'abertura':
        return 'Não foi possível abrir os dados salvos neste celular. Tente de novo.';
      case 'migracao':
        return 'Os dados salvos neste celular não podem ser usados por esta versão do aplicativo.';
      default:
        return MENSAGEM_PADRAO;
    }
  }

  return MENSAGEM_PADRAO;
}
