// Captura telas REAIS do painel /auditoria/ com o Chrome em modo headless (protocolo CDP, sem dependências).
// Requer Node 22+, a API em execução e Bootstrap__Email / Bootstrap__Password do operador.
// Uso: node scripts/capturar_painel.mjs   (saída em docs/evidencias-cybersecurity/*.png)
import { spawn } from "node:child_process";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";

const CHROME = process.env.CHROME || "C:/Program Files/Google/Chrome/Application/chrome.exe";
const BASE = (process.env.API_BASE_URL || "http://127.0.0.1:5055").replace(/\/$/, "");
const OUT = process.env.EVIDENCE_DIR || "docs/evidencias-cybersecurity";
const PORT = 9333;
const OPERATOR = { email: process.env.Bootstrap__Email, password: process.env.Bootstrap__Password };
const ADMIN_PASSWORD = "SenhaAdminDemo2026!"; // usuário simulado criado por scripts/evidencias_cybersecurity.py
if (!OPERATOR.email || !OPERATOR.password) throw new Error("Defina Bootstrap__Email e Bootstrap__Password.");

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const profile = await fs.mkdtemp(path.join(os.tmpdir(), "cdp-painel-"));
await fs.mkdir(OUT, { recursive: true });
const chrome = spawn(CHROME, ["--headless=new", `--remote-debugging-port=${PORT}`, `--user-data-dir=${profile}`,
  "--no-first-run", "--disable-gpu", "--hide-scrollbars", `${BASE}/auditoria/`], { stdio: "ignore" });

async function pageSocket() {
  for (let i = 0; i < 60; i++) {
    try {
      const list = await (await fetch(`http://127.0.0.1:${PORT}/json/list`)).json();
      const page = list.find((t) => t.type === "page" && t.url.includes("/auditoria"));
      if (page) return page.webSocketDebuggerUrl;
    } catch { /* Chrome ainda iniciando */ }
    await sleep(500);
  }
  throw new Error("Chrome não respondeu.");
}

const ws = new WebSocket(await pageSocket());
await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });
let seq = 0; const pending = new Map();
ws.onmessage = (m) => { const d = JSON.parse(m.data); if (d.id && pending.has(d.id)) { pending.get(d.id)(d); pending.delete(d.id); } };
const send = (method, params = {}) => new Promise((res, rej) => {
  const id = ++seq; pending.set(id, (d) => (d.error ? rej(new Error(JSON.stringify(d.error))) : res(d.result))); ws.send(JSON.stringify({ id, method, params }));
});
const evalJs = async (expression) => (await send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true })).result.value;
const waitFor = async (expression, label, tries = 80) => { for (let i = 0; i < tries; i++) { if (await evalJs(expression)) return; await sleep(250); } throw new Error("Tempo esgotado: " + label); };

async function shot(name, clipExpression, fromExpression) {
  let clip;
  if (clipExpression) {
    // fromExpression (opcional): começa nesse elemento e termina no fim de clipExpression.
    const r = await evalJs(`(() => { const e = ${clipExpression}; const b = e.getBoundingClientRect(); const f = ${fromExpression || "null"}; const top = f ? f.getBoundingClientRect().top : b.top; return { x: b.x + scrollX, y: top + scrollY, width: b.width, height: b.bottom - top }; })()`);
    clip = { ...r, scale: 1 };
  } else {
    const { cssContentSize } = await send("Page.getLayoutMetrics");
    clip = { x: 0, y: 0, width: cssContentSize.width, height: cssContentSize.height, scale: 1 };
  }
  const { data } = await send("Page.captureScreenshot", { format: "png", captureBeyondViewport: true, clip });
  await fs.writeFile(path.join(OUT, name), Buffer.from(data, "base64"));
  console.log("capturado:", name);
}

async function login(email, password) {
  await evalJs(`(() => { document.getElementById('email').value = ${JSON.stringify(email)}; document.getElementById('senha').value = ${JSON.stringify(password)}; document.getElementById('login-form').requestSubmit(); })()`);
  await waitFor(`!document.getElementById('content').hidden`, "painel carregado");
}
const selectCity = async (prefix) => {
  await evalJs(`(() => { const s = document.getElementById('municipio'); const o = [...s.options].find(x => x.value.startsWith(${JSON.stringify(prefix)})); if (o) { s.value = o.value; s.dispatchEvent(new Event('change')); } return !!o; })()`);
  await sleep(1500); await waitFor(`!document.getElementById('content').hidden`, "painel do município");
};

try {
  await send("Page.enable"); await send("Runtime.enable");
  await send("Emulation.setDeviceMetricsOverride", { width: 1100, height: 900, deviceScaleFactor: 1, mobile: false });
  await waitFor(`document.readyState === 'complete'`, "página");
  await shot("01-login.png");

  await login(OPERATOR.email, OPERATOR.password);
  await selectCity("poa-demo");
  await shot("02-painel-indicador-e-resumo.png", `document.querySelector('main')`);
  await shot("03-eventos-com-explicacao-ia.png", `document.getElementById('eventos-title').closest('section')`);

  const cities = await (await fetch(`${BASE}/api/v1/municipios`)).json();
  const poa = cities.find((c) => c.id.startsWith("poa-demo"));
  await evalJs(`document.getElementById('logout').click()`); await sleep(500);
  await login(`admin.${poa.id}@example.test`, ADMIN_PASSWORD);
  // Só a parte superior: perfil da sessão, único município disponível e indicador.
  await shot("04-painel-perfil-admin-somente-proprio-municipio.png", `document.getElementById('risco-title').closest('section')`, `document.getElementById('filtros-title').closest('section')`);
} finally {
  ws.close();
  spawn("taskkill", ["/PID", String(chrome.pid), "/T", "/F"], { stdio: "ignore" });
  await sleep(1000);
  await fs.rm(profile, { recursive: true, force: true }).catch(() => {});
}
