import React from "react";
import { Img, interpolate, staticFile, useCurrentFrame } from "remotion";
import { C, Card, Chip, FPS, Scene, count, typed, useIn, ease } from "./theme";

type P = { duration: number };

/* ---------- 1. Abertura ---------- */
export const S1: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const words = ["Privacidade", "em", "cada", "saída"];
  const team = ["Fernanda Rocha Menon · RM 554673", "Luiza Macena Dantas · RM 556237", "Luan Ramos Garcia de Souza · RM 558537", "Matheus Ricciotti · RM 556930", "Matheus Bortolotto · RM 555189"];
  const shield = useIn(0, 30);
  return (
    <Scene n={1} kicker="Cybersecurity" title="" hero caption="Identificação de dados pessoais, controle de saídas e explicações acessíveis." duration={duration}>
      <div style={{ position: "absolute", left: 0, top: -232, display: "flex", gap: 24, fontSize: 104, fontWeight: 800, lineHeight: 1 }}>
        {words.map((w, i) => {
          const p = useIn(6 + i * 7, 20);
          return <span key={w} style={{ opacity: p, transform: `translateY(${(1 - p) * 40}px)`, color: i === 3 ? C.teal : C.text }}>{w}</span>;
        })}
      </div>
      <div style={{ position: "absolute", left: 0, top: -108, fontSize: 38, color: C.muted, opacity: useIn(40, 20) }}>Fácil Município • Global Solution FIAP 2026</div>
      <svg width={210} height={240} viewBox="0 0 100 114" style={{ position: "absolute", right: 20, top: -248, opacity: shield, transform: `scale(${0.8 + shield * 0.2})` }}>
        <path d="M50 4 L92 20 V54 C92 82 74 100 50 110 C26 100 8 82 8 54 V20 Z" fill="none" stroke={C.teal} strokeWidth={3.5} strokeDasharray={420} strokeDashoffset={420 * (1 - interpolate(f, [0, 60], [0, 1], { extrapolateRight: "clamp" }))} />
        <path d="M32 56 L45 70 L70 40" fill="none" stroke={C.yellow} strokeWidth={6} strokeLinecap="round" strokeLinejoin="round" strokeDasharray={80} strokeDashoffset={80 * (1 - interpolate(f, [50, 80], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" }))} />
      </svg>
      <div style={{ position: "absolute", left: 0, right: 0, top: 10, display: "flex", gap: 30 }}>
        {[["01", "Identificar PII"], ["02", "Bloquear saídas"], ["03", "Explicar com IA"]].map(([n, t], i) => (
          <Card key={n} delay={30 + i * 10} style={{ flex: 1, padding: 28 }}>
            <div style={{ fontSize: 40, fontWeight: 800, color: C.teal }}>{n}</div>
            <div style={{ fontSize: 40, marginTop: 6 }}>{t}</div>
          </Card>
        ))}
      </div>
      <div style={{ position: "absolute", left: 0, right: 0, top: 215, display: "flex", flexWrap: "wrap", gap: 12, opacity: useIn(120, 30) }}>
        {team.map((m) => <span key={m} style={{ fontSize: 26, color: C.muted, border: `1px solid ${C.cardLine}`, borderRadius: 999, padding: "6px 18px" }}>{m}</span>)}
      </div>
    </Scene>
  );
};

/* ---------- 2. Contexto ---------- */
export const S2: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const nodes = [["Cidadão", "Pergunta e histórico", C.blue], ["API C#", "Parsing e controle dos destinos", C.teal], ["Conhecimento", "PostgreSQL + Neo4j", C.yellow]] as const;
  const dot = ((f % 90) / 90);
  return (
    <Scene n={2} kicker="01 / Contexto" title="Um serviço público. Dados protegidos." subtitle="O cidadão consulta serviços municipais com fontes revisadas." caption="A API protege pergunta, histórico, fontes e rascunhos nos caminhos instrumentados." duration={duration}>
      <div style={{ display: "flex", alignItems: "center", gap: 0, height: "100%" }}>
        {nodes.map(([t, d, col], i) => (
          <React.Fragment key={t}>
            <Card delay={10 + i * 12} glow={col} style={{ width: 470, height: 250 }}>
              <div style={{ fontSize: 50, fontWeight: 800, color: col }}>{t}</div>
              <div style={{ fontSize: 34, color: C.muted, marginTop: 14 }}>{d}</div>
            </Card>
            {i < 2 && (
              <div style={{ position: "relative", width: 120, height: 8, background: C.cardLine, borderRadius: 4, opacity: useIn(24 + i * 12) }}>
                <div style={{ position: "absolute", left: `${dot * 100}%`, top: -8, width: 24, height: 24, borderRadius: "50%", background: C.teal, boxShadow: `0 0 20px ${C.teal}` }} />
              </div>
            )}
          </React.Fragment>
        ))}
      </div>
    </Scene>
  );
};

/* ---------- 3. Parsing ---------- */
export const S3: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const input = "Meu CPF é 123.456.789-00 e tenho diabetes.";
  const shown = typed(input, f, 20, 20);
  const masked = f > 20 + (input.length / 20) * FPS + 24;
  const out = useIn(20 + (input.length / 20) * FPS + 24, 18);
  return (
    <Scene n={3} kicker="02 / Parsing" title="O dado sensível é identificado" subtitle="Categoria + origem + marca em memória" caption="Exemplo ilustrativo com dados simulados. Detectar PII não exige salvar o valor original na auditoria." duration={duration}>
      <div style={{ display: "flex", gap: 40, height: "100%" }}>
        <Card style={{ flex: 1 }}>
          <div style={{ fontSize: 34, fontWeight: 800, color: C.teal }}>Entrada simulada</div>
          <div style={{ fontSize: 46, marginTop: 24, lineHeight: 1.4, minHeight: 130 }}>{shown}<span style={{ opacity: Math.floor(f / 12) % 2 ? 0 : 1, color: C.teal }}>▌</span></div>
        </Card>
        <Card style={{ flex: 1 }} glow={masked ? C.teal : undefined} delay={4}>
          <div style={{ fontSize: 34, fontWeight: 800, color: C.teal }}>Tratamento</div>
          <div style={{ fontSize: 46, marginTop: 24, lineHeight: 1.4, minHeight: 130, opacity: masked ? out : 0.15 }}>
            Meu CPF é <b style={{ color: C.yellow }}>[DADO REMOVIDO]</b> e <b style={{ color: C.yellow }}>[DADO REMOVIDO]</b>.
          </div>
          <div style={{ display: "flex", gap: 14, marginTop: 8, flexWrap: "wrap" }}>
            {masked && <><Chip delay={0} size={26}>CPF</Chip><Chip delay={6} size={26}>Saúde</Chip><Chip delay={12} color={C.blue} size={26}>origem: conversa.pergunta</Chip></>}
          </div>
        </Card>
      </div>
    </Scene>
  );
};

