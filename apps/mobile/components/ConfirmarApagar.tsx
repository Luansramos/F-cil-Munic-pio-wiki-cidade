import { StyleSheet, View } from 'react-native';

import { AppText } from '@/components/AppText';
import { Button } from '@/components/Button';
import { FolhaModal } from '@/components/FolhaModal';
import { spacing } from '@/theme';

type ConfirmarApagarProps = {
  visivel: boolean;
  titulo: string;
  mensagem: string;
  rotuloConfirmar: string;
  ocupado?: boolean;
  aoConfirmar: () => void;
  aoCancelar: () => void;
};

export function ConfirmarApagar({
  visivel,
  titulo,
  mensagem,
  rotuloConfirmar,
  ocupado = false,
  aoConfirmar,
  aoCancelar,
}: ConfirmarApagarProps) {
  return (
    <FolhaModal visivel={visivel} titulo={titulo} aoFechar={aoCancelar}>
      <AppText variant="body">{mensagem}</AppText>
      <View style={styles.botoes}>
        <Button
          rotulo={rotuloConfirmar}
          variante="danger"
          icone="apagar"
          desativado={ocupado}
          aoPressionar={aoConfirmar}
        />
        <Button rotulo="Cancelar" variante="secondary" aoPressionar={aoCancelar} />
      </View>
    </FolhaModal>
  );
}

const styles = StyleSheet.create({
  botoes: {
    gap: spacing.md,
  },
});
