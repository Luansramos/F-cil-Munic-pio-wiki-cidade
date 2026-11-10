#!/usr/bin/env python3
"""Prova HTTP completa do backend: fonte real -> revisão manual -> Neo4j -> resposta.

Executa somente no ambiente de demonstração do CI, com usuário fictício.
Não usa LLM real, nem afirma que o app Expo está conectado.
"""
from __future__ import annotations

from datetime import date, datetime, timezone
import json
import os
from pathlib import Path
import secrets
import sys
from urllib.error import HTTPError
from urllib.request import Request, urlopen

BASE = os.environ.get("API_BASE_URL", "http://127.0.0.1:5055").rstrip("/")
EMAIL = os.environ.get("Bootstrap__Email")
PASSWORD = os.environ.get("Bootstrap__Password")
OUTPUT = Path(os.environ.get("DEMO_EVIDENCE_PATH", "test-results/demo-fluxo-http.json"))

SOURCE_URL = (
    "https://poa.sp.gov.br/"
    "programa-cata-treco-reforca-limpeza-urbana-e-evita-descarte-irregular-em-poa/"
)
SERVICE_URL = "https://poa.sp.gov.br/telefones-uteis/"


def http(method: str, path: str, payload: dict | None = None, token: str | None = None):
    body = None if payload is None else json.dumps(payload, ensure_ascii=False).encode("utf-8")
    headers = {"Accept": "application/json"}
    if payload is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    request = Request(BASE + path, data=body, headers=headers, method=method)
    try:
        with urlopen(request, timeout=20) as response:
            return response.status, json.load(response)
    except HTTPError as error:
        details = error.read().decode("utf-8", errors="replace")[:300]
        raise AssertionError(f"{method} {path}: HTTP {error.code}, resposta: {details}") from error


def require(condition: bool, message: str):
    if not condition:
        raise AssertionError(message)