/* ---------- 4. Taint ---------- */
export const S4: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const steps = [["SOURCE", "Pergunta / fonte"], ["MARCA", "Só em memória"], ["TRATAMENTO", "Sanitizar / rejeitar"], ["SINK", "Verificar / bloquear"]];
  const active = Math.min(3, Math.floor(Math.max(0, f - 40) / (FPS * 3.2)));
  const sinks = ["IA de conhecimento", "Resposta pública", "Banco de dados", "Grafo publicado", "Logs"];
  return (
    <Scene n={4} kicker="03 / Taint analysis" title="Da entrada até a saída" subtitle="Identificar → tratar → verificar o destino" caption="A camada SinkGuard verifica o contexto enviado à IA, a resposta pública e os destinos de persistência." duration={duration}>
      <div style={{ display: "flex", gap: 26 }}>
        {steps.map(([t, d], i) => (
          <Card key={t} delay={8 + i * 8} glow={i <= active && f > 40 ? C.teal : undefined} style={{ flex: 1, padding: 28 }}>
            <div style={{ fontSize: 34, fontWeight: 800, color: i <= active && f > 40 ? C.teal : C.muted }}>{t}</div>
            <div style={{ fontSize: 31, marginTop: 10 }}>{d}</div>
          </Card>
        ))}
      </div>
      <div style={{ marginTop: 34, fontSize: 28, color: C.muted, letterSpacing: 2, opacity: useIn(60) }}>DESTINOS PROTEGIDOS</div>
      <div style={{ display: "flex", gap: 16, marginTop: 14, flexWrap: "wrap" }}>
        {sinks.map((s, i) => <Chip key={s} delay={64 + i * 8} color={C.blue} size={30}>{s}</Chip>)}
      </div>
    </Scene>
  );
};

