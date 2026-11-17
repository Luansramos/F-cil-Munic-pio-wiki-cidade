"""Experimento autocontido de Random Forest para a série mensal de Poá.

Usa somente a biblioteca padrão para que a execução seja reprodutível em
ambientes sem acesso a pacotes externos. Cada árvore usa bootstrap e divisões
CART de regressão por redução da soma de quadrados. Todas as features são
consideradas em cada nó; a aleatoriedade vem do bootstrap e do desempate.
"""

from __future__ import annotations

import csv
import html
import itertools
import json
import math
import random
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence


DATA_REL = Path("ml/dados/ouvidoria_poa_mensal.csv")
FEATURE_NAMES = ("mes_sin", "mes_cos", "ano", "lag_1")
PARAM_GRID = {
    "n_estimators": (50, 100, 200),
    "max_depth": (2, 4, None),
    "min_samples_leaf": (1, 2),
}
RANDOM_STATE = 42


def project_root(start: Path | None = None) -> Path:
    current = (start or Path.cwd()).resolve()
    for candidate in (current, *current.parents):
        if (candidate / DATA_REL).is_file():
            return candidate
    raise FileNotFoundError(f"Não encontrei {DATA_REL} a partir de {current}")


def load_monthly_data(path: Path) -> list[dict[str, object]]:
    expected = {"ano", "mes", "manifestacoes_registradas", "url_fonte"}
    with path.open("r", encoding="utf-8-sig", newline="") as source:
        reader = csv.DictReader(source)
        if set(reader.fieldnames or ()) != expected:
            raise ValueError(f"Colunas inesperadas: {reader.fieldnames}")
        rows: list[dict[str, object]] = []
        for raw in reader:
            rows.append(
                {
                    "ano": int(raw["ano"]),
                    "mes": int(raw["mes"]),
                    "manifestacoes_registradas": int(
                        raw["manifestacoes_registradas"]
                    ),
                    "url_fonte": raw["url_fonte"],
                }
            )
    return rows


def validate_monthly_data(rows: Sequence[dict[str, object]]) -> dict[str, object]:
    if len(rows) != 24:
        raise ValueError(f"Esperados 24 meses; encontrados {len(rows)}")
    keys = [(int(row["ano"]), int(row["mes"])) for row in rows]
    if len(set(keys)) != len(keys):
        raise ValueError("Há meses duplicados")
    expected_keys = [(year, month) for year in (2023, 2024) for month in range(1, 13)]
    if keys != expected_keys:
        raise ValueError("A série deve cobrir, em ordem, jan/2023 a dez/2024")
    if any(int(row["mes"]) not in range(1, 13) for row in rows):
        raise ValueError("Mês fora do intervalo 1-12")
    if any(int(row["manifestacoes_registradas"]) < 0 for row in rows):
        raise ValueError("Contagem negativa")
    if any(not str(row["url_fonte"]).startswith("https://") for row in rows):
        raise ValueError("Cada linha deve ter URL HTTPS de origem")

    totals = {
        year: sum(
            int(row["manifestacoes_registradas"])
            for row in rows
            if int(row["ano"]) == year
        )
        for year in (2023, 2024)
    }
    if totals != {2023: 717, 2024: 853}:
        raise ValueError(f"Totais não reconciliados: {totals}")
    return {
        "linhas": len(rows),
        "meses_consecutivos": True,
        "duplicatas": 0,
        "lacunas": 0,
        "totais_anuais": totals,
    }


def prepare_supervised_rows(
    monthly: Sequence[dict[str, object]],
) -> list[dict[str, object]]:
    output: list[dict[str, object]] = []
    counts = [int(row["manifestacoes_registradas"]) for row in monthly]
    for index in range(1, len(monthly)):
        row = monthly[index]
        month = int(row["mes"])
        year = int(row["ano"])
        features = (
            math.sin(2 * math.pi * month / 12),
            math.cos(2 * math.pi * month / 12),
            float(year),
            float(counts[index - 1]),
        )
        output.append(
            {
                "ano": year,
                "mes": month,
                "alvo": counts[index],
                "features": features,
                "lag_1": counts[index - 1],
            }
        )
    return output


