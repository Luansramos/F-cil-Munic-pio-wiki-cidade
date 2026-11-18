# Physical Computing — Totem Inteligente de Atendimento (IoT)

**Global Solution 2026 | FIAP — Engenharia de Software**

## Visão geral

O **Totem Inteligente de Atendimento** é um protótipo de Internet das Coisas (IoT) desenvolvido como parte do projeto **Fácil Município — Wiki Cidade**.

A solução utiliza um microcontrolador ESP32, simulado na plataforma Wokwi, para representar o gerenciamento de filas em unidades de atendimento público, permitindo registrar entradas e atendimentos, acompanhar a quantidade de pessoas aguardando e identificar situações de maior demanda.

O objetivo é demonstrar como tecnologias de IoT podem contribuir para a organização e o monitoramento de serviços públicos.

### Informações do projeto

| Informação | Descrição |
|---|---|
| Projeto | Fácil Município — Wiki Cidade |
| Instituição | FIAP |
| Disciplina | Physical Computing — IoT & IoB |
| Responsável pela frente IoT | Fernanda Rocha Menon — RM 554673 |
| Backlog | G5, G6 e G7 |
| Microcontrolador | ESP32 |
| Ambiente | Wokwi |
| Linguagem | C++ |
| Modalidade | Simulação funcional |

### Demonstração do projeto

