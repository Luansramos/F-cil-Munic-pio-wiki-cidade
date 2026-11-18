# Entrega acadêmica de ML

Pergunta: o histórico mensal agregado ajuda a estimar o volume de manifestações registradas na Ouvidoria de Poá? O experimento não prevê casos por serviço e não integra o runtime do Nexo Cidadão.

## Arquivos

- `dados/ouvidoria_poa_mensal.csv`: série pública transcrita e rastreável.
- `dados/README.md`: fonte, extração, reconciliação e limitações.
- `notebooks/experimento-ouvidoria-poa.ipynb`: notebook executado, com código e saídas salvas.
- `scripts/executar_notebook.py`: executa o notebook usando `nbclient` e um kernel Jupyter real.
- `experimento_ouvidoria.py`: valida os dados, prepara features, salva métricas e previsões e gera gráficos.
- `resultados/`: 18 configurações avaliadas, previsões e métricas produzidas pelo notebook.
- `figuras/`: gráficos SVG gerados com Matplotlib.
- Relatório de duas páginas: `../output/pdf/Relatorio_ML_Ouvidoria_Poa.pdf` e `RELATORIO_ML.md`.
- `roteiro-video.md`: roteiro de apoio; a gravação do vídeo está fora do escopo desta entrega.

## Execução

Requer Python 3.12 ou superior. Instale as dependências fixadas e registre o kernel:

    python -m pip install -r ml/requirements.txt
    python -m ipykernel install --user --name python3 --display-name "Python 3 (ML)"

Execute todas as células do notebook em ordem, em kernel Jupyter:

    python ml/scripts/executar_notebook.py

O notebook usa `scikit-learn` (`RandomForestRegressor`, `GridSearchCV` e `TimeSeriesSplit`) para comparar 18 combinações; o teste cronológico fica separado da seleção. A execução atual foi feita com scikit-learn 1.9.1 e as versões de dependências declaradas em `requirements.txt`.

O código não busca dados pela rede: trabalha com o CSV documentado no repositório. Para recriar o relatório após executar o notebook:

    python ml/scripts/gerar_relatorio_pdf.py

O resultado é exploratório. São 24 observações mensais originais, 23 linhas supervisionadas e cinco meses de teste; portanto, as métricas são instáveis e não representam previsão operacional.