def chronological_holdout(
    rows: Sequence[dict[str, object]], test_fraction: float = 0.20
) -> tuple[list[dict[str, object]], list[dict[str, object]]]:
    if not 0 < test_fraction < 1:
        raise ValueError("test_fraction precisa estar entre 0 e 1")
    test_size = math.ceil(len(rows) * test_fraction)
    split = len(rows) - test_size
    return list(rows[:split]), list(rows[split:])


class TimeSeriesSplit:
    """Particionador temporal com janela expansiva, sem embaralhar linhas."""

    def __init__(self, n_splits: int = 3):
        if n_splits < 2:
            raise ValueError("n_splits precisa ser pelo menos 2")
        self.n_splits = n_splits

    def split(self, n_samples: int) -> Iterable[tuple[list[int], list[int]]]:
        test_size = n_samples // (self.n_splits + 1)
        if test_size == 0:
            raise ValueError("A amostra de treino é pequena para este número de folds")
        first_test_start = n_samples - self.n_splits * test_size
        for fold in range(self.n_splits):
            test_start = first_test_start + fold * test_size
            test_end = test_start + test_size
            yield list(range(test_start)), list(range(test_start, test_end))


@dataclass
class _Node:
    value: float
    feature: int | None = None
    threshold: float | None = None
    left: "_Node | None" = None
    right: "_Node | None" = None


def _mean(values: Sequence[float]) -> float:
    return sum(values) / len(values)


def _leaf(examples: Sequence[tuple[tuple[float, ...], float]]) -> _Node:
    return _Node(_mean([target for _, target in examples]))


def _sse(total: float, total_sq: float, count: int) -> float:
    return max(0.0, total_sq - total * total / count)


def _build_tree(
    examples: Sequence[tuple[tuple[float, ...], float]],
    rng: random.Random,
    min_samples_leaf: int,
    max_depth: int | None,
    depth: int = 0,
) -> _Node:
    node = _leaf(examples)
    n = len(examples)
    if n < 2 * min_samples_leaf or (max_depth is not None and depth >= max_depth):
        return node

    baseline = sum((target - node.value) ** 2 for _, target in examples)
    best: tuple[float, int, float] | None = None
    feature_order = list(range(len(examples[0][0])))
    rng.shuffle(feature_order)

    for feature in feature_order:
        ordered = sorted((x[feature], target) for x, target in examples)
        total = sum(target for _, target in ordered)
        total_sq = sum(target * target for _, target in ordered)
        left_sum = 0.0
        left_sq = 0.0
        for index in range(n - 1):
            value, target = ordered[index]
            left_sum += target
            left_sq += target * target
            left_n = index + 1
            right_n = n - left_n
            next_value = ordered[index + 1][0]
            if left_n < min_samples_leaf or right_n < min_samples_leaf:
                continue
            if value == next_value:
                continue
            right_sum = total - left_sum
            right_sq = total_sq - left_sq
            score = _sse(left_sum, left_sq, left_n) + _sse(
                right_sum, right_sq, right_n
            )
            if best is None or score < best[0] - 1e-12:
                best = (score, feature, (value + next_value) / 2)

    if best is None or best[0] >= baseline - 1e-12:
        return node
    _, feature, threshold = best
    left_examples = [item for item in examples if item[0][feature] <= threshold]
    right_examples = [item for item in examples if item[0][feature] > threshold]
    if not left_examples or not right_examples:
        return node
    node.feature = feature
    node.threshold = threshold
    node.left = _build_tree(
        left_examples, rng, min_samples_leaf, max_depth, depth + 1
    )
    node.right = _build_tree(
        right_examples, rng, min_samples_leaf, max_depth, depth + 1
    )
    return node


