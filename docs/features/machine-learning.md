# Feature - Experimento acadêmico de Machine Learning

- Status: experimento e pacote documental executados; revisão acadêmica e gravação do vídeo pendentes
- Responsável sugerido: P3
- Backlog: E1-E6
- Escopo: artefato acadêmico independente da API, do painel e do Expo

## Pergunta

O histórico mensal agregado ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá e a orientar a cadência de informação pública? O alvo não discrimina serviço, canal ou tipo de ocorrência. Não sustenta previsão operacional nem priorização de iluminação, coleta ou Cata-Treco separadamente.

## Fonte e preparação

O CSV contém manifestações por mês de 2023 e 2024, transcritas do Relatório Anual da Ouvidoria de Poá de 2024, tabela da página 9. Os totais foram reconciliados: 717 em 2023 e 853 em 2024. A série tem 24 meses consecutivos, sem lacunas, duplicatas ou contagens negativas. A documentação de extração e os limites estão em [dados/README.md](../../ml/dados/README.md).

O Relatório de 2025 informa 1.074 manifestações no ano e 557 ligações sem protocolo. Não há valores mensais de 2025 transcritos nesta entrega; esses números não entram no CSV. Ligações sem protocolo não foram misturadas ao alvo.

## Método

- Modelo: Random Forest para regressão com bootstrap e árvores CART; parâmetros controlados: n_estimators, max_depth e min_samples_leaf.
- Features: seno e cosseno do mês (ciclo anual), ano e contagem do mês anterior.
- Janeiro de 2023 é excluído da matriz supervisionada por não ter defasagem; restam 23 linhas.
- Divisão cronológica: 18 linhas de treino (02/2023-07/2024) e cinco de teste (08/2024-12/2024).
- Horizonte avaliado: um mês à frente; cada previsão usa a contagem observada no mês anterior, inclusive no teste (atualização rolling mensal).
- Busca: 18 combinações, em 3 folds de janela expansiva no treino: tamanhos de treino/validação 6/4, 10/4 e 14/4. O teste não é usado para selecionar parâmetros.
- Seleção por MAE médio de validação; métricas finais MAE, RMSE e R2; baseline ingênuo repete o último mês.
- Semente fixa: 42.

### Adaptação do ambiente

O Python 3.12.14 disponível não tinha scikit-learn nem Jupyter, e o acesso ao PyPI falhou por resolução DNS. Para ainda executar o experimento, foi implementada em [experimento_ouvidoria.py](../../ml/experimento_ouvidoria.py) uma versão didática e autocontida de Random Forest e de TimeSeriesSplit, usando apenas a biblioteca padrão. A validação cruzada mantém os mesmos tamanhos cronológicos de janela expansiva, mas não usa as classes da biblioteca scikit-learn. O executor roda as células do notebook sequencialmente num processo Python limpo; esta execução não ocorreu num kernel Jupyter.

Esta adaptação precisa de revisão de P3/P5. Se a rubrica exigir scikit-learn especificamente ou não aceitar implementação didática, rerodar a busca com scikit-learn num ambiente com pacotes disponíveis antes do envio.

## Resultados

Melhores parâmetros encontrados no treino: 200 árvores, profundidade máxima 2, folha mínima 2. MAE médio nos três folds: 21,82.

| Modelo | Partição | MAE | RMSE | R2 |
|---|---|---:|---:|---:|
| Random Forest | Treino | 9,72 | 13,22 | 0,68 |
| Random Forest | Teste | 17,01 | 18,41 | -0,06 |
| Último mês | Treino | 18,61 | 24,27 | -0,07 |
| Último mês | Teste | 18,00 | 22,86 | -0,63 |

No teste de cinco meses, o MAE da floresta foi 5,5% menor que o baseline. Contudo, o R2 do teste é negativo e o MAE aumenta de 9,72 no treino para 17,01 no teste. O resultado indica risco de sobreajuste e é insuficiente para afirmar que o histórico melhora decisões de atendimento.

## Artefatos e aceite

- Notebook executado: [experimento-ouvidoria-poa.ipynb](../../ml/notebooks/experimento-ouvidoria-poa.ipynb).
- Dados, busca dos 18 parâmetros e previsões: pasta [ml/resultados](../../ml/resultados/).
- Figuras SVG: pasta [ml/figuras](../../ml/figuras/).
- Relatório de duas páginas: [PDF](../../output/pdf/Relatorio_ML_Ouvidoria_Poa.pdf) e [fonte Markdown](../../ml/RELATORIO_ML.md).
- Instruções e execução: [ml/README.md](../../ml/README.md).
- [Roteiro do vídeo de até 5 minutos](../../ml/roteiro-video.md); vídeo não gravado nem enviado nesta execução.

## Verificação e limites

Executado com o Python 3.12.14 incluído no runtime local por python ml/scripts/executar_notebook.py; duas células de código terminaram sem erro, criaram os resultados e salvaram saídas no notebook. O PDF foi gerado com ReportLab 4.4.9, conferido com pdfinfo (2 páginas A4) e renderizado para inspeção visual. A série foi verificada contra os totais publicados.

Há somente 23 exemplos supervisionados e cinco pontos de teste. Os relatórios podem mudar categorias ou canais entre anos, e a série agregada não revela quais serviços geram manifestações. Não usar como previsão oficial, dimensionamento ou priorização por serviço. Revisar a transcrição, a implementação e os critérios acadêmicos antes do envio.

Fontes:

- [Relatório da Ouvidoria de Poá 2024](https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf), tabela mensal, página 9.
- [Relatório da Ouvidoria de Poá 2025](https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf), contexto anual não incorporado à série mensal.
