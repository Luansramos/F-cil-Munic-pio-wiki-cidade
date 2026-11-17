#!/usr/bin/env python3
"""Gera eventos de privacidade reais e registra a transcrição das chamadas HTTP (Cybersecurity).

Usa somente dados simulados. Não grava tokens nem senhas. Requer a API em execução
(API_BASE_URL, padrão http://127.0.0.1:5055) e Bootstrap__Email / Bootstrap__Password do operador.
Saída: docs/evidencias-cybersecurity/transcricao-api.json e .md
"""
import json
import os
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

BASE = os.environ.get("API_BASE_URL", "http://127.0.0.1:5055").rstrip("/")
EMAIL = os.environ.get("Bootstrap__Email")
PASSWORD = os.environ.get("Bootstrap__Password")
OUT = Path(os.environ.get("EVIDENCE_DIR", "docs/evidencias-cybersecurity"))
ADMIN_PASSWORD = "SenhaAdminDemo2026!"  # senha de teste de usuário simulado; não é registrada na transcrição
RESULTS = []


def call(method, path, body=None, token=None):
    headers = {"Content-Type": "application/json; charset=utf-8"}
    if token:
        headers["Authorization"] = "Bearer " + token
    data = json.dumps(body).encode("utf-8") if body is not None else None
    request = urllib.request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(request, timeout=40) as response:
            raw = response.read().decode("utf-8")
            return response.status, (json.loads(raw) if raw else None)
    except urllib.error.HTTPError as error:
        raw = error.read().decode("utf-8")
        try:
            return error.code, json.loads(raw)
        except ValueError:
            return error.code, None


def record(cenario, descricao, method, path, request_summary, status, response_summary, esperado):
    RESULTS.append({
        "cenario": cenario, "descricao": descricao, "requisicao": f"{method} {path}",
        "corpo_enviado": request_summary, "status_http": status, "resposta": response_summary,
        "esperado": esperado, "confere": str(status) == str(esperado.split()[0]),
        "horario_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
    })


def painel(city, token, horas=24):
    return call("GET", f"/api/v1/municipios/{city}/seguranca/painel?horas={horas}", token=token)