def _predict_tree(node: _Node, features: Sequence[float]) -> float:
    current = node
    while current.feature is not None:
        if features[current.feature] <= float(current.threshold):
            current = current.left or current
        else:
            current = current.right or current
    return current.value


class RandomForestRegressor:
    """Random forest de regressão com bootstrap de árvores CART."""

    def __init__(
        self,
        n_estimators: int = 100,
        max_depth: int | None = None,
        min_samples_leaf: int = 1,
        random_state: int = RANDOM_STATE,
    ):
        if n_estimators < 1 or min_samples_leaf < 1:
            raise ValueError("Parâmetros de tamanho devem ser positivos")
        self.n_estimators = n_estimators
        self.max_depth = max_depth
        self.min_samples_leaf = min_samples_leaf
        self.random_state = random_state
        self._trees: list[_Node] = []

    def fit(self, X: Sequence[Sequence[float]], y: Sequence[float]):
        if len(X) != len(y) or len(X) == 0:
            raise ValueError("X e y precisam ter a mesma quantidade de linhas")
        rng = random.Random(self.random_state)
        self._trees = []
        training = [
            (tuple(float(v) for v in row), float(target))
            for row, target in zip(X, y)
        ]
        for _ in range(self.n_estimators):
            bootstrap = [
                training[rng.randrange(len(training))] for _ in range(len(training))
            ]
            tree_rng = random.Random(rng.getrandbits(64))
            self._trees.append(
                _build_tree(
                    bootstrap,
                    tree_rng,
                    self.min_samples_leaf,
                    self.max_depth,
                )
            )
        return self

    def predict(self, X: Sequence[Sequence[float]]) -> list[float]:
        if not self._trees:
            raise ValueError("A floresta precisa ser ajustada antes de predict")
        return [
            sum(_predict_tree(tree, row) for tree in self._trees) / len(self._trees)
            for row in X
        ]


def mae(actual: Sequence[float], predicted: Sequence[float]) -> float:
    return sum(abs(a - p) for a, p in zip(actual, predicted)) / len(actual)


def rmse(actual: Sequence[float], predicted: Sequence[float]) -> float:
    return math.sqrt(
        sum((a - p) ** 2 for a, p in zip(actual, predicted)) / len(actual)
    )


def r2(actual: Sequence[float], predicted: Sequence[float]) -> float:
    mean_actual = _mean(actual)
    total = sum((a - mean_actual) ** 2 for a in actual)
    if total == 0:
        return float("nan")
    residual = sum((a - p) ** 2 for a, p in zip(actual, predicted))
    return 1 - residual / total


def metrics(actual: Sequence[float], predicted: Sequence[float]) -> dict[str, float]:
    return {
        "MAE": mae(actual, predicted),
        "RMSE": rmse(actual, predicted),
        "R2": r2(actual, predicted),
    }


def _parameter_combinations() -> list[dict[str, int | None]]:
    return [
        {
            "n_estimators": n_estimators,
            "max_depth": max_depth,
            "min_samples_leaf": min_samples_leaf,
        }
        for n_estimators, max_depth, min_samples_leaf in itertools.product(
            PARAM_GRID["n_estimators"],
            PARAM_GRID["max_depth"],
            PARAM_GRID["min_samples_leaf"],
        )
    ]


def grid_search(
    train_rows: Sequence[dict[str, object]], n_splits: int = 3
) -> list[dict[str, object]]:
    X = [row["features"] for row in train_rows]
    y = [float(row["alvo"]) for row in train_rows]
    folds = list(TimeSeriesSplit(n_splits=n_splits).split(len(train_rows)))
    records: list[dict[str, object]] = []
    for params in _parameter_combinations():
        fold_mae: list[float] = []
        for fold_no, (train_idx, validation_idx) in enumerate(folds, start=1):
            model = RandomForestRegressor(
                **params, random_state=RANDOM_STATE + fold_no
            )
            model.fit([X[i] for i in train_idx], [y[i] for i in train_idx])
            predictions = model.predict([X[i] for i in validation_idx])
            fold_mae.append(mae([y[i] for i in validation_idx], predictions))
        records.append(
            {
                **params,
                "mae_por_fold": fold_mae,
                "cv_mae_media": sum(fold_mae) / len(fold_mae),
            }
        )
    return sorted(
        records,
        key=lambda item: (
            float(item["cv_mae_media"]),
            int(item["n_estimators"]),
            item["max_depth"] is None,
            int(item["max_depth"] or 0),
            int(item["min_samples_leaf"]),
        ),
    )


