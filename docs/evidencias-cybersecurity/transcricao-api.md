# Transcrição das chamadas reais à API (Cybersecurity)

Gerado em 2026-11-17T00:55:30+00:00 (UTC) contra `http://127.0.0.1:5055`. Dados simulados; sem tokens nem senhas.

| Cenário | Descrição | Requisição | Status | Esperado | Confere |
|---|---|---|---|---|---|
| C2 | Pergunta normal (menciona 'senha' sem informar uma senha) não gera evento | `POST /api/v1/municipios/poa-demo-b21f845128/conversas` | 200 | 200 e nenhum evento novo | sim |
| C1 | Pergunta e histórico (ambos os lados) com PII simulada: sanitizado antes de seguir | `POST /api/v1/municipios/poa-demo-b21f845128/conversas` | 200 | 200 e resposta sem os dados enviados | sim |
| C3 | Fonte com CPF simulado é rejeitada antes da persistência | `POST /api/v1/municipios/poa-demo-b21f845128/fontes` | 422 | 422 e nada gravado | sim |
| C3b | Rascunho com senha em campo textual é rejeitado antes da persistência | `POST /api/v1/fontes/{id}/rascunhos` | 422 | 422 e nada gravado | sim |
| C9 | Sem token | `GET /api/v1/municipios/poa-demo-b21f845128/seguranca/painel` | 401 | 401 | sim |
| C9 | ADMIN consulta o próprio município | `GET /api/v1/municipios/poa-demo-b21f845128/seguranca/painel` | 200 | 200 | sim |
| C9 | ADMIN tenta consultar outro município | `GET /api/v1/municipios/cidade-ficticia-b21f845128/seguranca/painel` | 403 | 403 | sim |
| C9 | OPERADOR consulta outro município (política administrativa) | `GET /api/v1/municipios/cidade-ficticia-b21f845128/seguranca/painel` | 200 | 200 | sim |
| C9 | ADMIN tenta listar fontes de outro município | `GET /api/v1/municipios/cidade-ficticia-b21f845128/fontes` | 403 | 403 | sim |