def main():
    if not EMAIL or not PASSWORD:
        sys.exit("Defina Bootstrap__Email e Bootstrap__Password.")
    status, login = call("POST", "/api/v1/auth/login", {"email": EMAIL, "senha": PASSWORD})
    if status != 200:
        sys.exit(f"Login do operador falhou ({status}).")
    operador = login["token"]

    cities = call("GET", "/api/v1/municipios")[1]
    wanted_a, wanted_b = os.environ.get("CITY_A", "poa-demo"), os.environ.get("CITY_B", "cidade-ficticia")
    candidates = [c for c in cities if c["id"].startswith(wanted_a)] + cities
    published = next((c for c in candidates if call("GET", f"/api/v1/municipios/{c['id']}/servicos")[1]), None)
    others = [c for c in cities if published and c["id"] != published["id"]]
    other = next((c for c in others if c["id"].startswith(wanted_b)), others[0] if others else None)
    if not published or not other:
        sys.exit("É necessário um município com serviço publicado e outro município (rode scripts/demo_fluxo_http.py).")
    a, b = published["id"], other["id"]

    before = painel(a, operador)[1]["resumo"]["total"]

    # C2 — pergunta normal: sem evento.
    q = "Como alterar minha senha? E como funciona o Cata Treco?"
    s, r = call("POST", f"/api/v1/municipios/{a}/conversas", {"pergunta": q})
    after_normal = painel(a, operador)[1]["resumo"]["total"]
    record("C2", "Pergunta normal (menciona 'senha' sem informar uma senha) não gera evento", "POST", f"/api/v1/municipios/{a}/conversas",
           {"pergunta": q}, s, {"informacaoSuficiente": r.get("informacaoSuficiente"), "eventos_antes": before, "eventos_depois": after_normal},
           "200 e nenhum evento novo")
    RESULTS[-1]["confere"] = s == 200 and after_normal == before

    # C1 — PII na pergunta e nos dois lados do histórico.
    body = {"pergunta": "Meu CPF é 123.456.789-00 e tenho diabetes. Como funciona o Cata Treco?",
            "historico": [{"pergunta": "Meu e-mail é maria@example.org", "resposta": "Anotei senha=Abc12345"},
                          {"pergunta": "Meu RG é 12.345.678-9", "resposta": "Entendi"}]}
    s, r = call("POST", f"/api/v1/municipios/{a}/conversas", body)
    record("C1", "Pergunta e histórico (ambos os lados) com PII simulada: sanitizado antes de seguir", "POST", f"/api/v1/municipios/{a}/conversas",
           "pergunta com CPF e saúde; histórico com e-mail, senha e RG (dados simulados)", s,
           {"informacaoSuficiente": r.get("informacaoSuficiente"), "resposta_contem_dados_enviados": any(x in json.dumps(r) for x in ["123.456", "maria@", "Abc12345", "12.345.678"])},
           "200 e resposta sem os dados enviados")
    RESULTS[-1]["confere"] = s == 200 and not RESULTS[-1]["resposta"]["resposta_contem_dados_enviados"]

    # C3 — fonte com CPF é rejeitada antes de persistir.
    fontes_antes = len(call("GET", f"/api/v1/municipios/{a}/fontes", token=operador)[1])
    s, r = call("POST", f"/api/v1/municipios/{a}/fontes", {"titulo": "Fonte de teste", "url": "https://example.org/x",
                "texto": "Procure o responsável, CPF 123.456.789-00.", "dataConsulta": "2026-01-01"}, token=operador)
    fontes_depois = len(call("GET", f"/api/v1/municipios/{a}/fontes", token=operador)[1])
    record("C3", "Fonte com CPF simulado é rejeitada antes da persistência", "POST", f"/api/v1/municipios/{a}/fontes",
           "texto com CPF simulado", s, {"title": (r or {}).get("title"), "fontes_antes": fontes_antes, "fontes_depois": fontes_depois},
           "422 e nada gravado")
    RESULTS[-1]["confere"] = s == 422 and fontes_antes == fontes_depois

    # C3b — rascunho com senha é rejeitado.
    fontes = call("GET", f"/api/v1/municipios/{a}/fontes", token=operador)[1]
    servico = {"id": "servico-teste", "nome": "Serviço de teste", "descricao": "Orientação fictícia.", "orgao": None,
               "etapas": ["Informe a senha=Abc12345 no atendimento."], "requisitos": [], "canalUrl": None, "termos": ["teste"]}
    s, r = call("POST", f"/api/v1/fontes/{fontes[0]['id']}/rascunhos", {"servico": servico}, token=operador)
    record("C3b", "Rascunho com senha em campo textual é rejeitado antes da persistência", "POST", "/api/v1/fontes/{id}/rascunhos",
           "etapa com 'senha=…' (simulada)", s, {"title": (r or {}).get("title")}, "422 e nada gravado")

    # C9 — isolamento municipal e autenticação.
    email_a = f"admin.{a}@example.test"
    s_user, _ = call("POST", "/api/v1/usuarios", {"email": email_a, "senha": ADMIN_PASSWORD, "municipioId": a}, token=operador)
    s, tok = call("POST", "/api/v1/auth/login", {"email": email_a, "senha": ADMIN_PASSWORD})
    admin = tok["token"]
    for desc, who, token, city, esperado in [
        ("Sem token", "anônimo", None, a, "401"),
        ("ADMIN consulta o próprio município", "ADMIN", admin, a, "200"),
        ("ADMIN tenta consultar outro município", "ADMIN", admin, b, "403"),
        ("OPERADOR consulta outro município (política administrativa)", "OPERADOR", operador, b, "200"),
    ]:
        s, r = painel(city, token)
        record("C9", desc, "GET", f"/api/v1/municipios/{city}/seguranca/painel", f"perfil: {who}", s,
               {"total_eventos": r["resumo"]["total"]} if s == 200 else None, esperado)
    s, _ = call("GET", f"/api/v1/municipios/{b}/fontes", token=admin)
    record("C9", "ADMIN tenta listar fontes de outro município", "GET", f"/api/v1/municipios/{b}/fontes", "perfil: ADMIN", s, None, "403")

    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "transcricao-api.json").write_text(json.dumps({"base": BASE, "gerado_em_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                                                             "municipio_com_servico": a, "outro_municipio": b, "cenarios": RESULTS}, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = ["# Transcrição das chamadas reais à API (Cybersecurity)", "",
             f"Gerado em {datetime.now(timezone.utc).isoformat(timespec='seconds')} (UTC) contra `{BASE}`. Dados simulados; sem tokens nem senhas.", "",
             "| Cenário | Descrição | Requisição | Status | Esperado | Confere |", "|---|---|---|---|---|---|"]
    for item in RESULTS:
        lines.append(f"| {item['cenario']} | {item['descricao']} | `{item['requisicao']}` | {item['status_http']} | {item['esperado']} | {'sim' if item['confere'] else '**NÃO**'} |")
    (OUT / "transcricao-api.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    sys.exit(0 if all(i["confere"] for i in RESULTS) else 1)


if __name__ == "__main__":
    main()
