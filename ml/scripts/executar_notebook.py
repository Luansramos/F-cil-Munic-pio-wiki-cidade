"""Executa o notebook em um kernel Jupyter real e atualiza seus resumos."""

from __future__ import annotations

import json
import math
import re
from pathlib import Path

import nbformat
from nbclient import NotebookClient


ROOT = Path(__file__).resolve().parents[2]
NOTEBOOK = ROOT / "ml/notebooks/experimento-ouvidoria-poa.ipynb"
METRICS = ROOT / "ml/resultados/metricas.json"


def _source(cell: nbformat.NotebookNode) -> str:
    value = cell.get("source", "")
    return "".join(value) if isinstance(value, list) else str(value)


def _replace_markdown(notebook: nbformat.NotebookNode, marker: str, body: str) -> None:
    begin = f"<!-- {marker}_BEGIN -->"
    end = f"<!-- {marker}_END -->"
    for cell in notebook.cells:
        if cell.cell_type != "markdown":
            continue
        content = _source(cell)
        if begin in content and end in content:
            cell.source = re.sub(
                re.escape(begin) + r".*?" + re.escape(end),
                begin + "\n" + body.strip() + "\n" + end,
                content,
                flags=re.DOTALL,
            )
            return
    raise ValueError(f"Marcadores {marker} não encontrados no notebook")


def _interpretation(result: dict[str, object]) -> tuple[str, str]:
    metrics = result["metricas"]
    model = metrics["modelo"]
    naive = metrics["ingenua_ultimo_mes"]
    train = model["treino"]
    test = model["teste"]
    baseline_mae = naive["teste"]["MAE"]
    improvement = 100 * (baseline_mae - test["MAE"]) / baseline_mae
    comparison = "reduziu" if improvement >= 0 else "aumentou"
    summary = (
        f"Com {result['linhas_treino']} linhas de treino e {result['linhas_teste']} "
        f"de teste ({result['periodo_teste']}), o RandomForestRegressor do "
        f"scikit-learn teve MAE de {test['MAE']:.2f}, RMSE de {test['RMSE']:.2f} "
        f"e R² de {test['R2']:.2f}. O baseline do último mês teve MAE de "
        f"{baseline_mae:.2f}; o modelo {comparison} o MAE em "
        f"{abs(improvement):.1f}%. O resultado cobre apenas cinco meses e não "
        "demonstra capacidade de previsão operacional."
    )
    gap = test["MAE"] - train["MAE"]
    overfit = (
        f"O MAE foi {train['MAE']:.2f} no treino e {test['MAE']:.2f} no teste "
        f"(diferença teste menos treino: {gap:.2f}). O R² de teste foi "
        f"{test['R2']:.2f}. Esses indicadores sugerem ajuste menos estável fora "
        "do treino, mas cinco pontos de teste são insuficientes para um "
        "diagnóstico conclusivo de sobreajuste."
    )
    return summary, overfit


def execute() -> int:
    notebook = nbformat.read(NOTEBOOK, as_version=4)
    nbformat.validate(notebook)
    client = NotebookClient(
        notebook,
        timeout=600,
        kernel_name="python3",
        resources={"metadata": {"path": str(ROOT)}},
        allow_errors=False,
    )
    try:
        client.execute(cwd=str(ROOT))
    finally:
        nbformat.write(notebook, NOTEBOOK)

    result = json.loads(METRICS.read_text(encoding="utf-8"))
    summary, overfit = _interpretation(result)
    _replace_markdown(notebook, "RESULT_SUMMARY", summary)
    _replace_markdown(notebook, "TAKEAWAYS", overfit)
    nbformat.validate(notebook)
    nbformat.write(notebook, NOTEBOOK)
    count = sum(cell.cell_type == "code" for cell in notebook.cells)
    print(f"{count} células executadas em kernel Jupyter; saídas atualizadas.")
    return count


if __name__ == "__main__":
    execute()