/* ---------- 5. Bloqueio ---------- */
export const S5: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const items = [["FONTE COM CPF", "POST /municipios/{id}/fontes", "C3"], ["RASCUNHO COM SENHA", "POST /fontes/{id}/rascunhos", "C3b"]];
  const msg = "O conteúdo informado parece conter dados pessoais… Remova esses dados e envie novamente.";
  return (
    <Scene n={5} kicker="04 / Bloqueio" title="Conteúdo indevido não é salvo" subtitle="Evidência: chamadas reais C3 e C3b" caption="Fonte com CPF e rascunho com senha foram rejeitados com HTTP 422 antes da persistência." duration={duration}>
      <div style={{ display: "flex", gap: 40, height: "100%" }}>
        {items.map(([t, req, id], i) => (
          <Card key={t} delay={8 + i * 12} glow={C.yellow} style={{ flex: 1 }}>
            <div style={{ fontSize: 36, fontWeight: 800, color: C.yellow }}>{t}</div>
            <div style={{ fontFamily: "Consolas, monospace", fontSize: 26, color: C.muted, marginTop: 12 }}>{req}</div>
            <div style={{ display: "flex", alignItems: "baseline", gap: 18, marginTop: 14 }}>
              <span style={{ fontSize: 92, fontWeight: 800, color: C.red, transform: `scale(${0.7 + useIn(30 + i * 12, 12) * 0.3})`, display: "inline-block", opacity: useIn(30 + i * 12, 12) }}>422</span>
              <span style={{ fontSize: 34 }}>cadastro recusado · nada persistido</span>
            </div>
            <div style={{ fontSize: 25, color: C.muted, marginTop: 10, lineHeight: 1.35, minHeight: 70 }}>{typed(msg, f, 50 + i * 12, 40)}</div>
            <div style={{ fontSize: 24, color: C.teal, marginTop: 8, opacity: useIn(110) }}>{id} · transcrição HTTP real</div>
          </Card>
        ))}
      </div>
    </Scene>
  );
};

/* ---------- 6. Auditoria ---------- */
export const S6: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const stats = [[10, "Sanitizações", C.teal], [2, "Bloqueios", C.yellow], [0, "Linhas com valores pesquisados", C.green]] as const;
  const cols = ["categoria", "origem", "destino", "ação", "gravidade", "código técnico"];
  return (
    <Scene n={6} kicker="05 / Auditoria" title="Registrar somente a decisão" subtitle="Evidência: consulta SQL ao PostgreSQL" caption="O recorte registra 10 sanitizações e 2 bloqueios. A pesquisa dos valores simulados retornou zero linhas." duration={duration}>
      <div style={{ display: "flex", gap: 30 }}>
        {stats.map(([v, l, col], i) => (
          <Card key={l} delay={8 + i * 10} glow={col} style={{ flex: 1, padding: 28 }}>
            <div style={{ fontSize: 110, fontWeight: 800, color: col, lineHeight: 1 }}>{count(f, 14 + i * 10, 70 + i * 10, v)}</div>
            <div style={{ fontSize: 32, marginTop: 8 }}>{l}</div>
          </Card>
        ))}
      </div>
      <div style={{ marginTop: 30, fontSize: 28, color: C.muted, letterSpacing: 2, opacity: useIn(70) }}>SOMENTE METADADOS</div>
      <div style={{ display: "flex", gap: 14, marginTop: 14, flexWrap: "wrap" }}>
        {cols.map((c, i) => <Chip key={c} delay={74 + i * 7} size={28}>{c}</Chip>)}
      </div>
    </Scene>
  );
};

/* ---------- 7. IA ---------- */
export const S7: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const text = "O sistema impediu a gravação do seu CPF no banco de dados porque identificou que o texto continha um dado pessoal sensível. Por segurança, nada foi registrado ou publicado no sistema.";
  const arrow = useIn(40, 16);
  return (
    <Scene n={7} kicker="06 / IA generativa" title="A falha técnica ganha contexto" subtitle="Gemini • gemini-3.1-flash-lite" caption="Chamada real registrada: HTTP 200. A IA recebe metadados e explica a ação; não recebe a pergunta original." duration={duration}>
      <div style={{ display: "flex", gap: 24, alignItems: "stretch", height: "100%" }}>
        <Card style={{ width: 560 }}>
          <div style={{ fontSize: 32, fontWeight: 800, color: C.teal }}>METADADOS</div>
          {[["Tipo de dado", "CPF"], ["Destino", "gravação no banco de dados"], ["Ação", "cadastro recusado"], ["Gravidade potencial", "Média"]].map(([k, v], i) => (
            <div key={k} style={{ marginTop: 14, opacity: useIn(12 + i * 8) }}>
              <span style={{ fontSize: 24, color: C.muted }}>{k}: </span><span style={{ fontSize: 30 }}>{v}</span>
            </div>
          ))}
        </Card>
        <div style={{ display: "flex", alignItems: "center", fontSize: 70, color: C.teal, opacity: arrow, transform: `translateX(${(1 - arrow) * -20}px)` }}>➜</div>
        <Card glow={C.teal} delay={36} style={{ flex: 1 }}>
          <div style={{ display: "flex", gap: 14, alignItems: "center" }}>
            <Chip size={24} color={C.green}>HTTP 200</Chip><Chip size={24} color={C.blue}>RESPOSTA REAL</Chip>
          </div>
          <div style={{ fontSize: 34, marginTop: 20, lineHeight: 1.4 }}>{typed(text, f, 56, 34)}</div>
          <div style={{ fontSize: 22, color: C.muted, marginTop: 10, opacity: useIn(260) }}>Trecho do modelo, sem edição.</div>
        </Card>
      </div>
    </Scene>
  );
};

