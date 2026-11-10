import { useState } from 'react';
import { AccessibilityInfo, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Button } from '@/components/Button';
import { ConfirmarApagar } from '@/components/ConfirmarApagar';
import { ContainerTela } from '@/components/ContainerTela';
import { FolhaModal } from '@/components/FolhaModal';
import { Icon } from '@/components/Icon';
import { ListRow } from '@/components/ListRow';
import { Notice } from '@/components/Notice';
import { SectionTitle } from '@/components/SectionTitle';
import { Selo } from '@/components/Selo';
import { config } from '@/config';
import { TROCAS_NO_CONTEXTO } from '@/config/limites';
import { usePreferencias } from '@/contexts/PreferenciasContext';
import { useApagarHistorico } from '@/hooks/useApagarHistorico';
import { borderWidth, colors, radii, spacing } from '@/theme';
import { mensagemDeErro } from '@/utils/erros';

export function PreferenciasScreen() {
  const { nivelTexto, quantidadeDeNiveis, podeAumentar, podeDiminuir, aumentarTexto, diminuirTexto } =
    usePreferencias();
  const apagarHistorico = useApagarHistorico();

  const [privacidadeAberta, setPrivacidadeAberta] = useState(false);
  const [confirmando, setConfirmando] = useState(false);

  async function confirmarApagar() {
    const apagou = await apagarHistorico.apagar();
    setConfirmando(false);
    if (apagou) {
      AccessibilityInfo.announceForAccessibility('Histórico apagado');
    }
  }

  return (
    <ContainerTela rolavel>
      <View style={styles.pagina}>
        <AppText variant="title" accessibilityRole="header">
          Preferências
        </AppText>

        <View style={styles.secao}>
          <SectionTitle titulo="Acessibilidade" faixa />
          <View style={styles.linhaTamanho}>
            <View style={styles.textoTamanho}>
              <AppText variant="label">Tamanho do texto</AppText>
              <AppText variant="small" color="textMuted" accessibilityLiveRegion="polite">
                Tamanho {nivelTexto + 1} de {quantidadeDeNiveis}
              </AppText>
            </View>
            <View style={styles.botoesTamanho}>
              <Button
                rotulo="A−"
                accessibilityLabel="Diminuir o tamanho do texto"
                variante="secondary"
                larguraTotal={false}
                desativado={!podeDiminuir}
                aoPressionar={diminuirTexto}
              />
              <Button
                rotulo="A+"
                accessibilityLabel="Aumentar o tamanho do texto"
                variante="secondary"
                larguraTotal={false}
                desativado={!podeAumentar}
                aoPressionar={aumentarTexto}
              />
            </View>
          </View>
          <View style={styles.exemplo}>
            <AppText variant="body">Orientações fáceis de ler.</AppText>
          </View>
        </View>

        <View style={styles.secao}>
          <SectionTitle titulo="Privacidade" faixa />
          <ListRow
            icone="privacidade"
            tom="surface"
            titulo="Como usamos seus dados"
            accessibilityHint="Abre uma explicação sobre os dados"
            aoPressionar={() => setPrivacidadeAberta(true)}
          />
          <Button
            rotulo="Apagar todo o histórico"
            variante="danger"
            icone="apagar"
            aoPressionar={() => {
              apagarHistorico.limpar();
              setConfirmando(true);
            }}
          />
          <AppText variant="small" color="textMuted">
            Antes de apagar, o app pede uma confirmação.
          </AppText>
          {apagarHistorico.concluido ? (
            <Notice tom="tint" icone="check" mensagem="Histórico apagado deste celular." />
          ) : null}
          {apagarHistorico.erro !== null ? (
            <Notice
              tom="warning"
              titulo="Não foi possível apagar o histórico"
              mensagem={mensagemDeErro(apagarHistorico.erro)}
            />
          ) : null}
        </View>

        <View style={styles.secao}>
          <SectionTitle titulo="Sobre o aplicativo" faixa />
          <View style={styles.linhaDemo}>
            <Icon name="info" color="primary" />
            <AppText variant="label" style={styles.textoDemo}>
              Modo demonstrativo
            </AppText>
            <Selo texto={config.usarMock ? 'Ativo' : 'Desativado'} />
          </View>
          <Notice
            mensagem={
              config.usarMock
                ? 'Protótipo acadêmico, sem parceria com nenhuma prefeitura. No modo demonstrativo, as respostas são exemplos e não vêm de fontes oficiais.'
                : 'Protótipo acadêmico, sem parceria com nenhuma prefeitura. As respostas vêm da API configurada.'
            }
          />
          <Notice mensagem="Este aplicativo orienta. A solicitação é feita no canal oficial." />
        </View>
      </View>

      <FolhaModal
        visivel={privacidadeAberta}
        titulo="Como usamos seus dados"
        aoFechar={() => setPrivacidadeAberta(false)}
      >
        <AppText variant="body">
          Suas conversas e preferências ficam salvas só neste celular. O app não tem conta nem login.
        </AppText>
        <AppText variant="body">
          {config.usarMock
            ? 'No modo demonstrativo, nada sai do celular: as respostas são exemplos criados no próprio app.'
            : `Para responder, o app envia sua pergunta e as últimas ${TROCAS_NO_CONTEXTO} trocas da conversa ao serviço configurado. Neste protótipo, o tratamento desses dados no servidor ainda não foi definido.`}
        </AppText>
        <AppText variant="body">Não digite CPF, senhas ou dados de saúde.</AppText>
        <AppText variant="body">
          Os dados salvos não são criptografados pelo app. A proteção vem do isolamento do aplicativo
          pelo sistema do celular. Ao apagar, o app sobrescreve o espaço no arquivo, o que não garante
          que os dados sejam irrecuperáveis.
        </AppText>
        <AppText variant="body">
          Você pode apagar uma conversa no Histórico ou todo o histórico nesta tela.
        </AppText>
      </FolhaModal>

      <ConfirmarApagar
        visivel={confirmando}
        titulo="Apagar todo o histórico?"
        mensagem="Todas as conversas salvas neste celular serão apagadas. Suas preferências, como o tamanho do texto e o município, continuam. Isso não pode ser desfeito."
        rotuloConfirmar="Apagar tudo"
        ocupado={apagarHistorico.apagando}
        aoConfirmar={confirmarApagar}
        aoCancelar={() => setConfirmando(false)}
      />
    </ContainerTela>
  );
}

const styles = StyleSheet.create({
  pagina: {
    gap: spacing.xl,
  },
  secao: {
    gap: spacing.md,
  },
  linhaTamanho: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.md,
  },
  textoTamanho: {
    flexGrow: 1,
    flexShrink: 1,
    flexBasis: 160,
  },
  botoesTamanho: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  exemplo: {
    padding: spacing.lg,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.border,
    borderRadius: radii.lg,
  },
  linhaDemo: {
    minHeight: 56,
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: spacing.md,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.border,
    borderRadius: radii.lg,
  },
  textoDemo: {
    flexGrow: 1,
    flexShrink: 1,
  },
});