def _month_label(row: dict[str, object]) -> str:
    return f"{int(row['mes']):02d}/{int(row['ano'])}"


def _svg_line_chart(
    path: Path,
    title: str,
    labels: Sequence[str],
    series: Sequence[tuple[str, Sequence[float], str]],
    y_min: float = 0,
    y_max: float | None = None,
    split_index: int | None = None,
) -> None:
    width, height = 960, 430
    left, right, top, bottom = 74, 28, 54, 78
    plot_w, plot_h = width - left - right, height - top - bottom
    all_values = [float(value) for _, values, _ in series for value in values]
    high = y_max if y_max is not None else max(all_values) * 1.12
    high = max(high, y_min + 1)

    def x_pos(index: int) -> float:
        return left + (plot_w * index / max(1, len(labels) - 1))

    def y_pos(value: float) -> float:
        return top + plot_h * (high - value) / (high - y_min)

    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="100%" height="100%" fill="#ffffff"/>',
        f'<text x="{left}" y="30" font-size="21" font-family="Arial" font-weight="700" fill="#14263d">{html.escape(title)}</text>',
    ]
    for tick in range(5):
        value = y_min + (high - y_min) * tick / 4
        y = y_pos(value)
        parts.extend(
            [
                f'<line x1="{left}" y1="{y:.1f}" x2="{width-right}" y2="{y:.1f}" stroke="#dce4ed"/>',
                f'<text x="{left-12}" y="{y+4:.1f}" text-anchor="end" font-size="12" font-family="Arial" fill="#53657b">{value:.0f}</text>',
            ]
        )
    if split_index is not None:
        x = x_pos(split_index) - plot_w / max(1, len(labels) - 1) / 2
        parts.append(
            f'<line x1="{x:.1f}" y1="{top}" x2="{x:.1f}" y2="{top+plot_h}" stroke="#9a6700" stroke-width="2" stroke-dasharray="6 5"/>'
        )
    for series_index, (label, values, color) in enumerate(series):
        points = " ".join(
            f"{x_pos(i):.1f},{y_pos(float(value)):.1f}"
            for i, value in enumerate(values)
        )
        parts.append(
            f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>'
        )
        for index, value in enumerate(values):
            parts.append(
                f'<circle cx="{x_pos(index):.1f}" cy="{y_pos(float(value)):.1f}" r="3.5" fill="{color}"/>'
            )
        legend_x = left + series_index * 220
        parts.extend(
            [
                f'<line x1="{legend_x}" y1="{height-28}" x2="{legend_x+20}" y2="{height-28}" stroke="{color}" stroke-width="3"/>',
                f'<text x="{legend_x+27}" y="{height-24}" font-size="12" font-family="Arial" fill="#33465c">{html.escape(label)}</text>',
            ]
        )
    step = max(1, math.ceil(len(labels) / 8))
    for index, label in enumerate(labels):
        if index % step == 0 or index == len(labels) - 1:
            parts.append(
                f'<text x="{x_pos(index):.1f}" y="{top+plot_h+22}" text-anchor="middle" font-size="11" font-family="Arial" fill="#53657b">{html.escape(label)}</text>'
            )
    parts.append("</svg>")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(parts), encoding="utf-8")


