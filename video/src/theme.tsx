import React from "react";
import { AbsoluteFill, Easing, interpolate, useCurrentFrame, useVideoConfig } from "remotion";

export const FPS = 30;
export const C = {
  bg: "#070d18", bg2: "#0c1a2e", card: "#10213a", cardLine: "#23405f", text: "#eef3fa", muted: "#9db0c7",
  teal: "#4ecdc4", yellow: "#ffd166", red: "#ff6b6b", green: "#5fe3a1", blue: "#6aa5ff",
};
export const FONT = '"Segoe UI", "Helvetica Neue", Arial, sans-serif';

// Cortes das cenas: meio de cada pausa longa (~1,3 s) da narração reaproveitada (Facil_Municipio_Narracao_PTBR.mp3, 214,6 s).
export const BOUNDS_S = [0, 16.1, 32.25, 50.7, 67.15, 86.2, 104.2, 122.15, 139.15, 156.05, 174.2, 193.8, 214.6];
export const sceneFrames = BOUNDS_S.slice(1).map((end, i) => ({
  from: Math.round(BOUNDS_S[i] * FPS),
  duration: Math.round(end * FPS) - Math.round(BOUNDS_S[i] * FPS),
}));
export const TOTAL_FRAMES = Math.round(BOUNDS_S[BOUNDS_S.length - 1] * FPS);

export const ease = Easing.bezier(0.22, 1, 0.36, 1);
export const useIn = (start: number, dur = 18) => {
  const f = useCurrentFrame();
  return interpolate(f, [start, start + dur], [0, 1], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: ease });
};
export const typed = (text: string, f: number, start: number, cps = 28) =>
  text.slice(0, Math.max(0, Math.min(text.length, Math.floor(((f - start) / FPS) * cps))));
export const count = (f: number, start: number, end: number, to: number) =>
  Math.round(interpolate(f, [start, end], [0, to], { extrapolateLeft: "clamp", extrapolateRight: "clamp", easing: Easing.out(Easing.cubic) }));

export const Background: React.FC = () => {
  const f = useCurrentFrame();
  const t = f / 220;
  return (
    <AbsoluteFill style={{ background: `linear-gradient(135deg, ${C.bg} 0%, ${C.bg2} 100%)` }}>
      <div style={{ position: "absolute", width: 900, height: 900, borderRadius: "50%", left: -250 + Math.sin(t) * 80, top: -350 + Math.cos(t) * 60,
        background: "radial-gradient(circle, rgba(78,205,196,0.20) 0%, rgba(78,205,196,0) 65%)" }} />
      <div style={{ position: "absolute", width: 800, height: 800, borderRadius: "50%", right: -250 + Math.cos(t * 0.8) * 90, bottom: -380 + Math.sin(t * 0.9) * 70,
        background: "radial-gradient(circle, rgba(106,165,255,0.16) 0%, rgba(106,165,255,0) 65%)" }} />
      <div style={{ position: "absolute", inset: 0, opacity: 0.06,
        backgroundImage: "linear-gradient(#fff 1px, transparent 1px), linear-gradient(90deg, #fff 1px, transparent 1px)", backgroundSize: "64px 64px" }} />
    </AbsoluteFill>
  );
};

export const Card: React.FC<{ children: React.ReactNode; style?: React.CSSProperties; delay?: number; glow?: string }> = ({ children, style, delay = 0, glow }) => {
  const p = useIn(delay, 22);
  return (
    <div style={{ background: `linear-gradient(160deg, ${C.card}, #0c1a30)`, border: `1.5px solid ${glow ?? C.cardLine}`, borderRadius: 26, padding: 36,
      boxShadow: glow ? `0 0 46px ${glow}33` : "0 14px 40px rgba(0,0,0,0.35)", opacity: p, transform: `translateY(${(1 - p) * 40}px)`, ...style }}>
      {children}
    </div>
  );
};

export const Chip: React.FC<{ children: React.ReactNode; color?: string; delay?: number; size?: number }> = ({ children, color = C.teal, delay = 0, size = 28 }) => {
  const p = useIn(delay, 16);
  return (
    <span style={{ display: "inline-block", padding: "8px 22px", borderRadius: 999, border: `1.5px solid ${color}`, color, fontSize: size, fontWeight: 700,
      background: `${color}18`, opacity: p, transform: `scale(${0.85 + p * 0.15})` }}>{children}</span>
  );
};

// Moldura comum de cada cena: cabeçalho, número, kicker, título, subtítulo e legenda inferior.
export const Scene: React.FC<{ n: number; kicker: string; title: string; subtitle?: string; caption: string; duration: number; hero?: boolean; children: React.ReactNode }> = ({ n, kicker, title, subtitle, caption, duration, hero, children }) => {
  const f = useCurrentFrame();
  const fadeIn = interpolate(f, [0, 12], [0, 1], { extrapolateRight: "clamp" });
  const fadeOut = interpolate(f, [duration - 8, duration], [1, 0], { extrapolateLeft: "clamp" });
  const tIn = useIn(4, 22);
  const lineW = useIn(14, 26);
  return (
    <AbsoluteFill style={{ opacity: Math.min(fadeIn, fadeOut), fontFamily: FONT, color: C.text }}>
      <div style={{ position: "absolute", left: 110, top: 54, fontSize: 28, fontWeight: 800, letterSpacing: 2, color: C.teal }}>
        FÁCIL MUNICÍPIO <span style={{ color: C.muted }}>/</span> CYBERSECURITY
      </div>
      <div style={{ position: "absolute", right: 110, top: 56, fontSize: 28, color: C.muted, fontWeight: 600 }}>{String(n).padStart(2, "0")} / 12</div>
      <div style={{ position: "absolute", left: 110, top: 128, opacity: tIn, transform: `translateY(${(1 - tIn) * 24}px)` }}>
        <div style={{ fontSize: 26, fontWeight: 800, letterSpacing: 3, color: C.muted, textTransform: "uppercase" }}>{kicker}</div>
        {!hero && <><div style={{ fontSize: 84, fontWeight: 800, lineHeight: 1.05, marginTop: 8 }}>{title}</div>
        {subtitle && <div style={{ fontSize: 36, color: C.muted, marginTop: 10 }}>{subtitle}</div>}
        <div style={{ height: 5, width: 380 * lineW, background: C.teal, borderRadius: 4, marginTop: 20 }} /></>}
      </div>
      <div style={{ position: "absolute", left: 110, right: 110, top: 400, bottom: 190 }}>{children}</div>
      <div style={{ position: "absolute", left: 0, right: 0, bottom: 14, height: 150, background: "rgba(8,18,34,0.92)", borderTop: `1px solid ${C.cardLine}`,
        padding: "34px 110px", fontSize: 36, lineHeight: 1.35, display: "flex", alignItems: "center" }}>{caption}</div>
    </AbsoluteFill>
  );
};

export const ProgressBar: React.FC = () => {
  const f = useCurrentFrame();
  const { durationInFrames } = useVideoConfig();
  return <div style={{ position: "absolute", left: 0, bottom: 0, height: 14, width: `${(f / durationInFrames) * 100}%`, background: `linear-gradient(90deg, ${C.teal}, ${C.blue})` }} />;
};
