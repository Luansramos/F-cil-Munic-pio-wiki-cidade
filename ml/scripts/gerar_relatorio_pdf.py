"""Gera o relatório Markdown e o PDF de duas páginas a partir das métricas."""

from __future__ import annotations

import csv
import json
import math
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import (
    Flowable,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[2]
RESULTS = ROOT / "ml/resultados/metricas.json"
PREDICTIONS = ROOT / "ml/resultados/previsoes_teste.csv"
OUTPUT_PDF = ROOT / "output/pdf/Relatorio_ML_Ouvidoria_Poa.pdf"
OUTPUT_MD = ROOT / "ml/RELATORIO_ML.md"
NAVY = colors.HexColor("#14263d")
TEAL = colors.HexColor("#176b87")
ORANGE = colors.HexColor("#d97706")
MUTED = colors.HexColor("#52657b")
PALE = colors.HexColor("#edf5f7")
GRID = colors.HexColor("#dce4ed")


def fmt(value: float) -> str:
    if math.isnan(value):
        return "indefinido"
    return f"{value:.2f}"


def read_inputs() -> tuple[dict[str, object], list[dict[str, str]]]:
    result = json.loads(RESULTS.read_text(encoding="utf-8"))
    with PREDICTIONS.open(encoding="utf-8", newline="") as source:
        predictions = list(csv.DictReader(source))
    if len(predictions) != int(result["linhas_teste"]):
        raise ValueError("As previsões não correspondem ao tamanho informado do teste")
    return result, predictions


class ForecastChart(Flowable):
    def __init__(
        self,
        labels: list[str],
        series: list[tuple[str, list[float], colors.Color]],
    ):
        super().__init__()
        self.labels = labels
        self.series = series
        self.height = 160

    def wrap(self, available_width: float, available_height: float) -> tuple[float, float]:
        self.width = available_width
        return self.width, self.height

    def draw(self) -> None:
        canvas = self.canv
        canvas.saveState()
        left, right, top, bottom = 38, 14, 18, 34
        plot_width = self.width - left - right
        plot_height = self.height - top - bottom
        x_count = max(1, len(self.labels) - 1)
        high = 80.0

        def x(index: int) -> float:
            return left + plot_width * index / x_count

        def y(value: float) -> float:
            return bottom + plot_height * value / high

        canvas.setFont("Helvetica-Bold", 9)
        canvas.setFillColor(NAVY)
        canvas.drawString(0, self.height - 10, "Teste cronológico: valor real e previsões")
        canvas.setStrokeColor(GRID)
        canvas.setLineWidth(0.5)
        for tick in (0, 20, 40, 60, 80):
            ypos = y(tick)
            canvas.line(left, ypos, self.width - right, ypos)
            canvas.setFont("Helvetica", 7)
            canvas.setFillColor(MUTED)
            canvas.drawRightString(left - 5, ypos - 2, str(tick))
        canvas.setStrokeColor(NAVY)
        for index, label in enumerate(self.labels):
            canvas.setFont("Helvetica", 7)
            canvas.setFillColor(MUTED)
            canvas.drawCentredString(x(index), 19, label)
        for series_index, (label, values, color) in enumerate(self.series):
            canvas.setStrokeColor(color)
            canvas.setFillColor(color)
            canvas.setLineWidth(1.8)
            points = [(x(i), y(value)) for i, value in enumerate(values)]
            for first, second in zip(points, points[1:]):
                canvas.line(first[0], first[1], second[0], second[1])
            for xpos, ypos in points:
                canvas.circle(xpos, ypos, 2.6, fill=1, stroke=0)
            legend_y = 8
            legend_x = 42 + 155 * series_index
            canvas.setStrokeColor(color)
            canvas.line(legend_x, legend_y + 2, legend_x + 14, legend_y + 2)
            canvas.setFillColor(MUTED)
            canvas.setFont("Helvetica", 7)
            canvas.drawString(legend_x + 18, legend_y, label)
        canvas.restoreState()


class MonthlyChart(Flowable):
    def __init__(self, labels: list[str], values: list[float], split_index: int):
        super().__init__()
        self.labels = labels
        self.values = values
        self.split_index = split_index
        self.height = 154

    def wrap(self, available_width: float, available_height: float) -> tuple[float, float]:
        self.width = available_width
        return self.width, self.height

    def draw(self) -> None:
        canvas = self.canv
        canvas.saveState()
        left, right, top, bottom = 38, 14, 22, 30
        plot_width = self.width - left - right
        plot_height = self.height - top - bottom
        high = 130.0

        def x(index: float) -> float:
            return left + plot_width * index / max(1, len(self.values) - 1)

        def y(value: float) -> float:
            return bottom + plot_height * value / high

        canvas.setFont("Helvetica-Bold", 9)
        canvas.setFillColor(NAVY)
        canvas.drawString(0, self.height - 10, "Série mensal e período reservado para teste")
        canvas.setStrokeColor(GRID)
        canvas.setLineWidth(0.5)
        for tick in (0, 30, 60, 90, 120):
            ypos = y(tick)
            canvas.line(left, ypos, self.width - right, ypos)
            canvas.setFont("Helvetica", 7)
            canvas.setFillColor(MUTED)
            canvas.drawRightString(left - 5, ypos - 2, str(tick))
        boundary = x(self.split_index - 0.5)
        canvas.setStrokeColor(ORANGE)
        canvas.setDash(3, 2)
        canvas.line(boundary, bottom, boundary, bottom + plot_height)
        canvas.setDash()
        canvas.setStrokeColor(TEAL)
        canvas.setFillColor(TEAL)
        canvas.setLineWidth(1.6)
        points = [(x(i), y(value)) for i, value in enumerate(self.values)]
        for first, second in zip(points, points[1:]):
            canvas.line(first[0], first[1], second[0], second[1])
        for xpos, ypos in points:
            canvas.circle(xpos, ypos, 1.8, fill=1, stroke=0)
        shown = {0, 5, 11, 17, 23}
        for index, label in enumerate(self.labels):
            if index in shown:
                canvas.setFont("Helvetica", 7)
                canvas.setFillColor(MUTED)
                canvas.drawCentredString(x(index), 15, label)
        canvas.setStrokeColor(TEAL)
        canvas.line(left, 4, left + 14, 4)
        canvas.setFont("Helvetica", 7)
        canvas.setFillColor(MUTED)
        canvas.drawString(left + 18, 2, "Observado")
        canvas.setStrokeColor(ORANGE)
        canvas.setDash(3, 2)
        canvas.line(left + 105, 4, left + 119, 4)
        canvas.setDash()
        canvas.setFillColor(MUTED)
        canvas.drawString(left + 123, 2, "Início do teste")
        canvas.restoreState()


def make_styles():
    base = getSampleStyleSheet()
    base.add(
        ParagraphStyle(
            name="ReportTitle",
            parent=base["Title"],
            fontName="Helvetica-Bold",
            fontSize=20,
            leading=23,
            textColor=NAVY,
            alignment=TA_LEFT,
            spaceAfter=4,
        )
    )
    base.add(
        ParagraphStyle(
            name="SubTitle",
            parent=base["Normal"],
            fontName="Helvetica",
            fontSize=9,
            leading=12,
            textColor=MUTED,
            spaceAfter=10,
        )
    )
    base.add(
        ParagraphStyle(
            name="Section",
            parent=base["Heading2"],
            fontName="Helvetica-Bold",
            fontSize=12,
            leading=15,
            textColor=NAVY,
            spaceBefore=8,
            spaceAfter=4,
        )
    )
    base.add(
        ParagraphStyle(
            name="BodySmall",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=8.5,
            leading=11,
            textColor=NAVY,
            spaceAfter=5,
        )
    )
    base.add(
        ParagraphStyle(
            name="Tiny",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=7,
            leading=9,
            textColor=MUTED,
            spaceAfter=3,
        )
    )
    base.add(
        ParagraphStyle(
            name="Callout",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=9.5,
            leading=13,
            textColor=NAVY,
        )
    )
    base.add(
        ParagraphStyle(
            name="TableHead",
            parent=base["BodyText"],
            fontName="Helvetica-Bold",
            fontSize=7.5,
            leading=9,
            textColor=colors.white,
        )
    )
    base.add(
        ParagraphStyle(
            name="TableCell",
            parent=base["BodyText"],
            fontName="Helvetica",
            fontSize=7.5,
            leading=9,
            textColor=NAVY,
        )
    )
    return base


def page_chrome(canvas, document) -> None:
    canvas.saveState()
    width, _ = A4
    canvas.setStrokeColor(GRID)
    canvas.setLineWidth(0.6)
    canvas.line(document.leftMargin, 15 * mm, width - document.rightMargin, 15 * mm)
    canvas.setFont("Helvetica", 7)
    canvas.setFillColor(MUTED)
    canvas.drawString(
        document.leftMargin, 10 * mm, "Fácil Município | Experimento acadêmico de ML"
    )
    canvas.drawRightString(width - document.rightMargin, 10 * mm, f"Página {document.page} de 2")
    canvas.restoreState()


def metric_table(result: dict[str, object], styles) -> Table:
    rows = [
        [
            Paragraph("Modelo", styles["TableHead"]),
            Paragraph("Partição", styles["TableHead"]),
            Paragraph("MAE", styles["TableHead"]),
            Paragraph("RMSE", styles["TableHead"]),
            Paragraph("R2", styles["TableHead"]),
        ]
    ]
    metrics = result["metricas"]
    for name, key in (("Random Forest", "modelo"), ("Último mês", "ingenua_ultimo_mes")):
        for partition in ("treino", "teste"):
            value = metrics[key][partition]
            rows.append(
                [
                    Paragraph(name, styles["TableCell"]),
                    Paragraph(partition.title(), styles["TableCell"]),
                    Paragraph(fmt(value["MAE"]), styles["TableCell"]),
                    Paragraph(fmt(value["RMSE"]), styles["TableCell"]),
                    Paragraph(fmt(value["R2"]), styles["TableCell"]),
                ]
            )
    table = Table(rows, colWidths=[60 * mm, 38 * mm, 24 * mm, 24 * mm, 22 * mm])
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), NAVY),
                ("BACKGROUND", (0, 1), (-1, -1), colors.white),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE]),
                ("GRID", (0, 0), (-1, -1), 0.4, GRID),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
            ]
        )
    )
    return table


