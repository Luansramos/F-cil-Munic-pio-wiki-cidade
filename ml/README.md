# Entrega acadêmica de ML

Pergunta: o histórico mensal agregado ajuda a estimar o volume de manifestações registradas na Ouvidoria de Poá? O experimento não prevê casos por serviço e não integra o runtime do Nexo Cidadão.

## Arquivos

- dados/ouvidoria_poa_mensal.csv: série pública transcrita e rastreável.
- dados/README.md: fonte, extração, reconciliação e limitações.
- notebooks/experimento-ouvidoria-poa.ipynb: notebook com células e saídas salvas.
- scripts/executar_notebook.py: executa as células em ordem num processo Python limpo e atualiza as saídas do notebook.
- experimento_ouvidoria.py: implementação didática reprodutível do Random Forest para regressão e utilitários de avaliação.
- resultados/: combinações avaliadas, previsões e métricas produzidas pelo notebook.
- figuras/: gráficos vetoriais SVG produzidos pelo notebook.
- Relatório PDF: ../output/pdf/Relatorio_ML_Ouvidoria_Poa.pdf.
- roteiro-video.md: roteiro de gravação de até 5 minutos.

## Execução

Requer Python 3.10 ou superior. Não há dependências de terceiros para executar o experimento:

    python ml/scripts/executar_notebook.py

Para abrir e rodar as células em Jupyter, a máquina do integrante precisa ter Jupyter instalado; o experimento em si usa apenas Python padrão. O código não busca dados pela rede: trabalha com o CSV documentado no repositório. Para recriar o relatório PDF, instale a dependência declarada e execute:

    python -m pip install -r ml/requirements.txt
    python ml/scripts/gerar_relatorio_pdf.py

O Random Forest foi implementado de forma autocontida porque a sessão de preparação não tinha scikit-learn/Jupyter e não conseguia acessar o PyPI. A implementação explicita bootstrap, árvores CART de regressão, max_depth, min_samples_leaf e desempate aleatório. A busca e o TimeSeriesSplit seguem divisão cronológica; não há embaralhamento. Isso mantém o experimento executável sem baixar dependências, mas não substitui uma revisão acadêmica do código do algoritmo.

O relatório registra os resultados gerados e os limites da amostra. A gravação e o envio do vídeo ficam a cargo da equipe; o roteiro está preparado no arquivo acima.