def main():
    require(bool(EMAIL and PASSWORD), "Configure Bootstrap__Email e Bootstrap__Password.")
    _, session = http("POST", "/api/v1/auth/login", {"email": EMAIL, "senha": PASSWORD})
    token = session["token"]
    suffix = secrets.token_hex(5)
    poa = "poa-demo-" + suffix
    other = "cidade-ficticia-" + suffix

    for city, name in [(poa, "Poá (piloto demonstrativo)"),
                       (other, "Município fictício (isolamento)")]:
        status, _ = http("POST", "/api/v1/municipios",
                         {"id": city, "nome": name, "uf": "SP"}, token)
        require(status == 201, "Cadastro de município não retornou 201.")

    _, before = http("POST", f"/api/v1/municipios/{poa}/conversas",
                     {"pergunta": "Como solicito Cata Treco?"})
    require(not before["informacaoSuficiente"] and before["fontes"] == [],
            "Conteúdo não publicado não pode responder ao cidadão.")

    _, source = http("POST", f"/api/v1/municipios/{poa}/fontes", {
        "titulo": "Programa Cata Treco reforça limpeza urbana — Prefeitura Municipal de Poá",
        "url": SOURCE_URL,
        "texto": (
            "Conforme notícia da Prefeitura de Poá de 24/07/2025, o programa Cata Treco, "
            "da Secretaria de Serviços Urbanos, recolhe materiais em desuso, incluindo "
            "móveis. A solicitação pode ser feita pelo telefone (11) 4636-3270. "
            "A Prefeitura orienta deixar os itens na calçada apenas no dia marcado e indica "
            "atendimento conforme cronograma por bairro. "
            "Não foi identificada nesta fonte uma confirmação de atendimento aos sábados."
        ),
        "dataConsulta": date.today().isoformat()
    }, token)

    # A proposta aqui é manual: depende da revisão humana. Não simula extração por LLM.
    service = {
        "id": "cata-treco",
        "nome": "Cata Treco",
        "descricao": (
            "O serviço Cata Treco de Poá orienta a retirada de móveis e outros "
            "materiais em desuso, conforme organização da Secretaria de Serviços Urbanos."
        ),
        "orgao": "Secretaria de Serviços Urbanos de Poá",
        "etapas": [
            "Solicite informações e a coleta pelo telefone (11) 4636-3270.",
            "Confirme o cronograma do seu bairro com o atendimento oficial.",
            "Deixe os materiais na calçada em frente à residência apenas no dia marcado para a coleta."
        ],
        "requisitos": [],
        "canalUrl": SERVICE_URL,
        "termos": ["sofá", "móveis", "retirada", "cata treco"]
    }
    status, draft = http("POST", f"/api/v1/fontes/{source['id']}/rascunhos",
                         {"servico": service}, token)
    require(status == 201 and not draft["aprovado"], "Fonte deve gerar rascunho NÃO publicado.")
    _, reviewed = http("PUT", f"/api/v1/rascunhos/{draft['id']}", {
        "servico": service,
        "aprovado": True,
        "revisaoEsperada": draft["revisaoId"]
    }, token)
    require(reviewed["aprovado"], "Revisão humana não persistida.")
    _, publication = http("POST", f"/api/v1/rascunhos/{draft['id']}/publicacao",
                          token=token)
    _, retry = http("POST", f"/api/v1/rascunhos/{draft['id']}/publicacao",
                    token=token)
    require(publication["id"] == retry["id"], "Publicação não é idempotente.")

    _, services = http("GET", f"/api/v1/municipios/{poa}/servicos")
    require(any(item["id"] == "cata-treco" for item in services),
            "Serviço não retornou pelo Neo4j na versão ativa.")

    question = "Como solicito a retirada de um sofá?"
    _, answer = http("POST", f"/api/v1/municipios/{poa}/conversas",
                     {"pergunta": question})
    require(answer["informacaoSuficiente"], "A resposta deveria estar sustentada.")
    require(answer["servicosRelacionados"] == ["cata-treco"],
            "ID do serviço publicado não retornou.")
    require(any(fonte["url"] == SOURCE_URL for fonte in answer["fontes"]),
            "Link da fonte oficial não retornou.")
    require("(11) 4636-3270" in answer["resposta"],
            "Resposta não usa a etapa revisada do serviço.")
    require(answer.get("canalOficialUrl") == SERVICE_URL,
            "Canal oficial revisado no grafo não foi devolvido no contrato mobile.")

    _, without_evidence = http("POST", f"/api/v1/municipios/{poa}/conversas",
                               {"pergunta": "Posso retirar um sofá no sábado?"})
    require(not without_evidence["informacaoSuficiente"] and not without_evidence["fontes"],
            "O sistema não pode afirmar horário sem fonte específica.")

    _, follow_up = http("POST", f"/api/v1/municipios/{poa}/conversas", {
        "pergunta": "E no domingo?",
        "historico": [{"pergunta": question, "resposta": answer["resposta"]}]
    })
    require(not follow_up["informacaoSuficiente"] and not follow_up["fontes"],
            "Seguimento não pode afirmar atendimento no domingo sem evidência.")
    require("canalOficialUrl" not in follow_up,
            "O JSON não pode conter canalOficialUrl nulo.")

    _, foreign = http("POST", f"/api/v1/municipios/{other}/conversas",
                      {"pergunta": question})
    require(not foreign["informacaoSuficiente"] and not foreign["fontes"],
            "Falhou isolamento entre municípios.")

    evidence = {
        "tipo": "registro HTTP real de execução da API no CI",
        "executadoEmUtc": datetime.now(timezone.utc).isoformat(),
        "modo": "resposta determinística demonstrativa (NÃO LLM)",
        "appExpoIntegrado": False,
        "propostaGeradaPorLLM": False,
        "fonteVerificada": {
            "titulo": source["titulo"],
            "url": SOURCE_URL,
            "dataConsulta": source["dataConsulta"]
        },
        "etapasExecutadas": [
            "criar município", "cadastrar fonte", "criar rascunho manual",
            "revisar e aprovar", "publicar no Neo4j", "consultar serviço publicado",
            "enviar pergunta via API REST", "receber resposta fundamentada"
        ],
        "pergunta": question,
        "resposta": answer,
        "municipioSemPublicacaoNaoResponde": not before["informacaoSuficiente"],
        "perguntaNaoDocumentadaNaoResponde": not without_evidence["informacaoSuficiente"],
        "seguimentoComHistoricoSemInventarHorario": not follow_up["informacaoSuficiente"],
        "segundoMunicipioIsolado": not foreign["informacaoSuficiente"],
        "repetirPublicacaoMantemVersao": publication["id"] == retry["id"]
    }
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"DEMO HTTP OK: fonte -> revisão -> grafo -> resposta com fonte; evidência: {OUTPUT}")


if __name__ == "__main__":
    try:
        main()
    except Exception as ex:
        print(f"Falha no cenário demonstrativo: {type(ex).__name__}: {ex}", file=sys.stderr)
        sys.exit(1)