/* ---------- 8. Dashboard ---------- */
export const S8: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const p = interpolate(f, [60, 140], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: ease });
  // Recorte da captura real (12 eventos / 10 sanitizações / 2 bloqueios); zoom do farol para o resumo.
  const top = -interpolate(p, [0, 1], [330, 365]);
  return (
    <Scene n={8} kicker="07 / Dashboard" title="O risco fica visível" subtitle="Captura real • município demonstrativo de Poá" caption="AMARELO indica dados detectados e tratados. Bloqueio bem-sucedido não significa vazamento confirmado." duration={duration}>
      <div style={{ display: "flex", gap: 40, height: "100%" }}>
        <div style={{ width: 560, display: "flex", flexDirection: "column", gap: 20 }}>
          <Card delay={6} glow={C.yellow} style={{ padding: 30 }}>
            <div style={{ fontSize: 44, fontWeight: 800, color: C.yellow }}>12 EVENTOS</div>
            <div style={{ fontSize: 32, color: C.muted, marginTop: 8 }}>10 sanitizações • 2 bloqueios</div>
          </Card>
          <Card delay={16} style={{ padding: 30 }}>
            <div style={{ fontSize: 40, color: C.teal }}>0 proteções pendentes</div>
            <div style={{ fontSize: 40, color: C.teal, marginTop: 6 }}>0 traduções pendentes</div>
          </Card>
        </div>
        <div style={{ flex: 1, borderRadius: 22, border: `1.5px solid ${C.cardLine}`, overflow: "hidden", height: 490, position: "relative", opacity: useIn(10) }}>
          <Img src={staticFile("evidencias/02-painel-indicador-e-resumo.png")} style={{ width: "100%", position: "absolute", left: 0, top }} />
        </div>
      </div>
    </Scene>
  );
};

/* ---------- 9. Explicação / evento ao vivo ---------- */
export const S9: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const swap = interpolate(f, [150, 175], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp" });
  return (
    <Scene n={9} kicker="08 / Explicação" title="Cada evento tem uma origem" subtitle="Captura real • evento ao vivo, da mensagem padrão à explicação da IA" caption="A explicação aparece ao lado da categoria, origem, destino, ação e gravidade potencial." duration={duration}>
      <div style={{ display: "flex", gap: 36, height: "100%" }}>
        <div style={{ width: 420, display: "flex", flexDirection: "column", gap: 20 }}>
          <Card delay={8} glow={swap < 0.5 ? C.yellow : C.teal} style={{ padding: 28 }}>
            <div style={{ fontSize: 30, fontWeight: 800, color: swap < 0.5 ? C.yellow : C.teal }}>{swap < 0.5 ? "1 · EVENTO RECÉM-CRIADO" : "2 · ≈ 10 s DEPOIS"}</div>
            <div style={{ fontSize: 30, marginTop: 10, lineHeight: 1.35 }}>{swap < 0.5 ? "Tradução pendente: mensagem padrão, com rótulo próprio." : "Explicação gerada por IA, com o modelo registrado no evento."}</div>
          </Card>
          <Card delay={20} style={{ padding: 24 }}><div style={{ fontSize: 28, color: C.muted }}>Mensagem padrão nunca é apresentada como resposta de IA.</div></Card>
        </div>
        <div style={{ flex: 1, position: "relative", borderRadius: 22, border: `1.5px solid ${C.cardLine}`, overflow: "hidden", height: 490, opacity: useIn(10), background: "#141f31" }}>
          <Img src={staticFile("capturas/evento-novo-pendente.png")} style={{ position: "absolute", top: 0, left: 0, width: "100%", opacity: 1 - swap }} />
          <Img src={staticFile("capturas/evento-novo-ia.png")} style={{ position: "absolute", top: 0, left: 0, width: "100%", opacity: swap }} />
        </div>
      </div>
    </Scene>
  );
};

