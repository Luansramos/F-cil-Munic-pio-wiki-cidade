import React from "react";
import { AbsoluteFill, Audio, Composition, Sequence, staticFile } from "remotion";
import { Background, FPS, ProgressBar, TOTAL_FRAMES, sceneFrames } from "./theme";
import { SCENES } from "./scenes";

const Video: React.FC = () => (
  <AbsoluteFill>
    <Background />
    {SCENES.map((S, i) => (
      <Sequence key={i} from={sceneFrames[i].from} durationInFrames={sceneFrames[i].duration}>
        <S duration={sceneFrames[i].duration} />
      </Sequence>
    ))}
    <ProgressBar />
    {/* Narração reaproveitada (214,6 s). Os cortes das cenas seguem as pausas dessa faixa. */}
    <Audio src={staticFile("narracao.mp3")} />
  </AbsoluteFill>
);

export const Root: React.FC = () => (
  <Composition id="Cybersecurity" component={Video} durationInFrames={TOTAL_FRAMES} fps={FPS} width={1920} height={1080} />
);
