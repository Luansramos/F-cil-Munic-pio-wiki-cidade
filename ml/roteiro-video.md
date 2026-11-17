# Roteiro do vídeo de ML

**Duração planejada:** 4 min 40 s, abaixo do limite de 5 minutos.

**Status:** roteiro preparado; gravação e envio ainda pendentes.

| Tempo | O que mostrar | Narração sugerida |
|---|---|---|
| 0:00-0:25 | Título do notebook e pergunta | “Neste experimento verificamos se o histórico mensal agregado da Ouvidoria de Poá ajuda a estimar o volume de manifestações. O modelo é acadêmico e não prevê casos de um serviço específico.” |
| 0:25-1:05 | CSV e seção de dados | “Usamos o relatório oficial da Ouvidoria de 2024, com os meses de 2023 e 2024. Os 24 valores somam 717 e 853 e conferem com os totais anuais publicados. As 557 ligações de 2025 sem protocolo foram excluídas.” |
| 1:05-1:45 | Features e divisão temporal | “Usamos mês cíclico, ano e a quantidade do mês anterior. Após criar a defasagem, restaram 23 linhas: 18 para treino e os cinco últimos meses para teste. A validação temporal usa três folds somente no treino.” |
| 1:45-2:25 | Tabela das 18 combinações | “Comparamos 18 combinações de número de árvores, profundidade e tamanho mínimo das folhas. O teste final não participa da escolha. O baseline prevê que o próximo mês repita o último valor observado.” |
| 2:25-3:20 | Gráfico do teste e tabela de métricas | “O Random Forest teve MAE de 17,01 no teste, um pouco abaixo dos 18 do baseline. Mas o R2 foi negativo e o erro no treino foi menor que no teste. Com apenas cinco meses, esse ganho não comprova generalização.” |
| 3:20-4:10 | Relatório e limitações | “A série reúne temas diferentes e não diz se a manifestação se refere a iluminação, coleta ou Cata-Treco. Portanto, não recomendamos usar o modelo para dimensionamento nem para escolher qual serviço priorizar.” |
| 4:10-4:40 | Feature LLMWiki / encerrar | “Este experimento fica separado da LLMWiki do produto. A LLMWiki responde com base em conteúdo municipal revisado e fontes; o Random Forest foi uma análise acadêmica de dados agregados. A conclusão é que precisamos de mais meses e dados por categoria para avaliar utilidade prática.” |

## Preparação da gravação

1. Executar o notebook pelo comando em ml/README.md e abrir os gráficos e o PDF.
2. Gravar a tela com voz, sem exibir credenciais ou dados pessoais.
3. Manter o vídeo abaixo de cinco minutos e conferir legibilidade dos gráficos.
4. Substituir este roteiro por um link do vídeo após o upload autorizado da equipe.