/* ---------- 10. Acesso ---------- */
export const S10: React.FC<P> = ({ duration }) => {
  const rows = [["Sem token", "401", C.red], ["ADMIN · próprio município", "200", C.green], ["ADMIN · outro município", "403", C.yellow]] as const;
  return (
    <Scene n={10} kicker="09 / Acesso" title="A fronteira é o município" subtitle="Captura real + transcrição HTTP C9" caption="ADMIN: município próprio, HTTP 200; outro município, HTTP 403. A decisão é aplicada no backend." duration={duration}>
      <div style={{ display: "flex", gap: 40, height: "100%" }}>
        <div style={{ width: 640, display: "flex", flexDirection: "column", gap: 18 }}>
          {rows.map(([t, code, col], i) => (
            <Card key={t} delay={10 + i * 12} style={{ padding: 22, display: "flex", alignItems: "center", justifyContent: "space-between" }} glow={col}>
              <span style={{ fontSize: 34 }}>{t}</span>
              <span style={{ fontSize: 64, fontWeight: 800, color: col }}>{code}</span>
            </Card>
          ))}
        </div>
        <div style={{ flex: 1, borderRadius: 22, border: `1.5px solid ${C.cardLine}`, overflow: "hidden", height: 490, opacity: useIn(16), position: "relative" }}>
          <Img src={staticFile("evidencias/04-painel-perfil-admin-somente-proprio-municipio.png")} style={{ width: "100%" }} />
        </div>
      </div>
    </Scene>
  );
};

/* ---------- 11. Testes ---------- */
export const S11: React.FC<P> = ({ duration }) => {
  const f = useCurrentFrame();
  const n = count(f, 10, 110, 99);
  const circ = 2 * Math.PI * 150;
  return (
    <Scene n={11} kicker="10 / Verificação" title="99 testes aprovados" subtitle="Evidência publicada • 0 falhas" caption="Integração com PostgreSQL e Neo4j reais, além de cenários com banco, grafo e LLM simulados." duration={duration}>
      <div style={{ display: "flex", alignItems: "center", gap: 70 }}>
        <div style={{ position: "relative", width: 340, height: 340 }}>
          <svg width={340} height={340}>
            <circle cx={170} cy={170} r={150} stroke={C.cardLine} strokeWidth={18} fill="none" />
            <circle cx={170} cy={170} r={150} stroke={C.teal} strokeWidth={18} fill="none" strokeLinecap="round" strokeDasharray={circ} strokeDashoffset={circ * (1 - n / 99)} transform="rotate(-90 170 170)" />
          </svg>
          <div style={{ position: "absolute", inset: 0, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 130, fontWeight: 800, color: C.teal }}>{n}</div>
        </div>
        <div>
          <div style={{ fontSize: 60, fontWeight: 800 }}>APROVADOS</div>
          <div style={{ fontSize: 34, color: C.muted, marginTop: 8 }}>0 falhas no registro publicado</div>
          <div style={{ display: "flex", gap: 14, marginTop: 26, flexWrap: "wrap", width: 900 }}>
            {["bloqueio", "autorização", "publicação", "tradução"].map((c, i) => <Chip key={c} delay={50 + i * 8} size={30}>{c}</Chip>)}
          </div>
        </div>
      </div>
    </Scene>
  );
};

/* ---------- 12. Limites ---------- */
export const S12: React.FC<P> = ({ duration }) => {
  const cards = [["ESCOPO", "Caminhos instrumentados"], ["LIMITES", "Regex e histórico local"], ["ENTREGA", "Relatório técnico + evidências"]];
  return (
    <Scene n={12} kicker="11 / Limites" title="Proteção com escopo explícito" subtitle="Código + auditoria + evidências" caption="Regex tem limites. SQLite do mobile fica fora do escopo. A conversa usa demo; a tradução usa Gemini real." duration={duration}>
      <div style={{ display: "flex", gap: 30 }}>
        {cards.map(([t, d], i) => (
          <Card key={t} delay={8 + i * 10} style={{ flex: 1, padding: 30 }}>
            <div style={{ fontSize: 36, fontWeight: 800, color: C.teal }}>{t}</div>
            <div style={{ fontSize: 34, marginTop: 10 }}>{d}</div>
          </Card>
        ))}
      </div>
      <div style={{ marginTop: 40, fontSize: 52, fontWeight: 800, color: C.yellow, opacity: useIn(90, 30) }}>O sistema registra a decisão, nunca o dado.</div>
    </Scene>
  );
};

export const SCENES = [S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12];