def prediction_table(
    predictions: list[dict[str, str]], styles
) -> Table:
    rows = [
        [
            Paragraph("Mês", styles["TableHead"]),
            Paragraph("Real", styles["TableHead"]),
            Paragraph("Random Forest", styles["TableHead"]),
            Paragraph("Último mês", styles["TableHead"]),
        ]
    ]
    rows.extend(
        [
            Paragraph(row["mes"], styles["TableCell"]),
            Paragraph(row["real"], styles["TableCell"]),
            Paragraph(f"{float(row['random_forest']):.2f}", styles["TableCell"]),
            Paragraph(row["ultimo_mes"], styles["TableCell"]),
        ]
        for row in predictions
    )
    table = Table(rows, colWidths=[30 * mm, 25 * mm, 48 * mm, 40 * mm])
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), NAVY),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE]),
                ("GRID", (0, 0), (-1, -1), 0.4, GRID),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 3),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
            ]
        )
    )
    return table


def markdown_report(result: dict[str, object], predictions: list[dict[str, str]]) -> str:
    model = result["metricas"]["modelo"]
    naive = result["metricas"]["ingenua_ultimo_mes"]
    delta = naive["teste"]["MAE"] - model["teste"]["MAE"]
    percent = 100 * delta / naive["teste"]["MAE"]
    direction = "reduziu" if delta > 0 else "aumentou"
    lines = [
        "# Experimento de ML: volume mensal da Ouvidoria de Poá",
        "",
        "## Pergunta e escopo",
        "",
        "O histórico mensal ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá e pode orientar a cadência de informação pública? O alvo é o total agregado da Ouvidoria. Os dados não permitem prever ocorrências de Cata-Treco, coleta ou iluminação em separado; o resultado é exploratório e não operacional.",
        "",
        "## Dados e método",
        "",
        "A fonte é o Relatório Anual da Ouvidoria de Poá de 2024, tabela mensal da página 9. A série cobre janeiro de 2023 a dezembro de 2024: 24 meses, com totais reconciliados de 717 e 853, respectivamente. Depois de criar a defasagem de um mês, restam 23 linhas modeláveis. O CSV inclui ano, mês, contagem e link da fonte.",
        "",
        f"Variáveis: mês cíclico (seno e cosseno), ano e contagem do mês anterior. A divisão cronológica reservou os últimos {result['linhas_teste']} meses ({result['periodo_teste']}) para teste e usou {result['linhas_treino']} meses ({result['periodo_treino']}) para treino. Cada previsão de teste é um passo à frente e usa a contagem observada do mês anterior. A validação cruzada tem três folds de janela expansiva somente no treino (6/4, 10/4 e 14/4 observações de treino/validação).",
        "",
        f"Foram avaliadas {result['combinacoes']} configurações de Random Forest, variando n_estimators = 50/100/200, max_depth = 2/4/ilimitado e min_samples_leaf = 1/2. A seleção usou MAE médio de validação. Melhor configuração: {result['melhores_parametros']}; MAE médio de CV = {result['melhor_mae_cv']:.2f}. O comparador ingênuo prevê o valor do mês anterior.",
        "",
        "A implementação do Random Forest e da divisão temporal é autocontida em Python padrão. Foi usada porque a sessão não tinha scikit-learn/Jupyter e o PyPI estava inacessível. O código explicita bootstrap e árvores CART; deve ser revisado antes da entrega acadêmica.",
        "",
        "## Resultados",
        "",
        "| Modelo | Partição | MAE | RMSE | R2 |",
        "|---|---|---:|---:|---:|",
    ]
    for name, key in (("Random Forest", "modelo"), ("Último mês", "ingenua_ultimo_mes")):
        for partition in ("treino", "teste"):
            value = result["metricas"][key][partition]
            lines.append(
                f"| {name} | {partition.title()} | {fmt(value['MAE'])} | {fmt(value['RMSE'])} | {fmt(value['R2'])} |"
            )
    lines.extend(
        [
            "",
            f"No teste de cinco meses, o Random Forest {direction} o MAE em {abs(percent):.1f}% frente ao baseline (MAE {model['teste']['MAE']:.2f} contra {naive['teste']['MAE']:.2f}). O R2 do modelo foi {fmt(model['teste']['R2'])}, abaixo de zero, e o MAE subiu de {model['treino']['MAE']:.2f} no treino para {model['teste']['MAE']:.2f} no teste. A diferença de generalização e o R2 negativo não sustentam uso operacional.",
            "",
            "| Mês de teste | Real | Random Forest | Último mês |",
            "|---|---:|---:|---:|",
        ]
    )
    for row in predictions:
        lines.append(
            f"| {row['mes']} | {row['real']} | {float(row['random_forest']):.2f} | {row['ultimo_mes']} |"
        )
    lines.extend(
        [
            "",
            "## Leitura para o cidadão e limites",
            "",
            "O experimento não mostra que conhecer o histórico mensal melhora de forma confiável as orientações ao cidadão. O alvo mistura assuntos diferentes e não identifica qual informação de serviço deveria ser priorizada. A série curta, a mudança de categorias e o teste de cinco meses tornam MAE, RMSE e R2 instáveis; a comparação serve como exercício de método, não como previsão de demanda.",
            "",
            "O relatório de 2025 informa 1.074 manifestações registradas no ano e 557 ligações sem protocolo. Esses números contextualizam a evolução, mas não entraram na série mensal: não foram transcritos valores mensais de 2025 com segurança, e ligações sem protocolo não são manifestações registradas.",
            "",
            "Os arquivos de busca, métricas, previsões, notebook executado e gráficos SVG acompanham o código em ml/. A gravação e o envio do vídeo de até cinco minutos ainda precisam ser feitos pela equipe.",
            "",
            "## Fontes",
            "",
            "- [Relatório Anual da Ouvidoria de Poá 2024 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf), página 9.",
            "- [Relatório Anual da Ouvidoria de Poá 2025 (PDF)](https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf), contexto anual, sem dados mensais incorporados.",
        ]
    )
    return "\n".join(lines) + "\n"


