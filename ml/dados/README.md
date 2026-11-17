# Dados mensais da Ouvidoria de Poá

## Fonte e extração

Os números mensais de 2023 e 2024 foram transcritos da tabela de manifestações por mês no Relatório Anual da Ouvidoria Municipal de Poá de 2024 (página 9 do PDF). O relatório publica os totais anuais de 717 em 2023 e 853 em 2024; as somas da planilha reproduzem ambos os totais.

- Fonte usada para a série: [Relatório da Ouvidoria 2024 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf).
- Contexto posterior, não incorporado à série mensal: [Relatório da Ouvidoria 2025 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf). Ele informa 1.074 manifestações registradas no ano e 557 ligações sem geração de protocolo, mas os valores mensais não foram transcritos por não estarem disponíveis com segurança para conferência nesta execução.

## Formato e validações

ouvidoria_poa_mensal.csv tem uma linha por mês, sem valores imputados:

| Coluna | Significado |
|---|---|
| ano | Ano de referência (2023 ou 2024) |
| mes | Mês numérico, de 1 a 12 |
| manifestacoes_registradas | Manifestações/protocolos registrados no mês, conforme a tabela do relatório |
| url_fonte | Link do documento oficial de origem |

A verificação do experimento confirma 24 meses consecutivos, ausência de duplicatas e lacunas, contagens inteiras não negativas e reconciliação dos totais anuais. As somas são 717 para 2023 e 853 para 2024.

## Limites

- A série tem somente 24 observações; a defasagem de um mês reduz a amostra modelável para 23.
- São totais agregados da Ouvidoria, não contagens por tipo de serviço. Não permitem estimar procura por iluminação, coleta ou Cata-Treco separadamente.
- As categorias e formas de atendimento podem mudar entre anos; os relatórios não permitem verificar de forma completa a estabilidade de classificação.
- As 557 ligações de 2025 sem protocolo não são manifestações registradas e não foram somadas ao alvo.
- Embora o relatório de 2025 apresente total anual, esse total não é misturado ao conjunto mensal de 2023–2024.

O arquivo serve a um exercício exploratório acadêmico. Não deve ser usado para dimensionamento operacional ou previsão oficial.
