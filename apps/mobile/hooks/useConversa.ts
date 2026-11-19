import { useCallback, useEffect, useRef, useState } from 'react';

import { PERGUNTA_MAX } from '@/config/limites';
import { useRepositorios } from '@/contexts/BancoContext';
import { api } from '@/services/api';
import type { Troca } from '@/types';
import { montarHistorico } from '@/utils/historico';
import { gerarId } from '@/utils/id';

export function useConversa(conversaId: string, municipioIdInicial: string | null) {
  const { conversas } = useRepositorios();
  const [carregando, setCarregando] = useState(true);
  const [erroCarga, setErroCarga] = useState<unknown>(null);
  const [tentativa, setTentativa] = useState(0);
  const [municipioId, setMunicipioId] = useState<string | null>(municipioIdInicial);
  const [trocas, setTrocas] = useState<Troca[]>([]);
  const [pendente, setPendente] = useState<string | null>(null);
  const [erroEnvio, setErroEnvio] = useState<unknown>(null);
  const [falhaAoSalvar, setFalhaAoSalvar] = useState(false);

  const trocasRef = useRef<Troca[]>([]);
  const naoSalvasRef = useRef<Troca[]>([]);
  const enviandoRef = useRef(false);
  const ativoRef = useRef(true);

  useEffect(() => {
    ativoRef.current = true;
    return () => {
      ativoRef.current = false;
    };
  }, []);

  useEffect(() => {
    let ativo = true;
    setCarregando(true);
    setErroCarga(null);

    conversas
      .buscar(conversaId)
      .then((conversa) => {
        if (!ativo) {
          return;
        }
        if (conversa !== null) {
          trocasRef.current = conversa.trocas;
          setTrocas(conversa.trocas);
          setMunicipioId(conversa.municipioId);
        }
        setCarregando(false);
      })
      .catch((erro: unknown) => {
        if (ativo) {
          setErroCarga(erro);
          setCarregando(false);
        }
      });

    return () => {
      ativo = false;
    };
  }, [conversas, conversaId, tentativa]);

  const recarregar = useCallback(() => setTentativa((valor) => valor + 1), []);

  const gravarPendentes = useCallback(
    async (idDaConversa: string, idDoMunicipio: string): Promise<boolean> => {
      while (naoSalvasRef.current.length > 0) {
        const proxima = naoSalvasRef.current[0];
        try {
          await conversas.salvarTroca(idDaConversa, idDoMunicipio, proxima);
        } catch {
          return false;
        }
        naoSalvasRef.current = naoSalvasRef.current.slice(1);
      }
      return true;
    },
    [conversas],
  );

  const enviar = useCallback(
    async (texto: string): Promise<boolean> => {
      const pergunta = texto.trim();
      if (
        enviandoRef.current ||
        municipioId === null ||
        pergunta === '' ||
        pergunta.length > PERGUNTA_MAX
      ) {
        return false;
      }

      enviandoRef.current = true;
      setErroEnvio(null);
      setFalhaAoSalvar(false);
      setPendente(pergunta);

      try {
        const historico = montarHistorico(trocasRef.current);
        const resposta = await api.enviarPergunta(
          municipioId,
          historico.length > 0 ? { pergunta, historico } : { pergunta },
        );

        const troca: Troca = {
          ...resposta,
          id: gerarId(),
          pergunta,
          criadoEm: new Date().toISOString(),
        };
        trocasRef.current = [...trocasRef.current, troca];
        naoSalvasRef.current = [...naoSalvasRef.current, troca];

        if (ativoRef.current) {
          setTrocas(trocasRef.current);
          setPendente(null);
        }

        const salvou = await gravarPendentes(conversaId, municipioId);
        if (!salvou && ativoRef.current) {
          setFalhaAoSalvar(true);
        }
        return true;
      } catch (erro) {
        if (ativoRef.current) {
          setErroEnvio(erro);
          setPendente(null);
        }
        return false;
      } finally {
        enviandoRef.current = false;
      }
    },
    [conversaId, gravarPendentes, municipioId],
  );

  return {
    carregando,
    erroCarga,
    recarregar,
    municipioId,
    trocas,
    pendente,
    erroEnvio,
    falhaAoSalvar,
    enviar,
  };
}