def build_pdf(result: dict[str, object], predictions: list[dict[str, str]]) -> None:
    OUTPUT_PDF.parent.mkdir(parents=True, exist_ok=True)
    styles = make_styles()
    doc = SimpleDocTemplate(
        str(OUTPUT_PDF),
        pagesize=A4,
        rightMargin=17 * mm,
        leftMargin=17 * mm,
        topMargin=16 * mm,
        bottomMargin=20 * mm,
        title="Experimento de ML - Ouvidoria de Poá",
        author="Fácil Município - entrega acadêmica",
    )
    story = []
    story.append(Paragraph("Ouvidoria de Poá", styles["ReportTitle"]))
    story.append(
        Paragraph(
            "Estimativa exploratória do volume mensal de manifestações | 17/11/2026",
            styles["SubTitle"],
        )
    )
    answer = (
        f"Resultado: no teste cronológico, o Random Forest obteve MAE de "
        f"{result['metricas']['modelo']['teste']['MAE']:.2f}, contra "
        f"{result['metricas']['ingenua_ultimo_mes']['teste']['MAE']:.2f} do "
        "baseline. O ganho pontual não torna o modelo adequado para previsão "
        "operacional: o R2 de teste é negativo e há somente cinco meses no teste."
    )
    callout = Table([[Paragraph(answer, styles["Callout"])]], colWidths=[doc.width])
    callout.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), PALE),
                ("BOX", (0, 0), (-1, -1), 0.8, TEAL),
                ("LEFTPADDING", (0, 0), (-1, -1), 10),
                ("RIGHTPADDING", (0, 0), (-1, -1), 10),
                ("TOPPADDING", (0, 0), (-1, -1), 8),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 8),
            ]
        )
    )
    story.append(callout)
    story.append(Paragraph("Pergunta e escopo", styles["Section"]))
    story.append(
        Paragraph(
            "O histórico mensal ajuda a estimar o volume de manifestações "
            "registradas da Ouvidoria de Poá e pode orientar a cadência de "
            "informação pública? O alvo é o total agregado. Os relatórios não "
            "permitem prever ocorrências por serviço, como iluminação, coleta "
            "ou Cata-Treco. O resultado é acadêmico e exploratório.",
            styles["BodySmall"],
        )
    )
    story.append(Paragraph("Dados", styles["Section"]))
    story.append(
        Paragraph(
            "O Relatório Anual da Ouvidoria de 2024 publica valores mensais de "
            "2023 e 2024: 24 observações. As somas conferem com os totais "
            "oficiais de 717 em 2023 e 853 em 2024. Não há lacunas nem meses "
            "duplicados. A defasagem de um mês reduz a matriz supervisionada "
            "para 23 linhas. Os dados, URLs e validações estão registrados no CSV.",
            styles["BodySmall"],
        )
    )
    totals = Table(
        [
            [
                Paragraph("Ano", styles["TableHead"]),
                Paragraph("Meses", styles["TableHead"]),
                Paragraph("Manifestações", styles["TableHead"]),
            ],
            ["2023", "jan-dez", "717"],
            ["2024", "jan-dez", "853"],
        ],
        colWidths=[28 * mm, 42 * mm, 55 * mm],
    )
    totals.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), NAVY),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE]),
                ("GRID", (0, 0), (-1, -1), 0.4, GRID),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
            ]
        )
    )
    story.append(totals)
    with (ROOT / "ml/dados/ouvidoria_poa_mensal.csv").open(
        encoding="utf-8", newline=""
    ) as source:
        monthly = list(csv.DictReader(source))
    chart_labels = [
        f"{int(row['mes']):02d}/{str(row['ano'])[-2:]}" for row in monthly
    ]
    chart_values = [float(row["manifestacoes_registradas"]) for row in monthly]
    story.append(Spacer(1, 7))
    story.append(MonthlyChart(chart_labels, chart_values, split_index=19))
    story.append(Paragraph("Modelo e avaliação", styles["Section"]))
    params = result["melhores_parametros"]
    story.append(
        Paragraph(
            "Variáveis: mês cíclico (seno e cosseno), ano e contagem do mês "
            "anterior. A divisão cronológica reservou 18 meses para treino "
            "(02/2023-07/2024) e cinco para teste (08/2024-12/2024). Foram "
            "Cada previsão é de um mês à frente, usando a contagem observada "
            "do mês anterior. A busca avaliou 18 configurações: "
            "n_estimators = 50/100/200, "
            "max_depth = 2/4/ilimitado e min_samples_leaf = 1/2. A seleção "
            "minimizou MAE médio em três folds temporais de janela expansiva "
            "somente no treino (6/4, 10/4 e 14/4 linhas de treino/validação). "
            f"Melhor configuração: {params['n_estimators']} árvores, "
            f"profundidade {params['max_depth']}, folha mínima "
            f"{params['min_samples_leaf']}; "
            f"MAE médio de CV = {result['melhor_mae_cv']:.2f}.",
            styles["BodySmall"],
        )
    )
    story.append(
        Paragraph(
            "A implementação do Random Forest e do particionador temporal é "
            "autocontida em Python padrão. O ambiente não tinha scikit-learn "
            "nem acesso ao PyPI. O código explicita bootstrap e árvores CART; "
            "a equipe deve revisar a implementação antes da apresentação.",
            styles["Tiny"],
        )
    )
    story.append(PageBreak())

    story.append(Paragraph("Resultados e interpretação", styles["ReportTitle"]))
    story.append(
        Paragraph(
        "Cada ponto é uma previsão de um mês à frente, com a contagem observada "
        "no mês anterior. Nenhum alvo do teste participou da seleção de parâmetros.",
            styles["SubTitle"],
        )
    )
    story.append(metric_table(result, styles))
    story.append(Spacer(1, 7))
    labels = [row["mes"][0:2] + "/" + row["mes"][5:7] for row in predictions]
    actual = [float(row["real"]) for row in predictions]
    rf = [float(row["random_forest"]) for row in predictions]
    naive = [float(row["ultimo_mes"]) for row in predictions]
    story.append(
        ForecastChart(
            labels,
            [
                ("Real", actual, NAVY),
                ("Random Forest", rf, TEAL),
                ("Último mês", naive, ORANGE),
            ],
        )
    )
    story.append(Paragraph("Valores do conjunto de teste", styles["Section"]))
    story.append(prediction_table(predictions, styles))
    delta = (
        result["metricas"]["ingenua_ultimo_mes"]["teste"]["MAE"]
        - result["metricas"]["modelo"]["teste"]["MAE"]
    )
    percent = 100 * delta / result["metricas"]["ingenua_ultimo_mes"]["teste"]["MAE"]
    story.append(Paragraph("Leitura do resultado", styles["Section"]))
    story.append(
        Paragraph(
            f"O MAE do Random Forest no teste foi "
            f"{fmt(result['metricas']['modelo']['teste']['MAE'])}, "
            f"{'menor' if delta > 0 else 'maior'} que os "
            f"{fmt(result['metricas']['ingenua_ultimo_mes']['teste']['MAE'])} "
            f"do baseline em {abs(percent):.1f}%. Mesmo assim, R2 = "
            f"{fmt(result['metricas']['modelo']['teste']['R2'])}: o modelo não "
            "superou a previsão pela média do próprio teste. O MAE aumentou de "
            f"{fmt(result['metricas']['modelo']['treino']['MAE'])} no treino para "
            f"{fmt(result['metricas']['modelo']['teste']['MAE'])} no teste. O "
            "gap e a amostra reduzida indicam risco de sobreajuste e baixa "
            "estabilidade, apesar da pequena melhora contra o último mês.",
            styles["BodySmall"],
        )
    )
    story.append(Paragraph("Limites e uso possível", styles["Section"]))
    story.append(
        Paragraph(
            "Cinco pontos de teste não estabelecem capacidade de generalização. "
            "O total mistura temas e canais de atendimento, e mudanças na "
            "classificação podem afetar a série. O resultado não recomenda "
            "priorizar um serviço nem dimensionar equipes. No máximo, o histórico "
            "motiva acompanhamento de tendências agregadas com mais anos e "
            "categorias consistentes. As 557 ligações de 2025 sem protocolo "
            "foram excluídas do alvo.",
            styles["BodySmall"],
        )
    )
    story.append(Paragraph("Fontes", styles["Section"]))
    story.append(
        Paragraph(
            'Poá, Relatório Anual da Ouvidoria 2024, tabela mensal, p. 9: '
            '<link href="https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf" color="#176b87">abrir relatório</link>.',
            styles["Tiny"],
        )
    )
    story.append(
        Paragraph(
            'Poá, Relatório Anual da Ouvidoria 2025, usado apenas para contexto anual '
            '(1.074 manifestações; 557 ligações sem protocolo): '
            '<link href="https://poa.sp.gov.br/wp-content/uploads/2026/04/Relatorio-Ouvidoria-2025.pdf" color="#176b87">abrir relatório</link>.',
            styles["Tiny"],
        )
    )
    doc.build(story, onFirstPage=page_chrome, onLaterPages=page_chrome)
    OUTPUT_MD.write_text(markdown_report(result, predictions), encoding="utf-8")
    print(OUTPUT_PDF)
    print(OUTPUT_MD)


if __name__ == "__main__":
    build_pdf(*read_inputs())
