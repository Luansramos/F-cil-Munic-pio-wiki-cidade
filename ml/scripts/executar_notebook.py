"""Executa as células de código do notebook em um processo Python limpo.

O executor usa somente a biblioteca padrão. O notebook continua sendo um
arquivo .ipynb comum e pode também ser aberto em Jupyter com kernel Python 3.
"""

from __future__ import annotations

import contextlib
import io
import json
import math
import os
import re
import sys
import traceback
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
NOTEBOOK = ROOT / "ml/notebooks/experimento-ouvidoria-poa.ipynb"
METRICS = ROOT / "ml/resultados/metricas.json"


def _source(cell: dict[str, object]) -> str:
    value = cell.get("source", "")
    return "".join(value) if isinstance(value, list) else str(value)


def _set_source(cell: dict[str, object], value: str) -> None:
    cell["source"] = value.splitlines(keepends=True)


def _number(value: float) -> str:
    return "indefinido" if math.isnan(value) else f"{value:.2f}"


def _replace_markdown(notebook: dict[str, object], marker: str, body: str) -> None:
    begin = f"<!-- {marker}_BEGIN -->"
    end = f"<!-- {marker}_END -->"
    for cell in notebook["cells"]:
        if cell.get("cell_type") != "markdown":
            continue
        content = _source(cell)
        if begin in content and end in content:
            content = re.sub(
                re.escape(begin) + r".*?" + re.escape(end),
                begin + "\n" + body.strip() + "\n" + end,
                content,
                flags=re.DOTALL,
            )
            _set_source(cell, content)
            return
    raise ValueError(f"Marcadores {marker} não encontrados no notebook")


def _interpretation(result: dict[str, object]) -> tuple[str, str]:
    model = result["metricas"]["modelo"]
    naive = result["metricas"]["ingenua_ultimo_mes"]
    rf_test = model["teste"]
    naive_test = naive["teste"]
    rf_train = model["treino"]
    absolute = naive_test["MAE"] - rf_test["MAE"]
    if absolute > 0:
        pct = 100 * absolute / naive_test["MAE"]
        verdict = f"reduziu o MAE em {pct:.1f}% frente ao último mês"
    elif absolute < 0:
        pct = 100 * abs(absolute) / naive_test["MAE"]
        verdict = f"teve MAE {pct:.1f}% maior que a previsão do último mês"
    else:
        verdict = "empatou em MAE com a previsão do último mês"
    summary = (
        f"Com 18 linhas de treino e 5 de teste ({result['periodo_teste']}), "
        f"o Random Forest teve MAE de {rf_test['MAE']:.2f}, RMSE de "
        f"{rf_test['RMSE']:.2f} e R² de {_number(rf_test['R2'])}. "
        f"O baseline do último mês teve MAE de {naive_test['MAE']:.2f}; "
        f"o modelo {verdict}. Esse resultado descreve apenas os cinco meses "
        "reservados e não demonstra capacidade de previsão operacional."
    )
    train_test_gap = rf_test["MAE"] - rf_train["MAE"]
    overfit = (
        f"No modelo ajustado, o MAE de treino foi {rf_train['MAE']:.2f} e o de "
        f"teste {rf_test['MAE']:.2f} (diferença teste menos treino: "
        f"{train_test_gap:.2f}). Essa diferença é um sinal descritivo de "
        "ajuste, não um diagnóstico estável de sobreajuste com apenas cinco "
        "meses de teste."
    )
    return summary, overfit


def execute() -> int:
    if not NOTEBOOK.is_file():
        raise FileNotFoundError(NOTEBOOK)
    notebook = json.loads(NOTEBOOK.read_text(encoding="utf-8"))
    os.chdir(ROOT)
    sys.path.insert(0, str(ROOT / "ml"))
    namespace: dict[str, object] = {"__name__": "__main__"}
    count = 0
    for cell in notebook.get("cells", []):
        if cell.get("cell_type") != "code":
            continue
        count += 1
        stdout = io.StringIO()
        stderr = io.StringIO()
        cell["execution_count"] = count
        try:
            with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
                exec(compile(_source(cell), f"<notebook-cell-{count}>", "exec"), namespace)
        except BaseException:
            output_text = stdout.getvalue() + stderr.getvalue() + traceback.format_exc()
            cell["outputs"] = [
                {"output_type": "stream", "name": "stdout", "text": output_text}
            ]
            NOTEBOOK.write_text(
                json.dumps(notebook, ensure_ascii=False, indent=1) + "\n",
                encoding="utf-8",
            )
            raise
        output_text = stdout.getvalue() + stderr.getvalue()
        cell["outputs"] = (
            [{"output_type": "stream", "name": "stdout", "text": output_text}]
            if output_text
            else []
        )
    result = json.loads(METRICS.read_text(encoding="utf-8"))
    summary, overfit = _interpretation(result)
    _replace_markdown(notebook, "RESULT_SUMMARY", summary)
    _replace_markdown(notebook, "TAKEAWAYS", overfit)
    NOTEBOOK.write_text(
        json.dumps(notebook, ensure_ascii=False, indent=1) + "\n",
        encoding="utf-8",
    )
    print(f"{count} células de código executadas; saídas do notebook atualizadas.")
    return count


if __name__ == "__main__":
    execute()
