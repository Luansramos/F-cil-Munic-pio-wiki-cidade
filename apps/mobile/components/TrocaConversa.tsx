import { Linking, Pressable, StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { BolhaPergunta } from '@/components/BolhaPergunta';
import { Button } from '@/components/Button';
import { LinkExterno } from '@/components/LinkExterno';
import { Icon } from '@/components/Icon';
import { Notice } from '@/components/Notice';
import { Selo } from '@/components/Selo';
import { borderWidth, colors, radii, spacing } from '@/theme';
import type { Troca } from '@/types';
import { ehUrlSegura } from '@/utils/links';

type TrocaConversaProps = {
  troca: Troca;
  nomesDosServicos: string[];
  exemplo: boolean;
  aoVerFontes: () => void;
};

export function TrocaConversa({ troca, nomesDosServicos, exemplo, aoVerFontes }: TrocaConversaProps) {
  const primeiraFonte = troca.fontes.find((fonte) => ehUrlSegura(fonte.url));

  return (
    <View style={styles.troca}>
      <BolhaPergunta texto={troca.pergunta} />

      {troca.informacaoSuficiente ? (
        <>
          <View style={styles.resposta}>
            <AppText variant="body" selectable>
              {troca.resposta}
            </AppText>
          </View>

          {nomesDosServicos.length > 0 ? (
            <View style={styles.relacionados}>
              <AppText variant="small" color="textMuted">
                {nomesDosServicos.length === 1 ? 'Serviço relacionado:' : 'Serviços relacionados:'}
              </AppText>
              {nomesDosServicos.map((nome) => (
                <Selo key={nome} texto={nome} />
              ))}
            </View>
          ) : null}

          {primeiraFonte ? (
            <View style={styles.fonteDestaque}>
              <AppText variant="label">Fonte oficial</AppText>
              <LinkExterno
                rotulo="Consultar orientação"
                url={primeiraFonte.url}
                accessibilityLabel={`Consultar orientação em ${primeiraFonte.titulo}`}
              />
            </View>
          ) : null}

          {troca.fontes.length > 0 ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={`Ver fontes, ${troca.fontes.length}`}
              onPress={aoVerFontes}
              style={styles.verFontes}
            >
              <Icon name="documento" size="sm" color="primary" />
              <AppText variant="label" color="primary" style={styles.sublinhado}>
                Ver fontes ({troca.fontes.length})
              </AppText>
            </Pressable>
          ) : null}

          {exemplo ? <Notice mensagem="Exemplo de resposta para o protótipo." /> : null}
        </>
      ) : (
        <>
          <Notice
            tom="warning"
            titulo="Não encontrei informação suficiente"
            mensagem="A base deste município não tem dados confiáveis sobre isso. Para não passar uma orientação errada, consulte o canal oficial."
          />
          {ehUrlSegura(troca.canalOficialUrl) ? (
            <Button
              rotulo="Abrir canal oficial"
              icone="externo"
              larguraTotal={false}
              accessibilityHint="Abre o site do canal oficial no navegador"
              aoPressionar={() => {
                if (ehUrlSegura(troca.canalOficialUrl)) {
                  Linking.openURL(troca.canalOficialUrl).catch(() => undefined);
                }
              }}
            />
          ) : null}
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  troca: {
    gap: spacing.md,
  },
  resposta: {
    padding: spacing.lg,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.border,
    borderRadius: radii.xl,
  },
  relacionados: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: spacing.sm,
  },
  fonteDestaque: {
    gap: spacing.xs,
    padding: spacing.lg,
    backgroundColor: colors.surface,
    borderWidth: borderWidth.hairline,
    borderColor: colors.border,
    borderRadius: radii.xl,
  },
  verFontes: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: spacing.sm,
  },
  sublinhado: {
    textDecorationLine: 'underline',
  },
});
