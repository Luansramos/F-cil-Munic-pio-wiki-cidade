# Physical Computing — Totem Inteligente de Atendimento (IoT)

**Projeto:** Fácil Município — Wiki Cidade (Global Solution 2026 — FIAP)  
**Disciplina:** Physical Computing — IoT & IoB  
**Responsável pela frente IoT:** Fernanda Rocha Menon — RM 554673  
**Backlog:** G5, G6 e G7  
**Simulação pública:** https://wokwi.com/projects/480983207617445889

## 1. Contexto e objetivo

O Fácil Município — Wiki Cidade orienta moradores na consulta de informações e fontes sobre serviços municipais, com piloto em Poá (SP). O totem de atendimento é uma **extensão demonstrativa de IoT**, voltada à representação do fluxo de cidadãos em uma unidade pública. Não faz parte do fluxo principal já implementado de consulta de serviços do aplicativo.

O protótipo usa um ESP32 simulado no Wokwi para registrar entradas e atendimentos, exibir a quantidade de pessoas aguardando e sinalizar diferentes níveis de ocupação.

## 2. Componentes e conexões

| Componente | Conexão / função |
|---|---|
| ESP32 | Microcontrolador responsável pela lógica |
| Botão de entrada | GPIO 18 — incrementa a fila |
| Botão de atendimento | GPIO 19 — decrementa a fila sem ficar negativa |
| LED verde | GPIO 25 — fila tranquila |
| LED amarelo | GPIO 26 — fila moderada |
| LED vermelho | GPIO 27 — fila crítica |
| Buzzer | GPIO 23 — alerta ao atingir o limite crítico |
| OLED I2C 128×64 | SDA GPIO 21, SCL GPIO 22, endereço 0x3C — contador e status |

## 3. Regras de funcionamento

- Inicialização: contador de pessoas em zero.
- Botão de entrada: acrescenta uma pessoa.
- Botão de atendimento: retira uma pessoa, respeitando o mínimo de zero.
- O display OLED e os LEDs são atualizados conforme a contagem.
- Os limites são **parâmetros acadêmicos de demonstração**, não normas oficiais de atendimento.

| Pessoas aguardando | Status | Indicador |
|---|---|---|
| 0 a 4 | Fila tranquila | Verde |
| 5 a 9 | Fila moderada | Amarelo |
| 10 ou mais | Fila crítica | Vermelho |
 
O código prevê aviso sonoro ao alcançar o limiar crítico de 10 pessoas.

### Fluxo local

```text
Botões de entrada/atendimento
            ↓
        ESP32 (contador)
            ↓
   Classificação da ocupação
            ↓
       OLED + LEDs
            ↓
   Buzzer no limiar crítico
```

## 4. Relação com o produto e integração (G6)

O aplicativo Fácil Município permite ao cidadão encontrar orientações sobre serviços municipais. O totem demonstra uma possível etapa complementar de **acompanhamento de atendimento presencial**. O usuário poderia consultar um serviço pelo aplicativo e, em uma evolução futura, uma unidade municipal poderia monitorar sua demanda por meio de dispositivos IoT.

**Estado real:** a demonstração funcional é **local no Wokwi**. O código possui possibilidade de envio de dados por Wi-Fi/HTTP, mas a URL da API não está configurada e **não houve comunicação ponta a ponta validada com a aplicação principal**. O contrato existente em `docs/contratos-de-integracao.md` não define uma rota para telemetria de filas. Não há persistência remota, painel de filas nem visualização dessa informação no Expo comprovados.

Qualquer integração futura exige alinhamento com a equipe da API para definição de endpoint, estrutura de dados, isolamento municipal, autenticação apropriada, tratamento de falhas e testes. Nenhuma rota nova é declarada como existente nesta documentação.

## 5. Verificações e evidências (G5)

Testes manuais relatados na simulação Wokwi:

| Cenário | Resultado observado |
|---|---|
| Inicialização | OLED exibe contador em 0 e estado tranquilo |
| Pressionar entrada | Contador incrementa |
| Atingir 10 pessoas | OLED exibe estado `FILA CRITICA` |
| Pressionar atendimento | Contador decrementa |
| Retornar a zero | Contador não fica negativo |
| Comunicação HTTP | Monitor serial indica modo local / API não configurada |

**Verificações adicionais antes da entrega:** confirmar em demonstração o acionamento visual do LED vermelho, o alerta do buzzer e a transição de estados em 4/5 e 9/10. Esses comportamentos constam da lógica proposta, mas não devem ser declarados testados sem evidência.

**Ambiente:** simulador Wokwi; não houve teste em hardware físico nem teste integrado com backend.

**Link da simulação:** https://wokwi.com/projects/480983207617445889

## 6. Entregáveis (G7)

- [x] Circuito ESP32 funcional no Wokwi, relacionado à proposta do grupo
- [x] Link público do Wokwi
- [x] Documentação técnica e limitações da integração
- [x] Lista de integrantes em `docs/iot/integrantes.txt`
- [x] Gravação e publicação de vídeo demonstrativo de até 4 minutos
- [x] Inserção do link do vídeo neste documento
- [ ] Revisão da equipe e conferência da submissão no Teams / Portal do Aluno

**Vídeo demonstrativo (YouTube):** https://youtu.be/x3IEq_ttazc

### Roteiro sugerido para o vídeo (até 4 minutos)

1. Apresentar o Fácil Município e o papel complementar do totem.
2. Mostrar ESP32, dois botões, OLED, LEDs e buzzer.
3. Iniciar simulação com zero pessoas e demonstrar entradas.
4. Mostrar os estados tranquilo, moderado e crítico, inclusive o alerta.
5. Simular atendimentos e demonstrar o retorno a zero.
6. Explicar que o circuito opera localmente e a API ainda não está integrada.

## 7. Limitações e melhorias possíveis

A contagem depende de acionamento manual, não há sensores físicos de presença, armazenamento remoto ou telemetria integrada ao aplicativo. Os limiares de ocupação são demonstrativos.

Como evolução, a equipe poderia definir uma API de telemetria, adicionar persistência e painel de monitoramento, utilizar sensores automáticos e avaliar dados agregados de atendimento, respeitando os limites de privacidade e a arquitetura municipal.

## 8. Integrantes

- Fernanda Rocha Menon — RM 554673
- Luiza Macena Dantas — RM 556237
- Luan Ramos Garcia de Souza — RM 558537
- Matheus Ricciotti — RM 556930
- Matheus Bortolotto — RM 555189

## 9. Referências internas

- [Backlog G5–G7](../../planejamento/04-backlog.md)
- [Entregas por disciplina](../../planejamento/06-entregas-por-disciplina.md)
- [Especificação do MVP](../../planejamento/02-especificacao-mvp.md)
- [Contratos de integração](../contratos-de-integracao.md)
- [Registro da feature de infraestrutura e IoT](../features/infraestrutura-e-iot.md)
