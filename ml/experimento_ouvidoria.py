"""Funções de preparação, persistência e gráficos do experimento da Ouvidoria.

O ajuste do RandomForestRegressor e a validação cruzada ficam visíveis no
notebook; este módulo cuida da leitura e dos artefatos reproduzíveis.
"""

from __future__ import annotations

import csv
import json
import math
from pathlib import Path
from typing import Any, Iterable

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np


SOURCE_URL = "https://poa.sp.gov.br/wp-content/uploads/2025/03/Relatorio-Ouvidoria-2024.pdf"
FEATURE_NAMES = ("mes_sin", "mes_cos", "ano", "lag_1")


def load_monthly_data(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8-sig", newline="") as source:
        rows = [
            {
                "ano": int(row["ano"]),
                "mes": int(row["mes"]),
                "manifestacoes_registradas": int(row["manifestacoes_registradas"]),
                "url_fonte": row["url_fonte"],
            }
            for row in csv.DictReader(source)
        ]
    if not rows:
        raise ValueError("O CSV da série mensal está vazio")
    return rows


def validate_monthly_data(rows: list[dict[str, Any]]) -> dict[str, Any]:
    keys = [(row["ano"], row["mes"]) for row in rows]
    if len(set(keys)) != len(keys):
        raise ValueError("Há meses duplicados na série")
    if any(not 1 <= row["mes"] <= 12 for row in rows):
        raise ValueError("Mês fora do intervalo de 1 a 12")
    if any(row["manifestacoes_registradas"] < 0 for row in rows):
        raise ValueError("A série contém contagem negativa")
    ordered = sorted(rows, key=lambda row: (row["ano"], row["mes"]))
    month_numbers = [row["ano"] * 12 + row["mes"] for row in ordered]
    gaps = sum(
        current - previous - 1
        for previous, current in zip(month_numbers, month_numbers[1:])
    )
    totals = {
        str(year): sum(
            row["manifestacoes_registradas"]
            for row in ordered
            if row["ano"] == year
        )
        for year in sorted({row["ano"] for row in ordered})
    }
    expected = {"2023": 717, "2024": 853}
    if totals != expected:
        raise ValueError(f"Totais anuais divergentes: {totals}; esperado {expected}")
    return {
        "linhas": len(ordered),
        "meses_consecutivos": gaps == 0,
        "duplicatas": len(rows) - len(set(keys)),
        "lacunas": gaps,
        "totais_anuais": totals,
    }


def prepare_supervised_rows(
    monthly: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    ordered = sorted(monthly, key=lambda row: (row["ano"], row["mes"]))
    supervised: list[dict[str, Any]] = []
    for index in range(1, len(ordered)):
        current = ordered[index]
        previous = ordered[index - 1]
        if current["ano"] * 12 + current["mes"] != previous["ano"] * 12 + previous["mes"] + 1:
            raise ValueError("A defasagem exige meses consecutivos sem lacunas")
        angle = 2 * math.pi * (current["mes"] - 1) / 12
        supervised.append(
            {
                "ano": current["ano"],
                "mes": current["mes"],
                "mes_rotulo": f"{current['mes']:02d}/{current['ano']}",
                "mes_sin": math.sin(angle),
                "mes_cos": math.cos(angle),
                "lag_1": previous["manifestacoes_registradas"],
                "alvo": current["manifestacoes_registradas"],
            }
        )
    return supervised


def chronological_holdout(
    rows: list[dict[str, Any]], test_fraction: float = 0.2
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    if len(rows) < 5 or not 0 < test_fraction < 1:
        raise ValueError("A série é curta demais para o holdout solicitado")
    test_size = math.ceil(len(rows) * test_fraction)
    return rows[:-test_size], rows[-test_size:]


def feature_matrix(rows: Iterable[dict[str, Any]]) -> np.ndarray:
    return np.asarray(
        [
            [row["mes_sin"], row["mes_cos"], row["ano"], row["lag_1"]]
            for row in rows
        ],
        dtype=float,
    )


def target_vector(rows: Iterable[dict[str, Any]]) -> np.ndarray:
    return np.asarray([row["alvo"] for row in rows], dtype=float)


def _parameter_label(value: Any) -> str:
    return "ilimitado" if value is None else str(value)


def search_rows(cv_results: dict[str, Any]) -> list[dict[str, Any]]:
    rows = []
    for index in np.argsort(cv_results["rank_test_score"]):
        params = cv_results["params"][index]
        row: dict[str, Any] = {
            "n_estimators": int(params["n_estimators"]),
            "max_depth": _parameter_label(params["max_depth"]),
            "min_samples_leaf": int(params["min_samples_leaf"]),
        }
        fold_errors = []
        for fold in range(3):
            error = -float(cv_results[f"split{fold}_test_score"][index])
            row[f"mae_fold_{fold + 1}"] = error
            fold_errors.append(error)
        row["mae_cv_media"] = float(np.mean(fold_errors))
        rows.append(row)
    return rows


def save_experiment_artifacts(
    root: Path,
    monthly: list[dict[str, Any]],
    quality: dict[str, Any],
    train_rows: list[dict[str, Any]],
    test_rows: list[dict[str, Any]],
    search: Any,
    metrics: dict[str, dict[str, dict[str, float]]],
    train_predictions: np.ndarray,
    test_predictions: np.ndarray,
    train_baseline: np.ndarray,
    test_baseline: np.ndarray,
) -> dict[str, Any]:
    output_dir = root / "ml/resultados"
    output_dir.mkdir(parents=True, exist_ok=True)
    rows = search_rows(search.cv_results_)
    with (output_dir / "busca_hiperparametros.csv").open(
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
        for row in rows:
            writer.writerow(
                {
                    key: f"{value:.6f}" if isinstance(value, float) else value
                    for key, value in row.items()
                }
            )

    predictions = []
    for row, actual, model_value, naive_value in zip(
        test_rows, target_vector(test_rows), test_predictions, test_baseline
    ):
        predictions.append(
            {
                "mes": row["mes_rotulo"],
                "rotulo": row["mes_rotulo"],
                "real": int(actual),
                "random_forest": round(float(model_value), 6),
                "ultimo_mes": int(naive_value),
            }
        )
    with (output_dir / "previsoes_teste.csv").open(
        "w", encoding="utf-8", newline=""
    ) as target:
        writer = csv.DictWriter(
            target, fieldnames=["mes", "real", "random_forest", "ultimo_mes"]
        )
        writer.writeheader()
        for row in predictions:
            writer.writerow({key: row[key] for key in writer.fieldnames})

    best = search.best_params_
    result = {
        "pergunta": "O histórico mensal ajuda a estimar o volume de manifestações registradas da Ouvidoria de Poá?",
        "fonte": SOURCE_URL,
        "biblioteca_modelo": "scikit-learn",
        "versao_scikit_learn": __import__("sklearn").__version__,
        "observacoes_originais": len(monthly),
        "observacoes_modelaveis": len(train_rows) + len(test_rows),
        "linhas_treino": len(train_rows),
        "periodo_treino": f"{train_rows[0]['mes_rotulo']} a {train_rows[-1]['mes_rotulo']}",
        "linhas_teste": len(test_rows),
        "periodo_teste": f"{test_rows[0]['mes_rotulo']} a {test_rows[-1]['mes_rotulo']}",
        "features": list(FEATURE_NAMES),
        "horizonte": "um mês à frente, com atualização rolling usando o valor observado do mês anterior",
        "cv": {
            "metodo": "sklearn.model_selection.TimeSeriesSplit",
            "n_splits": 3,
            "treino_apenas": True,
            "folds": [
                {"treino": 6, "validacao": 4},
                {"treino": 10, "validacao": 4},
                {"treino": 14, "validacao": 4},
            ],
            "metric_selection": "MAE médio",
        },
        "combinacoes": len(rows),
        "melhores_parametros": {
            "n_estimators": int(best["n_estimators"]),
            "max_depth": _parameter_label(best["max_depth"]),
            "min_samples_leaf": int(best["min_samples_leaf"]),
        },
        "melhor_mae_cv": float(-search.best_score_),
        "metricas": metrics,
        "predicoes_teste": [
            {key: row[key] for key in ("rotulo", "real", "random_forest", "ultimo_mes")}
            for row in predictions
        ],
        "qualidade_dados": quality,
        "alerta": "Experimento exploratório com série curta agregada; não prevê serviços individuais.",
    }
    (output_dir / "metricas.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False) + "\n",
        encoding="utf-8",
    )
    return result


def generate_figures(
    root: Path,
    monthly: list[dict[str, Any]],
    test_rows: list[dict[str, Any]],
    test_predictions: np.ndarray,
    test_baseline: np.ndarray,
    cv_results: dict[str, Any],
) -> list[Path]:
    figure_dir = root / "ml/figuras"
    figure_dir.mkdir(parents=True, exist_ok=True)
    plt.rcParams.update(
        {
            "font.family": "DejaVu Sans",
            "font.size": 10,
            "axes.titlesize": 12,
            "axes.labelsize": 10,
            "svg.fonttype": "none",
        }
    )
    colors = {"navy": "#14263d", "teal": "#176b87", "orange": "#d97706", "grid": "#dce4ed"}
    plt.style.use("default")
    paths = []

    labels = [f"{row['mes']:02d}/{row['ano']}" for row in monthly]
    values = [row["manifestacoes_registradas"] for row in monthly]
    fig, ax = plt.subplots(figsize=(10, 4.4), constrained_layout=True)
    ax.plot(labels, values, color=colors["navy"], marker="o", linewidth=2, markersize=4)
    ax.axvline(18.5, color=colors["orange"], linestyle="--", linewidth=1.5, label="Início do teste: 08/2024")
    ax.set(title="Manifestações mensais registradas na Ouvidoria de Poá", ylabel="Manifestações registradas", xlabel="Mês")
    ax.grid(axis="y", color=colors["grid"], linewidth=0.8)
    ax.tick_params(axis="x", rotation=55, labelsize=8)
    ax.legend(frameon=False, loc="upper left")
    path = figure_dir / "serie_mensal.svg"
    fig.savefig(path, format="svg", bbox_inches="tight")
    plt.close(fig)
    paths.append(path)

    test_labels = [row["mes_rotulo"] for row in test_rows]
    actual = target_vector(test_rows)
    fig, ax = plt.subplots(figsize=(8.2, 4.2), constrained_layout=True)
    ax.plot(test_labels, actual, color=colors["navy"], marker="o", linewidth=2, label="Real")
    ax.plot(test_labels, test_predictions, color=colors["teal"], marker="s", linewidth=2, label="Random Forest (scikit-learn)")
    ax.plot(test_labels, test_baseline, color=colors["orange"], marker="^", linestyle="--", linewidth=1.8, label="Baseline: último mês")
    ax.set(title="Teste cronológico: valores observados e previsões", ylabel="Manifestações registradas", xlabel="Mês")
    ax.grid(axis="y", color=colors["grid"], linewidth=0.8)
    ax.legend(frameon=False, ncols=1)
    path = figure_dir / "previsao_teste.svg"
    fig.savefig(path, format="svg", bbox_inches="tight")
    plt.close(fig)
    paths.append(path)

    ranked = search_rows(cv_results)[:6]
    cv_labels = [f"{r['n_estimators']} árvores, prof. {r['max_depth']}, folha {r['min_samples_leaf']}" for r in ranked]
    cv_values = [r["mae_cv_media"] for r in ranked]
    fig, ax = plt.subplots(figsize=(9, 4.5), constrained_layout=True)
    y_pos = np.arange(len(cv_labels))
    ax.barh(y_pos, cv_values, color=colors["teal"])
    ax.set_yticks(y_pos, labels=cv_labels)
    ax.invert_yaxis()
    ax.set(title="Seis melhores configurações pela MAE média de validação", xlabel="MAE média nos três folds (manifestações)")
    ax.grid(axis="x", color=colors["grid"], linewidth=0.8)
    ax.set_axisbelow(True)
    path = figure_dir / "cv_melhores.svg"
    fig.savefig(path, format="svg", bbox_inches="tight")
    plt.close(fig)
    paths.append(path)

    for svg_path in paths:
        svg_text = svg_path.read_text(encoding="utf-8")
        svg_path.write_text(
            "\n".join(line.rstrip() for line in svg_text.splitlines()) + "\n",
            encoding="utf-8",
        )
    return paths
