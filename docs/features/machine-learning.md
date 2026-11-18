# Feature - Experimento acadêmico de Machine Learning

- Status: experimento, notebook e relatório executados; vídeo fora do escopo desta entrega
- Responsável sugerido: P3
- Backlog: E1-E6
- Escopo: artefato acadêmico independente da API, do painel e do Expo

## Pergunta

O histórico mensal agregado ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá e a orientar a cadência de informação pública? O alvo não discrimina serviço, canal ou tipo de ocorrência. Não sustenta previsão operacional nem priorização de iluminação, coleta ou Cata-Treco separadamente.

## Fonte e preparação

O CSV contém manifestações por mês de 2023 e 2024, transcritas do Relatório Anual da Ouvidoria de Poá de 2024, tabela da página 9. Os totais foram reconciliados: 717 em 2023 e 853 em 2024. A série tem 24 meses consecutivos, sem lacunas, duplicatas ou contagens negativas. A documentação de extração e os limites estão em [dados/README.md](../../ml/dados/README.md).

O Relatório de 2025 informa 1.074 manifestações no ano e 557 ligações sem protocolo. Não há valores mensais de 2025 transcritos nesta entrega; esses números não entram no CSV. Ligações sem protocolo não foram misturadas ao alvo.

## Método

- Modelo: `RandomForestRegressor` do scikit-learn 1.9.1; parâmetros: `n_estimators`, `max_depth` e `min_samples_leaf`.
- Justificativa: regressor não linear aceito na atividade, escolhido para combinar calendário cíclico e defasagem sem impor tendência linear. A escolha é didática; a amostra curta não demonstra superioridade sobre outros modelos.
- Features: seno e cosseno do mês (ciclo anual), ano e contagem do mês anterior.
- Janeiro de 2023 é excluído da matriz supervisionada por não ter defasagem; restam 23 linhas.
- Divisão cronológica: 18 linhas de treino (02/2023-07/2024) e cinco de teste (08/2024-12/2024).
- Horizonte: um mês à frente; cada previsão usa a contagem observada do mês anterior, inclusive no teste (atualização mensal rolling).
- Busca: `GridSearchCV` avalia 18 combinações, com `TimeSeriesSplit(n_splits=3)` somente no treino. Folds: 6/4, 10/4 e 14/4 linhas de treino/validação. O teste não participa da seleção.
- Seleção por MAE médio de validação; métricas finais MAE, RMSE e R²; baseline ingênuo repete o último mês.
- Semente fixa: 42.

## Resultados

Melhores parâmetros encontrados no treino: 100 árvores, profundidade máxima 2, folha mínima 2. MAE médio nos três folds: 22,46.

| Modelo | Partição | MAE | RMSE | R² |
|---|---|---:|---:|---:|
| Random Forest | Treino | 10,75 | 14,62 | 0,61 |
| Random Forest | Teste | 19,36 | 19,91 | -0,24 |
| Último mês | Treino | 18,61 | 24,27 | -0,07 |
| Último mês | Teste | 18,00 | 22,86 | -0,63 |

No teste de cinco meses, a MAE do Random Forest foi 7,5% maior que a do baseline (19,36 contra 18,00). A RMSE foi menor que a do baseline, mas o R² do modelo permaneceu negativo. O MAE aumentou de 10,75 no treino para 19,36 no teste. Com cinco observações, essa diferença é apenas descritiva e não permite concluir estabilidade nem sobreajuste de forma confiável.

O resultado não demonstra que o histórico melhora de modo consistente a priorização de informações ao cidadão. A série curta e agregada serve como exercício acadêmico, não como previsão de demanda.

## Artefatos e aceite

- Notebook executado em kernel Jupyter: [experimento-ouvidoria-poa.ipynb](../../ml/notebooks/experimento-ouvidoria-poa.ipynb).
- Dados e extração: [ml/dados](../../ml/dados/).
- Busca das 18 configurações, métricas e previsões: [ml/resultados](../../ml/resultados/).
- Figuras SVG: [ml/figuras](../../ml/figuras/).
- Relatório de duas páginas: [PDF](../../output/pdf/Relatorio_ML_Ouvidoria_Poa.pdf) e [fonte Markdown](../../ml/RELATORIO_ML.md).
- Reprodutibilidade: [instruções](../../ml/README.md) e dependências fixadas em `ml/requirements.txt`.

## Verificação e limites

O notebook foi executado de cima a baixo com Python 3.12.14, scikit-learn 1.9.1 e seis células de código em kernel Jupyter; não houve erros. A execução criou os resultados e os três gráficos SVG. O PDF foi gerado com ReportLab 4.4.9, renderizado e inspecionado em duas páginas A4. Os totais da série foram conferidos contra o relatório oficial.

Há somente 23 exemplos supervisionados e cinco pontos de teste. Os relatórios podem mudar categorias ou canais entre anos, e a série agregada não revela quais serviços geram manifestações. Não usar como previsão oficial, dimensionamento ou priorização por serviço.

Fontes:

- [Relatório da Ouvidoria de Poá 2024](https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf), tabela mensal, página 9.
- [Relatório da Ouvidoria de Poá 2025](https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf), contexto anual não incorporado à série mensal.
