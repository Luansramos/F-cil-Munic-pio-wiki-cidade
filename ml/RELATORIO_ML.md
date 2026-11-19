# Experimento de ML: volume mensal da Ouvidoria de Poá

## Pergunta e escopo

O histórico mensal ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá e pode orientar a cadência de informação pública? O alvo é o total agregado da Ouvidoria. Os dados não permitem prever ocorrências de Cata-Treco, coleta ou iluminação em separado; o resultado é exploratório e não operacional.

## Dados e método

A fonte é o Relatório Anual da Ouvidoria de Poá de 2024, tabela mensal da página 9. A série cobre janeiro de 2023 a dezembro de 2024: 24 meses, com totais reconciliados de 717 e 853, respectivamente. Depois de criar a defasagem de um mês, restam 23 linhas modeláveis. O CSV inclui ano, mês, contagem e link da fonte.

Variáveis: mês cíclico (seno e cosseno), ano e contagem do mês anterior. A divisão cronológica reservou os últimos 5 meses (08/2024 a 12/2024) para teste e usou 18 meses (02/2023 a 07/2024) para treino. Cada previsão de teste é um passo à frente e usa a contagem observada do mês anterior. A validação cruzada tem três folds de janela expansiva somente no treino (6/4, 10/4 e 14/4 observações de treino/validação).

Foram avaliadas 18 configurações de Random Forest, variando n_estimators = 50/100/200, max_depth = 2/4/ilimitado e min_samples_leaf = 1/2. A seleção usou MAE médio de validação. Melhor configuração: {'n_estimators': 100, 'max_depth': '2', 'min_samples_leaf': 2}; MAE médio de CV = 22.46. O comparador ingênuo prevê o valor do mês anterior.

O Random Forest foi escolhido por ser um regressor não linear incluído entre os modelos aceitos na atividade e por combinar calendário cíclico e defasagem sem impor uma tendência linear. A escolha é didática: com 23 linhas modeláveis e menos de dois ciclos anuais, os dados não demonstram superioridade sobre outros modelos.

O modelo RandomForestRegressor e a validação com TimeSeriesSplit são do scikit-learn 1.9.1. GridSearchCV comparou as 18 configurações e ajustou a melhor apenas com os dados de treino.

## Resultados

| Modelo | Partição | MAE | RMSE | R2 |
|---|---|---:|---:|---:|
| Random Forest | Treino | 10.75 | 14.62 | 0.61 |
| Random Forest | Teste | 19.36 | 19.91 | -0.24 |
| Último mês | Treino | 18.61 | 24.27 | -0.07 |
| Último mês | Teste | 18.00 | 22.86 | -0.63 |

No teste de cinco meses, o Random Forest aumentou o MAE em 7.5% frente ao baseline (MAE 19.36 contra 18.00). O R2 do modelo foi -0.24, abaixo de zero, e o MAE subiu de 10.75 no treino para 19.36 no teste. A diferença de generalização e o R2 negativo não sustentam uso operacional.

| Mês de teste | Real | Random Forest | Último mês |
|---|---:|---:|---:|
| 08/2024 | 72 | 53.36 | 44 |
| 09/2024 | 64 | 50.06 | 72 |
| 10/2024 | 71 | 50.13 | 64 |
| 11/2024 | 65 | 49.02 | 71 |
| 12/2024 | 24 | 51.36 | 65 |

## Leitura para o cidadão e limites

O experimento não mostra que conhecer o histórico mensal melhora de forma confiável as orientações ao cidadão. O alvo mistura assuntos diferentes e não identifica qual informação de serviço deveria ser priorizada. A série curta, a mudança de categorias e o teste de cinco meses tornam MAE, RMSE e R2 instáveis; a comparação serve como exercício de método, não como previsão de demanda.

O relatório de 2025 informa 1.074 manifestações registradas no ano e 557 ligações sem protocolo. Esses números contextualizam a evolução, mas não entraram na série mensal: não foram transcritos valores mensais de 2025 com segurança, e ligações sem protocolo não são manifestações registradas.

Os arquivos de busca, métricas, previsões, notebook executado e gráficos SVG acompanham o código em ml/. A gravação do vídeo está fora do escopo desta entrega.

## Fontes

- [Relatório Anual da Ouvidoria de Poá 2024 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf), página 9.
- [Relatório Anual da Ouvidoria de Poá 2025 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf), contexto anual, sem dados mensais incorporados.