**▶ [Assistir ao vídeo demonstrativo no YouTube](https://youtu.be/x3IEq_ttazc)**

**🔗 [Acessar a simulação interativa no Wokwi](https://wokwi.com/projects/480983207617445889)**

---

## 1. Contexto e problema

O Fácil Município — Wiki Cidade tem como proposta facilitar o acesso dos cidadãos a informações sobre serviços municipais, utilizando como piloto o município de Poá, em São Paulo.

Além da dificuldade de localizar informações e orientações sobre serviços públicos, o atendimento presencial pode apresentar desafios relacionados ao acompanhamento da demanda e à organização de filas.

Como extensão demonstrativa da proposta, desenvolvemos um totem inteligente capaz de representar o fluxo de pessoas em uma unidade pública.

**O protótipo não substitui nem integra atualmente o fluxo principal de consulta de serviços do aplicativo.** Sua finalidade é demonstrar uma possível aplicação complementar de IoT.

## 2. Objetivos da solução

### Objetivo geral

Desenvolver um protótipo funcional de monitoramento de filas utilizando ESP32, com indicadores visuais e sonoros que representem diferentes níveis de ocupação em um ambiente de atendimento público.

### Objetivos específicos

- Simular a entrada e a saída de pessoas de uma fila.
- Manter um contador atualizado de cidadãos aguardando atendimento.
- Impedir que a contagem apresente valores negativos.
- Classificar automaticamente o nível de ocupação.
- Apresentar informações em um display OLED.
- Utilizar LEDs para sinalizar visualmente a situação da fila.
- Acionar um alerta sonoro ao atingir o limite crítico previsto.
- Demonstrar o funcionamento do circuito no simulador Wokwi.

## 3. Arquitetura e componentes

A solução utiliza um ESP32 como unidade central de processamento, responsável por interpretar os comandos dos botões e atualizar os dispositivos de saída.

### Componentes utilizados

| Componente | GPIO / Configuração | Finalidade |
|---|---|---|
| ESP32 | Microcontrolador principal | Executar a lógica do sistema |
| Botão de entrada | GPIO 18 | Registrar chegada de pessoa |
| Botão de atendimento | GPIO 19 | Registrar atendimento concluído |
| LED verde | GPIO 25 | Indicar fila tranquila |
| LED amarelo | GPIO 26 | Indicar fila moderada |
| LED vermelho | GPIO 27 | Indicar fila crítica |
| Buzzer | GPIO 23 | Emitir alerta sonoro |
| Display OLED 128×64 | SDA 21 / SCL 22 / I2C 0x3C | Exibir contador e status |

### Fluxo de funcionamento

```text
       INÍCIO DA SIMULAÇÃO
                |
                v
        ESP32 inicializado
        Contador de fila = 0
                |
                v
       Leitura dos botões
                |
       +--------+--------+
       |                 |
       v                 v
 Botão entrada     Botão atendimento
       |                 |
       v                 v
 Incrementa +1     Decrementa -1
                   (mínimo = 0)
       |                 |
       +--------+--------+
                |
                v
       Atualiza contador
                |
                v
       Classifica a fila
                |
       +--------+--------+
       |        |        |
       v        v        v
    0 a 4     5 a 9     10+
    VERDE    AMARELO   VERMELHO
       |        |        |
       +--------+--------+
                |
                v
      Atualiza display OLED
       e indicadores LED
                |
                v
     Alerta sonoro ao atingir
         o limite crítico
                |
                v
      Aguarda nova interação
```

## 4. Regras de funcionamento

O sistema utiliza um contador de pessoas que aguardam atendimento.

Ao iniciar a simulação, o contador é definido como zero. Cada acionamento do botão de entrada acrescenta uma pessoa, enquanto o botão de atendimento reduz a contagem, respeitando o limite mínimo de zero.

A classificação da fila é realizada automaticamente de acordo com os seguintes parâmetros:

| Quantidade de pessoas | Classificação | LED |
|---|---|---|
| 0 a 4 | Fila tranquila | 🟢 Verde |
| 5 a 9 | Fila moderada | 🟡 Amarelo |
| 10 ou mais | Fila crítica | 🔴 Vermelho |

Quando a fila alcança dez pessoas, a lógica do protótipo prevê um aviso sonoro pelo buzzer.

**Observação:** os limites definidos são parâmetros acadêmicos utilizados para demonstrar o comportamento do sistema. Não representam normas oficiais de atendimento municipal.

### Monitoramento das informações

O acompanhamento do funcionamento ocorre por dois recursos:

**Display OLED:** apresenta a quantidade de pessoas aguardando e a classificação atual da fila.

**Monitor serial:** permite acompanhar mensagens de execução e informações relacionadas ao estado do protótipo.

## 5. Implementação no Wokwi — G5

A implementação foi realizada no ambiente de simulação Wokwi, utilizando um ESP32 e componentes eletrônicos virtuais.

O circuito demonstra a interação entre entradas digitais, processamento das informações e dispositivos de saída.

### Cenários de teste

| Cenário | Comportamento observado |
|---|---|
| Inicialização do circuito | Contador inicia em zero |
| Acionamento do botão de entrada | Contador incrementa |
| Acionamento do botão de atendimento | Contador decrementa |
| Tentativa de reduzir abaixo de zero | Contador permanece em zero |
| Contagem de dez pessoas | OLED apresenta `FILA CRITICA` |

### Verificações complementares

Antes da entrega final, devem ser confirmados na demonstração:

- Transição da fila tranquila para moderada, de quatro para cinco pessoas.
- Transição da fila moderada para crítica, de nove para dez pessoas.
- Acionamento visual do LED vermelho.
- Funcionamento do buzzer no limite crítico.

Esses comportamentos estão previstos na lógica do projeto, mas não são apresentados como testes validados sem evidência específica.

**Ambiente de execução:** simulação virtual. Não foram realizados testes com ESP32 físico.

## 6. Relação com o Fácil Município — G6

O projeto Fácil Município tem como foco orientar cidadãos sobre serviços públicos municipais, apresentando informações e referências oficiais.

O totem inteligente representa uma possível evolução complementar voltada ao acompanhamento do atendimento presencial.

Em um cenário futuro, uma unidade municipal poderia utilizar dispositivos IoT para monitorar a demanda de atendimento e fornecer informações operacionais para sistemas autorizados.

## 7. Evidências e entregáveis — G7

### Links da demonstração

**Simulação ESP32 — Wokwi**

https://wokwi.com/projects/480983207617445889

**Vídeo demonstrativo — YouTube**

https://youtu.be/x3IEq_ttazc


### Possibilidades de evolução

- Integrar o ESP32 a uma API de telemetria.
- Implementar persistência e histórico de atendimentos.
- Desenvolver um painel para acompanhamento da demanda.
- Utilizar sensores automáticos para detecção de entradas e saídas.
- Criar alertas operacionais com base em parâmetros definidos por cada unidade.
- Aplicar medidas de segurança e privacidade no tratamento dos dados.

Essas melhorias representam possibilidades futuras e não funcionalidades já implementadas.

## 9. Integrantes

| Integrante | RM |
|---|---|
| Fernanda Rocha Menon | 554673 |
| Luiza Macena Dantas | 556237 |
| Luan Ramos Garcia de Souza | 558537 |
| Matheus Ricciotti | 556930 |
| Matheus Bortolotto | 555189 |

**Responsável pela implementação e documentação da frente IoT:** Fernanda Rocha Menon.

## 10. Referências do projeto

- [Backlog — G5 a G7](../../planejamento/04-backlog.md)
- [Entregas por disciplina](../../planejamento/06-entregas-por-disciplina.md)
- [Especificação do MVP](../../planejamento/02-especificacao-mvp.md)
- [Contratos de integração](../contratos-de-integracao.md)
- [Documentação de infraestrutura e IoT](../features/infraestrutura-e-iot.md)

---

**FIAP — Global Solution 2026**

*Fácil Município — Wiki Cidade | Physical Computing — IoT & IoB*