def save_figures(
    root: Path,
    monthly: Sequence[dict[str, object]],
    train_rows: Sequence[dict[str, object]],
    test_rows: Sequence[dict[str, object]],
    model_test_predictions: Sequence[float],
    baseline_test_predictions: Sequence[float],
    grid: Sequence[dict[str, object]],
) -> None:
    figures = root / "ml" / "figuras"
    labels = [_month_label(row) for row in monthly]
    values = [float(row["manifestacoes_registradas"]) for row in monthly]
    _svg_line_chart(
        figures / "serie_mensal.svg",
        "Manifestações registradas por mês - Ouvidoria de Poá",
        labels,
        [("Série observada", values, "#176b87")],
        y_max=max(values) * 1.18,
        split_index=len(train_rows) + 1,
    )
    test_labels = [_month_label(row) for row in test_rows]
    actual = [float(row["alvo"]) for row in test_rows]
    _svg_line_chart(
        figures / "previsao_teste.svg",
        "Teste cronológico - realizado e previsto",
        test_labels,
        [
            ("Realizado", actual, "#14263d"),
            ("Random Forest", model_test_predictions, "#176b87"),
            ("Último mês (ingênua)", baseline_test_predictions, "#d97706"),
        ],
        y_max=max(actual + list(model_test_predictions) + list(baseline_test_predictions))
        * 1.18,
    )
    top = list(grid[:6])
    labels = [
        f"n{item['n_estimators']}-d{'inf' if item['max_depth'] is None else item['max_depth']}-f{item['min_samples_leaf']}"
        for item in top
    ]
    values = [float(item["cv_mae_media"]) for item in top]
    _svg_line_chart(
        figures / "cv_melhores.svg",
        "Seis melhores configurações por MAE médio de validação",
        labels,
        [("MAE CV", values, "#6c4bb7")],
        y_max=max(values) * 1.18,
    )


