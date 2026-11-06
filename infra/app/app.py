"""Aplicacao web da infraestrutura do Nexo Cidadao.

Serve a pagina de status (static/index.html) e dois endpoints JSON:
  GET /health      -> {"status": "ok"}
  GET /api/status  -> hostname, uptime, CPU, memoria, disco, servicos e alertas
Sem dependencias externas (apenas biblioteca padrao).
"""
import json
import os
import shutil
import socket
import subprocess
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

BASE = os.path.dirname(os.path.abspath(__file__))
ALERTAS = "/var/log/nexo/alertas.log"
LIMITES = {
    "cpu": int(os.getenv("CPU_MAX", "80")),
    "mem": int(os.getenv("MEM_MAX", "80")),
    "disco": int(os.getenv("DISK_MAX", "80")),
}
SERVICOS = ["nexo-app", "nexo-monitor"]

_lock = threading.Lock()


def _cpu_times():
    with open("/proc/stat") as f:
        v = list(map(int, f.readline().split()[1:]))
    return v[3] + v[4], sum(v)  # idle(+iowait), total


_ultimo = _cpu_times()


def cpu_percent():
    global _ultimo
    with _lock:
        i1, t1 = _ultimo
        i2, t2 = _cpu_times()
        _ultimo = (i2, t2)
    if t2 == t1:
        return 0
    return round(100 * (1 - (i2 - i1) / (t2 - t1)))


def mem_percent():
    info = {}
    with open("/proc/meminfo") as f:
        for linha in f:
            k, v = linha.split(":")
            info[k] = int(v.split()[0])
    return round(100 * (info["MemTotal"] - info["MemAvailable"]) / info["MemTotal"])


def disco_percent():
    u = shutil.disk_usage("/")
    return round(100 * u.used / u.total)


def uptime_segundos():
    with open("/proc/uptime") as f:
        return int(float(f.read().split()[0]))


def estado_servico(nome):
    try:
        r = subprocess.run(["systemctl", "is-active", nome],
                           capture_output=True, text=True, timeout=3)
        return r.stdout.strip() or "desconhecido"
    except Exception:
        return "desconhecido"


def ultimos_alertas(n=5):
    try:
        with open(ALERTAS, encoding="utf-8", errors="replace") as f:
            return [l.strip() for l in f.readlines()[-n:]]
    except OSError:
        return []


def status():
    return {
        "hostname": socket.gethostname(),
        "agora": time.strftime("%Y-%m-%d %H:%M:%S"),
        "uptime_s": uptime_segundos(),
        "cpu": cpu_percent(),
        "mem": mem_percent(),
        "disco": disco_percent(),
        "limites": LIMITES,
        "servicos": {s: estado_servico(s) for s in SERVICOS},
        "alertas": ultimos_alertas(),
    }


class Handler(BaseHTTPRequestHandler):
    def _enviar(self, codigo, tipo, corpo):
        self.send_response(codigo)
        self.send_header("Content-Type", tipo)
        self.send_header("Content-Length", str(len(corpo)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(corpo)

    def do_GET(self):
        caminho = self.path.split("?")[0]
        if caminho in ("/", "/index.html"):
            with open(os.path.join(BASE, "static", "index.html"), "rb") as f:
                self._enviar(200, "text/html; charset=utf-8", f.read())
        elif caminho == "/health":
            self._enviar(200, "application/json", b'{"status":"ok"}')
        elif caminho == "/api/status":
            self._enviar(200, "application/json",
                         json.dumps(status(), ensure_ascii=False).encode("utf-8"))
        else:
            self._enviar(404, "application/json", b'{"erro":"nao encontrado"}')


if __name__ == "__main__":
    porta = int(os.getenv("APP_PORT", "5000"))
    ThreadingHTTPServer(("0.0.0.0", porta), Handler).serve_forever()
