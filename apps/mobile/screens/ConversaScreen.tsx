import { useLocalSearchParams, useRouter } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import {
  AccessibilityInfo,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import type { ScrollViewInstance } from 'react-native';

import { BolhaPergunta } from '@/components/BolhaPergunta';
import { CabecalhoTela } from '@/components/CabecalhoTela';
import { CampoPergunta } from '@/components/CampoPergunta';
import { Carregando } from '@/components/Carregando';
import { ContainerTela } from '@/components/ContainerTela';
import { Notice } from '@/components/Notice';
import { PainelFontes } from '@/components/PainelFontes';
import { Selo } from '@/components/Selo';
import { TrocaConversa } from '@/components/TrocaConversa';
import { config } from '@/config';
import { useMunicipioAtual } from '@/contexts/MunicipioContext';
import { useConversa } from '@/hooks/useConversa';
import { useConexao } from '@/hooks/useConexao';
import { useServicos } from '@/hooks/useServicos';
import { colors, spacing } from '@/theme';
import type { Fonte } from '@/types';
import { mensagemDeErro } from '@/utils/erros';
import { formatarMunicipio } from '@/utils/municipio';

type ParametrosConversa = {
  id: string;
  municipioId?: string;
  perguntaSugerida?: string;
};

function primeiroValor(valor: string | string[] | undefined): string | undefined {
  return Array.isArray(valor) ? valor[0] : valor;
}

export function ConversaScreen() {
  const router = useRouter();
  const parametros = useLocalSearchParams<ParametrosConversa>();
  const conversaId = primeiroValor(parametros.id) ?? '';
  const municipioIdParam = primeiroValor(parametros.municipioId) ?? null;
  const perguntaSugerida = primeiroValor(parametros.perguntaSugerida) ?? '';

  const { estado } = useMunicipioAtual();
  const conversa = useConversa(conversaId, municipioIdParam);
  const servicos = useServicos(conversa.municipioId);
  const conectado = useConexao();

  const [texto, setTexto] = useState(perguntaSugerida);
  const [fontesAbertas, setFontesAbertas] = useState<Fonte[] | null>(null);
  const rolagem = useRef<ScrollViewInstance>(null);

  const municipio =
    estado.status === 'pronto'
      ? estado.dados.find((item) => item.id === conversa.municipioId)
      : undefined;

  const nomesPorId = new Map(
    servicos.estado.status === 'pronto'
      ? servicos.estado.dados.map((servico) => [servico.id, servico.nome] as const)
      : [],
  );

  const quantidadeDeTrocas = conversa.trocas.length;
  const primeiraCarga = useRef(true);

  useEffect(() => {
    if (primeiraCarga.current) {
      primeiraCarga.current = false;
      return;
    }
    if (quantidadeDeTrocas > 0) {
      AccessibilityInfo.announceForAccessibility('Resposta recebida');
    }
  }, [quantidadeDeTrocas]);

  function voltar() {
    if (router.canGoBack()) {
      router.back();
    } else {
      router.replace('/');
    }
  }

  async function enviar() {
    const enviada = await conversa.enviar(texto);
    if (enviada) {
      setTexto('');
    }
  }

  const semMunicipio = !conversa.carregando && conversa.municipioId === null;
  const enviando = conversa.pendente !== null;
  const semInternet = !config.usarMock && conectado === false;

  return (
    <ContainerTela bordas={['top', 'bottom']} semPadding>
      <CabecalhoTela
        titulo="Conversa"
        subtitulo={municipio ? formatarMunicipio(municipio) : undefined}
        aoVoltar={voltar}
        direita={config.usarMock ? <Selo texto="Demonstração" /> : undefined}
      />

      <KeyboardAvoidingView
        style={styles.fluxo}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <ScrollView
          ref={rolagem}
          keyboardShouldPersistTaps="handled"
          contentContainerStyle={styles.corpo}
          onContentSizeChange={() => rolagem.current?.scrollToEnd({ animated: true })}
        >
          {conversa.carregando ? <Carregando rotulo="Abrindo conversa" /> : null}

          {conversa.erroCarga !== null ? (
            <Notice
              tom="warning"
              titulo="Não foi possível abrir esta conversa"
              mensagem={mensagemDeErro(conversa.erroCarga)}
              acao={{ rotulo: 'Tentar novamente', aoPressionar: conversa.recarregar }}
            />
          ) : null}

          {semMunicipio && conversa.erroCarga === null ? (
            <Notice
              tom="warning"
              titulo="Município não identificado"
              mensagem="Volte ao Início, escolha um município e inicie a conversa de novo."
              acao={{ rotulo: 'Voltar ao Início', aoPressionar: voltar }}
            />
          ) : null}

          {conversa.trocas.map((troca) => (
            <TrocaConversa
              key={troca.id}
              troca={troca}
              exemplo={config.usarMock}
              nomesDosServicos={troca.servicosRelacionados
                .map((id) => nomesPorId.get(id))
                .filter((nome): nome is string => nome !== undefined)}
              aoVerFontes={() => setFontesAbertas(troca.fontes)}
            />
          ))}

          {conversa.pendente !== null ? (
            <View style={styles.pendente}>
              <BolhaPergunta texto={conversa.pendente} enviando />
              <Carregando rotulo="Buscando orientação" />
            </View>
          ) : null}

          {conversa.erroEnvio !== null ? (
            <Notice
              tom="warning"
              titulo="Não foi possível enviar a pergunta"
              mensagem={`${mensagemDeErro(conversa.erroEnvio)} Sua pergunta continua no campo abaixo.`}
            />
          ) : null}

          {conversa.falhaAoSalvar ? (
            <Notice
              tom="warning"
              titulo="Conversa não salva"
              mensagem="Não foi possível salvar esta conversa neste celular. Ela continua aberta, mas pode não aparecer no histórico."
            />
          ) : null}

          {semInternet ? (
            <Notice
              tom="tint"
              icone="semInternet"
              titulo="Sem internet"
              mensagem="Para enviar uma pergunta é preciso estar conectado. Você ainda pode ler esta conversa e o histórico."
            />
          ) : null}
        </ScrollView>

        <CampoPergunta
          valor={texto}
          aoMudar={setTexto}
          aoEnviar={enviar}
          desativado={conversa.carregando || semMunicipio || enviando || semInternet}
        />
      </KeyboardAvoidingView>

      <PainelFontes
        visivel={fontesAbertas !== null}
        fontes={fontesAbertas ?? []}
        aoFechar={() => setFontesAbertas(null)}
      />
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  fluxo: {
    flex: 1,
    backgroundColor: colors.background,
  },
  corpo: {
    flexGrow: 1,
    gap: spacing.xl,
    padding: spacing.lg,
  },
  pendente: {
    gap: spacing.sm,
  },
});