def run_experiment(root: Path | None = None) -> dict[str, object]:
    root = (root or project_root()).resolve()
    monthly = load_monthly_data(root / DATA_REL)
    quality = validate_monthly_data(monthly)
    rows = prepare_supervised_rows(monthly)
    train_rows, test_rows = chronological_holdout(rows)
    grid = grid_search(train_rows, n_splits=3)
    best = grid[0]

    X_train = [row["features"] for row in train_rows]
    y_train = [float(row["alvo"]) for row in train_rows]
    X_test = [row["features"] for row in test_rows]
    y_test = [float(row["alvo"]) for row in test_rows]
    selected = RandomForestRegressor(
        n_estimators=int(best["n_estimators"]),
        max_depth=best["max_depth"],
        min_samples_leaf=int(best["min_samples_leaf"]),
        random_state=RANDOM_STATE,
    ).fit(X_train, y_train)
    train_predictions = selected.predict(X_train)
    test_predictions = selected.predict(X_test)
    train_baseline = [float(row["lag_1"]) for row in train_rows]
    test_baseline = [float(row["lag_1"]) for row in test_rows]
    scores = {
        "modelo": {
            "treino": metrics(y_train, train_predictions),
            "teste": metrics(y_test, test_predictions),
        },
        "ingenua_ultimo_mes": {
            "treino": metrics(y_train, train_baseline),
            "teste": metrics(y_test, test_baseline),
        },
    }
    prediction_rows = []
    for row, rf_prediction, naive_prediction in zip(
        test_rows, test_predictions, test_baseline
    ):
        prediction_rows.append(
            {
                "mes": _month_label(row),
                "real": int(row["alvo"]),
                "random_forest": round(rf_prediction, 4),
                "ultimo_mes": int(naive_prediction),
            }
        )

    result = {
        "pergunta": "O histórico mensal ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá?",
        "fonte": str(monthly[0]["url_fonte"]),
        "observacoes_originais": len(monthly),
        "observacoes_modelaveis": len(rows),
        "linhas_treino": len(train_rows),
        "periodo_treino": f"{_month_label(train_rows[0])} a {_month_label(train_rows[-1])}",
        "linhas_teste": len(test_rows),
        "periodo_teste": f"{_month_label(test_rows[0])} a {_month_label(test_rows[-1])}",
        "features": list(FEATURE_NAMES),
        "horizonte": "um mês à frente, com atualização rolling usando o valor observado do mês anterior",
        "cv": {
            "n_splits": 3,
            "treino_apenas": True,
            "folds": [
                {"treino": len(tr), "validacao": len(va)}
                for tr, va in TimeSeriesSplit(3).split(len(train_rows))
            ],
            "metric_selection": "MAE médio",
        },
        "combinacoes": len(grid),
        "melhores_parametros": {
            "n_estimators": best["n_estimators"],
            "max_depth": best["max_depth"],
            "min_samples_leaf": best["min_samples_leaf"],
        },
        "melhor_mae_cv": best["cv_mae_media"],
        "metricas": scores,
        "predicoes_teste": prediction_rows,
        "qualidade_dados": quality,
        "alerta": "Experimento exploratório com série curta agregada; não prevê serviços individuais.",
    }

    results_dir = root / "ml" / "resultados"
    results_dir.mkdir(parents=True, exist_ok=True)
    (results_dir / "metricas.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n",
        encoding="utf-8",
    )
    with (results_dir / "busca_hiperparametros.csv").open(
        "w", encoding="utf-8", newline=""
    ) as target:
        writer = csv.DictWriter(
            target,
            fieldnames=[
                "n_estimators",
                "max_depth",
                "min_samples_leaf",
                "mae_fold_1",
                "mae_fold_2",
                "mae_fold_3",
                "mae_cv_media",
            ],
        )
        writer.writeheader()
        for item in grid:
            writer.writerow(
                {
                    "n_estimators": item["n_estimators"],
                    "max_depth": "ilimitado"
                    if item["max_depth"] is None
                    else item["max_depth"],
                    "min_samples_leaf": item["min_samples_leaf"],
                    "mae_fold_1": round(item["mae_por_fold"][0], 6),
                    "mae_fold_2": round(item["mae_por_fold"][1], 6),
                    "mae_fold_3": round(item["mae_por_fold"][2], 6),
                    "mae_cv_media": round(float(item["cv_mae_media"]), 6),
                }
            )
    with (results_dir / "previsoes_teste.csv").open(
        "w", encoding="utf-8", newline=""
    ) as target:
        writer = csv.DictWriter(
            target, fieldnames=["mes", "real", "random_forest", "ultimo_mes"]
        )
        writer.writeheader()
        writer.writerows(prediction_rows)
    save_figures(
        root,
        monthly,
        train_rows,
        test_rows,
        test_predictions,
        test_baseline,
        grid,
    )
    return result


def format_metric(value: float) -> str:
    return "indefinido" if math.isnan(value) else f"{value:.2f}"


def print_results(result: dict[str, object]) -> None:
    print(
        f"Observações: {result['observacoes_originais']} brutas; "
        f"{result['observacoes_modelaveis']} após defasagem"
    )
    print(
        f"Treino: {result['linhas_treino']} ({result['periodo_treino']}); "
        f"teste: {result['linhas_teste']} ({result['periodo_teste']})"
    )
    print(
        f"Busca: {result['combinacoes']} combinações; "
        "TimeSeriesSplit(3) somente no treino"
    )
    print(
        f"Melhores parâmetros: "
        f"{json.dumps(result['melhores_parametros'], ensure_ascii=False)}; "
        f"MAE CV={float(result['melhor_mae_cv']):.2f}"
    )
    print("Modelo | partição | MAE | RMSE | R²")
    for name, labels in (
        ("Random Forest", "modelo"),
        ("Último mês", "ingenua_ultimo_mes"),
    ):
        for partition in ("treino", "teste"):
            metric = result["metricas"][labels][partition]
            print(
                f"{name} | {partition} | {format_metric(metric['MAE'])} | "
                f"{format_metric(metric['RMSE'])} | {format_metric(metric['R2'])}"
            )
    print("Mês | real | Random Forest | último mês")
    for row in result["predicoes_teste"]:
        print(
            f"{row['mes']} | {row['real']} | "
            f"{row['random_forest']:.2f} | {row['ultimo_mes']}"
        )
